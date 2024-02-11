using CommNext.Managers;
using KSP.Game;
using KSP.Messages;
using KSP.Sim;
using KSP.Sim.Definitions;
using KSP.Sim.impl;

namespace CommNext.Network;

public class NetworkManager
{
    public static NetworkManager Instance { get; private set; } = new();

    public Dictionary<IGGuid, NetworkNode> Nodes { get; private set; } = new();

    public void Initialize()
    {
        Nodes.Clear();
        MessageListener.Messages.PersistentSubscribe<VesselControlStateChangedMessage>(OnVesselControlStateChanged);
    }

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

        networkNode.UpdateResourcesFromVessel(controlMessage.Vessel);
    }

    /// <summary>
    /// Returns the path from the target node to the source node for the given
    /// vessel ID.
    /// </summary>
    public static bool TryGetNetworkPath(
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

        if (targetIndex == -1)
        {
            path = null;
            return false;
        }


        path = [];
        var currentIndex = targetIndex;
        while (currentIndex !=
               CommunicationsManager.CommNetManager._connectionGraph._prevSourceIndex)
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