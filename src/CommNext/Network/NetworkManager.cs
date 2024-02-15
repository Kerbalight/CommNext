using CommNext.Managers;
using CommNext.Patches;
using KSP.Game;
using KSP.Messages;
using KSP.Sim;
using KSP.Sim.Definitions;
using KSP.Sim.impl;
using Unity.Collections;
using UnityEngine;

namespace CommNext.Network;

/// <summary>
/// CommNext version of the `CommNetManager`. This class is responsible for
/// managing the CommNet system, including the `ConnectionGraph` and the
/// nodes additional info we register.
///
/// Furthermore it keeps in sync itself with the `CommNetManager`, in order
/// to have the same lifecycle.
/// </summary>
public class NetworkManager : ILateUpdate
{
    public static NetworkManager Instance { get; private set; } = new();

    private bool _isInitialized;
    private CommNetManager? _commNetManager;

    public CommNetManager CommNetManager => _commNetManager!;

    // This is the time in seconds that the CommNetManager will wait
    // before rebuilding the graph.
    private const float RebuildGraphTimerSeconds = 0.2f;

    public Dictionary<IGGuid, NetworkNode> Nodes { get; private set; } = new();

    public void SetupListeners()
    {
        MessageListener.Messages.PersistentSubscribe<VesselControlStateChangedMessage>(OnVesselControlStateChanged);
    }

    public void Initialize(CommNetManager commNetManager)
    {
        Nodes.Clear();
        _commNetManager = commNetManager;
        _commNetManager._game.RegisterLateUpdate(this);
        _isInitialized = true;
    }

    public void Shutdown()
    {
        if (!_isInitialized)
            return;
        Nodes.Clear();
        _commNetManager!._game.UnregisterLateUpdate(this);
        _commNetManager = null;
        _isInitialized = false;
    }

    #region Nodes Lifecycle

    public void RegisterNode(NetworkNode node)
    {
        Nodes.Add(node.Owner, node);
    }

    public void UnregisterNode(IGGuid owner)
    {
        Nodes.Remove(owner);
    }

    private void OnVesselControlStateChanged(MessageCenterMessage message)
    {
        var controlMessage = (VesselControlStateChangedMessage)message;
        var networkNode = Nodes[controlMessage.Vessel.GlobalId];

        networkNode.UpdateFromVessel(controlMessage.Vessel);
    }

    #endregion

    /// <summary>
    /// We re-implement here the `CommNetManager`'s `Update` method since we
    /// want it to be executed on _LateUpdate_. This way, we can be sure all
    /// the bodies and nodes positions (from KSC) are updated.
    ///
    /// Without this fix, the `ConnectionGraph` would be built with outdated
    /// bodies positions, which would lead to incorrect occlusion calculations.
    /// This is especially important when the user is time-warping, or with
    /// outer planets, where the bodies move faster.
    /// </summary>
    public void OnLateUpdate()
    {
        var commNetManager = _commNetManager!;
        var spaceSimulation = commNetManager._game.SpaceSimulation;
        if (spaceSimulation is not { IsEnabled: true })
            return;

        commNetManager._timerRemaining -= Time.deltaTime;
        if ((double)commNetManager._timerRemaining < 0.0)
        {
            commNetManager._timerRemaining += RebuildGraphTimerSeconds;
            if (!commNetManager._isGraphBuilding)
                commNetManager._isDirty = true;
        }

        if (commNetManager._isDirty)
        {
            commNetManager._isDirty = false;
            commNetManager._isGraphBuilding = true;
            commNetManager._connectionGraph.RebuildConnectionGraph(
                commNetManager._allNodes,
                commNetManager._controlSourceIndex
            );
        }

        commNetManager._connectionGraph.OnUpdate();
        if (!commNetManager._isGraphBuilding || !commNetManager._connectionGraph.HasResult)
            return;
        commNetManager._isGraphBuilding = false;
    }

    /// <summary>
    /// Returns the results of the latest built graph, made by the
    /// `ConnectionGraph` job.
    /// </summary>
    public bool TryGetConnectionGraphNodesAndIndexes(
        out List<ConnectionGraphNode>? nodes,
        out int[] prevIndexes,
        out NativeArray<double>? connectedNodes)
    {
        var connectionGraph = _commNetManager?._connectionGraph;
        if (connectionGraph == null || connectionGraph.IsRunning ||
            ConnectionGraphPatches.ConnectedNodes is not { IsCreated: true })
        {
            nodes = null;
            prevIndexes = Array.Empty<int>();
            connectedNodes = null;
            return false;
        }

        nodes = connectionGraph._allNodes;

        var prevIndexesNative = connectionGraph._previousIndices;
        prevIndexes = prevIndexesNative is not { IsCreated: true }
            ? Array.Empty<int>()
            : prevIndexesNative.ToArray<int>();

        connectedNodes = ConnectionGraphPatches.ConnectedNodes;

        return true;
    }

    public List<NetworkConnection> GetNodeConnections(NetworkNode networkNode)
    {
        var connections = new List<NetworkConnection>();
        var connectedNodes = ConnectionGraphPatches.ConnectedNodes;
        var allNodes = _commNetManager!._connectionGraph._allNodes;
        var length = allNodes.Count;

        var nodeIndex = -1;
        for (var index = 0; index < length; index++)
        {
            if (allNodes[index].Owner != networkNode.Owner) continue;
            nodeIndex = index;
            break;
        }

        for (var i = 0; i < length; i++)
        {
            var sourceDistance = connectedNodes[i * length + nodeIndex];
            if (sourceDistance != 0)
            {
                var targetNode = allNodes[i];
                var targetNetworkNode = Nodes[targetNode.Owner];
                connections.Add(
                    new NetworkConnection(
                        networkNode, targetNetworkNode,
                        allNodes[nodeIndex], targetNode,
                        sourceDistance
                    ));
            }

            var targetDistance = connectedNodes[nodeIndex * length + i];
            if (targetDistance != 0)
            {
                var sourceNode = allNodes[i];
                var sourceNetworkNode = Nodes[sourceNode.Owner];
                connections.Add(
                    new NetworkConnection(
                        sourceNetworkNode, networkNode,
                        sourceNode, allNodes[nodeIndex],
                        targetDistance
                    ));
            }
        }

        return connections;
    }

    /// <summary>
    /// Returns the path from the target node to the source node for the given
    /// vessel ID.
    /// </summary>
    public bool TryGetNetworkPath(
        IGGuid targetId,
        List<ConnectionGraphNode> graphNodes,
        int[] prevIndexes,
        out HashSet<(int, int)>? path)
    {
        var targetIndex = -1;
        for (var i = 0; i < graphNodes.Count; i++)
        {
            if (graphNodes[i].Owner != targetId) continue;
            targetIndex = i;
            break;
        }

        if (targetIndex == -1 || !_isInitialized)
        {
            path = null;
            return false;
        }


        path = [];
        var currentIndex = targetIndex;
        while (currentIndex != _commNetManager!._connectionGraph._prevSourceIndex)
        {
            var prevIndex = prevIndexes[currentIndex];
            if (prevIndex == -1)
            {
                path = null;
                return false;
            }

            path.Add((prevIndex, currentIndex));
            currentIndex = prevIndex;
        }

        return true;
    }
}