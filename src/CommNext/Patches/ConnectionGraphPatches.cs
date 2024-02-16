// #define DEBUG_SET_NAMES

using BepInEx.Logging;
using CommNext.Network;
using CommNext.Network.Compute;
using CommNext.Utils;
using HarmonyLib;
using KSP.Game;
using KSP.Logging;
using KSP.Sim;
using KSP.Sim.impl;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace CommNext.Patches;

/// <summary>
/// We want to patch the `ConnectionGraph` class to add our own logic.
/// This is needed to pass custom info to the Job, plus we need to execute
/// the `GetNextConnectedNodesJob` with this custom info.
///
/// Basically we're replacing the IJob executed.
/// </summary>
public static class ConnectionGraphPatches
{
    private static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource("CommNext.ConnectionGraphPatches");

    private static GameInstance Game => GameManager.Instance.Game;
    private static IGGuid _kscId;

    // Since we have only one `ConnectionGraph` instance, we store
    // the additional infos here.
    private static NativeArray<CommNextBodyInfo> _bodyInfos;

    private static NativeArray<NetworkJobNode> _networkNodes;

    /// <summary>
    /// We want to store the connections between nodes, so we can use an
    /// array of doubles; this is a multi-dimensional array, represented
    /// with (i * N) + j, where N is the number of nodes.
    /// 
    /// This is needed to pass the connections to the Job and avoid
    /// allocating memory every time.
    ///
    /// Right now size is not an issue, since KSP2 by itself doesn't allow
    /// for a lot of background vessels. Even with 1.000 vessels, we're
    /// talking about 1.000.000 doubles, which is 8MB.
    ///
    /// In the future, if this is not sustainable, we can use two
    /// NativeArray, one of doubles (the distances) and one of ints (the
    /// nodes indices), with a fixed size (e.g. the first 100 connections),
    /// so that this could be linear in the number of nodes, or just
    /// use `NativeParallelMultiHashMap`
    /// </summary>
    private static NativeArray<NetworkJobConnection> _connections;

    public static NativeArray<NetworkJobConnection> Connections => _connections;

#if DEBUG_MAP_POSITIONS
    public static NativeArray<double3> debugPositions;
#endif

    /// <summary>
    /// Here starts the fun! We're going to patch the RebuildConnectionGraph method to add
    /// our own logic (in order to pass custom info to the Job), plus we need to execute our
    /// custom job. This makes this method a bit slower, but it's worth it since we are gaining
    /// performance in the `GetNextConnectedNodesJob`.
    /// </summary>
    [HarmonyPatch(typeof(ConnectionGraph), "RebuildConnectionGraph")]
    [HarmonyPrefix]
    // ReSharper disable InconsistentNaming
    public static bool RebuildNextConnectionGraph(ConnectionGraph __instance,
        ref bool ____hasBuiltGraph,
        ref bool ____isRunning,
        ref List<ConnectionGraphNode> ____allNodes,
        ref NativeArray<ConnectionGraph.ConnectionGraphJobNode> ____nodes,
        ref JobHandle ____jobHandle,
        ref int ____allNodeCount,
        ref NativeArray<int> ____previousIndices,
        ref int ____prevSourceIndex,
        // ReSharper restore InconsistentNaming
        List<ConnectionGraphNode> nodes,
        int sourceNodeIndex)
    {
        if (__instance.IsRunning)
        {
            Logger.LogError("Cannot rebuild CommNext MST. Job already in progress");
            return false;
        }

        ____hasBuiltGraph = false;
        ____allNodes.Clear();
        ____allNodes.AddRange((IEnumerable<ConnectionGraphNode>)nodes);
        ____allNodeCount = nodes.Count;
        if (!____nodes.IsCreated || ____allNodeCount != ____nodes.Length)
        {
            __instance.ResizeCollections(____allNodeCount);
            // Custom: resizing our custom array
            if (_networkNodes.IsCreated) _networkNodes.Dispose();
            _networkNodes = new NativeArray<NetworkJobNode>(____allNodeCount, Allocator.Persistent);

            if (_connections.IsCreated) _connections.Dispose();
            _connections =
                new NativeArray<NetworkJobConnection>(____allNodeCount * ____allNodeCount, Allocator.Persistent);
        }

        // Custom: We assume Bodies count never changes.
        if (!_bodyInfos.IsCreated)
        {
            _bodyInfos = new NativeArray<CommNextBodyInfo>(
                Game.UniverseModel.GetAllCelestialBodies().Count,
                Allocator.Persistent);
#if DEBUG_MAP_POSITIONS
            debugPositions = new NativeArray<double3>(3, Allocator.Persistent);
#endif
        }

        UpdateComputedBodiesPositions(_bodyInfos);

        for (var index = 0; index < ____nodes.Length; ++index)
        {
            var flagsFrom = ConnectionGraph.GetFlagsFrom(nodes[index]);
            ____nodes[index] =
                new ConnectionGraph.ConnectionGraphJobNode(nodes[index].Position, nodes[index].MaxRange, flagsFrom);

            // Custom: Extra flags
            _networkNodes[index] = GetNetworkNodeFrom(nodes[index]);
        }

        ____jobHandle = new GetNextConnectedNodesJob()
        {
            BestPath = Settings.BestPath.Value,
            Nodes = ____nodes,
            StartIndex = sourceNodeIndex,
            PrevIndices = ____previousIndices,
            // Custom: Extra data
            BodyInfos = _bodyInfos,
            NetworkNodes = _networkNodes,
            Connections = _connections
#if DEBUG_MAP_POSITIONS
            DebugPositions = debugPositions
#endif
        }.Schedule<GetNextConnectedNodesJob>();
        ____isRunning = true;
        ____prevSourceIndex = sourceNodeIndex;

        return false;
    }

    private static NetworkJobNode GetNetworkNodeFrom(ConnectionGraphNode node)
    {
        var networkJobNode = new NetworkJobNode();

        if (!NetworkManager.Instance.Nodes.TryGetValue(node.Owner, out var networkNode))
        {
            Logger.LogWarning($"Network node not found for {node.Owner}");
            return networkJobNode;
        }

        var flagsFrom = NetworkNodeFlags.None;
        if (networkNode.IsRelay)
            flagsFrom |= NetworkNodeFlags.IsRelay;
        if (networkNode.HasEnoughResources)
            flagsFrom |= NetworkNodeFlags.HasEnoughResources;

        networkJobNode.Flags = flagsFrom;
        networkJobNode.Name = Settings.EnableProfileLogs.Value ? networkNode.DebugVesselName : string.Empty;
        return networkJobNode;
    }

    /// <summary>
    /// We need the current position of the celestial bodies in order to compute the
    /// occlusion. We pass the bodyInfos to the job, since it's faster than using
    /// wrapper lists.
    /// </summary>
    private static void UpdateComputedBodiesPositions(NativeArray<CommNextBodyInfo> bodyInfos)
    {
        var game = GameManager.Instance.Game;
        var celestialBodies = game.UniverseModel.GetAllCelestialBodies();
        // Source = KSC
        var sourceNode = game.SessionManager.CommNetManager.GetSourceNode();
        var sourceTransform = (TransformModel)game.SpaceSimulation.FindSimObject(sourceNode.Owner).transform;
        _kscId = sourceNode.Owner;

        for (var i = 0; i < celestialBodies.Count; ++i)
        {
            var body = celestialBodies[i];
            bodyInfos[i] = new CommNextBodyInfo
            {
                position = sourceTransform.celestialFrame.ToLocalPosition(body.transform.Position),
                radius = body.radius,
                name = body.bodyName
            };
        }
    }
}