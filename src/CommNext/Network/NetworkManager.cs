using CommNext.Managers;
using KSP.Game;
using KSP.Messages;
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
}