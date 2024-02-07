using BepInEx.Logging;
using CommNext.Compute;
using HarmonyLib;
using KSP.Game;
using KSP.Logging;
using KSP.Sim;
using KSP.Sim.impl;
using Unity.Collections;
using Unity.Jobs;

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
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CommNext.ConnectionGraphPatches");

    private static GameInstance Game => GameManager.Instance.Game;
    private static IGGuid _kscId;

    // Since we have only one `ConnectionGraph` instance, we store
    // the additional infos here.
    private static NativeArray<CommNextBodyInfo> _bodyInfos;
    private static NativeArray<ExtraConnectionGraphJobNode> _extraNodes;

    /// <summary>
    /// Here starts the fun! We're going to patch the RebuildConnectionGraph method to add
    /// our own logic (in order to pass custom info to the Job), plus we need to execute our
    /// custom job. This makes this method a bit slower, but it's worth it since we are gaining
    /// performance in the `GetNextConnectedNodesJob`.
    /// </summary>
    [HarmonyPatch(typeof(ConnectionGraph), "RebuildConnectionGraph")]
    [HarmonyPrefix]
    // ReSharper disable InconsistentNaming
    public static void RebuildNextConnectionGraph(ConnectionGraph __instance,
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
            Logger.LogError("Cannot rebuild MST. Job already in progress");
            return;
        }

        ____hasBuiltGraph = false;
        ____allNodes.Clear();
        ____allNodes.AddRange((IEnumerable<ConnectionGraphNode>)nodes);
        ____allNodeCount = nodes.Count;
        if (!____nodes.IsCreated || ____allNodeCount != ____nodes.Length)
        {
            Traverse.Create(__instance).Method("ResizeCollections", [typeof(int)]).GetValue(____allNodeCount);
            // Custom: resizing our custom array
            if (_extraNodes.IsCreated) _extraNodes.Dispose();
            _extraNodes = new NativeArray<ExtraConnectionGraphJobNode>(____allNodeCount, Allocator.Persistent);
        }
        
        // Custom: We assume Bodies count never changes.
        if (!_bodyInfos.IsCreated)
        {
            _bodyInfos = new NativeArray<CommNextBodyInfo>(Game.UniverseModel.GetAllCelestialBodies().Count,
                Allocator.Persistent);
        }
        UpdateComputedBodiesPositions(_bodyInfos);

        var getFlagsFromMethod = Traverse.Create(__instance).Method("GetFlagsFrom", [typeof(ConnectionGraphNode)]);
        for (var index = 0; index < ____nodes.Length; ++index)
        {
            var flagsFrom = getFlagsFromMethod.GetValue<ConnectionGraphNodeFlags>(nodes[index]);
            ____nodes[index] = new ConnectionGraph.ConnectionGraphJobNode(nodes[index].Position, nodes[index].MaxRange, flagsFrom);
           
            // Custom: Extra flags
            _extraNodes[index] = new ExtraConnectionGraphJobNode(GetExtraFlagsFrom(nodes[index]));
        }

        ____jobHandle = new GetNextConnectedNodesJob()
        {
            Nodes = ____nodes,
            StartIndex = sourceNodeIndex,
            // Custom: Extra data
            BodyInfos = _bodyInfos,
            ExtraNodes = _extraNodes,
            PrevIndices = ____previousIndices
        }.Schedule<GetNextConnectedNodesJob>();
        ____isRunning = true;
        ____prevSourceIndex = sourceNodeIndex;
    }

    private static ExtraConnectionGraphNodeFlags GetExtraFlagsFrom(ConnectionGraphNode node)
    {
        var flagsFrom = ExtraConnectionGraphNodeFlags.None;
        if (true) // TODO We need custom module
            flagsFrom |= ExtraConnectionGraphNodeFlags.IsRelay;
        return flagsFrom;
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
                position = sourceTransform.celestialFrame.ToLocalPosition(body.Position),
                radius = body.radius,
                name = body.bodyName
            };
        }
    }
}