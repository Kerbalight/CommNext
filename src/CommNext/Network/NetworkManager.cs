using KSP.Sim.impl;

namespace CommNext.Network;

public class NetworkManager
{
    public static NetworkManager Instance { get; private set; } = new();

    public Dictionary<IGGuid, NetworkNode> Nodes { get; private set; } = new();

    public void Initialize()
    {
        Nodes.Clear();
    }

    public void RegisterNode(NetworkNode node)
    {
        Nodes.Add(node.Owner, node);
    }

    public void UnregisterNode(IGGuid owner)
    {
        Nodes.Remove(owner);
    }
}