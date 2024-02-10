using KSP.Sim.impl;

namespace CommNext.Network;

public class NetworkNode
{
    public IGGuid Owner { get; private set; }

    public bool IsRelay { get; set; }

    public NetworkNode(IGGuid owner)
    {
        Owner = owner;
    }
}