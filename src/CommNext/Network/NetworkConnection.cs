using KSP.Sim;
using Unity.Mathematics;

namespace CommNext.Network;

public class NetworkConnection
{
    public double DistanceSquared { get; private set; }
    public double Distance => math.sqrt(DistanceSquared);
    public NetworkNode Source { get; private set; }
    public ConnectionGraphNode SourceNode { get; private set; }
    public NetworkNode Target { get; private set; }
    public ConnectionGraphNode TargetNode { get; private set; }

    public bool IsConnected { get; private set; }

    public NetworkConnection(
        NetworkNode source,
        NetworkNode target,
        ConnectionGraphNode sourceNode,
        ConnectionGraphNode targetNode,
        double distanceSquared)
    {
        Source = source;
        Target = target;
        SourceNode = sourceNode;
        TargetNode = targetNode;
        DistanceSquared = math.abs(distanceSquared);
        IsConnected = distanceSquared > 0;
    }

    public bool IsSource(NetworkNode node)
    {
        return Source == node;
    }

    public NetworkNode GetOther(NetworkNode node)
    {
        return Source == node ? Target : Source;
    }
}