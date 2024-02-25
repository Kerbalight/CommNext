using CommNext.Network.Compute;
using KSP.Sim;
using Unity.Mathematics;

namespace CommNext.Network;

/// <summary>
/// Represents a connection between two nodes in the network.
/// It's a wrapper around the `NetworkJobConnection` struct, it's used
/// only to represent the connections in the UI.
/// </summary>
public class NetworkConnection
{
    public double DistanceSquared { get; private set; }
    public double Distance { get; private set; }
    public NetworkNode Source { get; private set; }
    public ConnectionGraphNode SourceNode { get; private set; }
    public NetworkNode Target { get; private set; }
    public ConnectionGraphNode TargetNode { get; private set; }

    public bool IsActive { get; private set; }
    public bool IsConnected { get; private set; }
    public int? OccludingBody { get; private set; }
    public short? SelectedBand { get; private set; }

    /// <summary>
    /// Set to true if the band is not present in both nodes, and the
    /// connection failed because of that.
    /// </summary>
    public bool IsBandMissingRange { get; private set; }

    /// <summary>
    /// Set to true if the band is not present in both nodes, and the
    /// connection failed because of that.
    /// </summary>
    public bool IsBandNotAvailable { get; private set; }

    public NetworkConnection(
        NetworkNode source,
        NetworkNode target,
        ConnectionGraphNode sourceNode,
        ConnectionGraphNode targetNode,
        NetworkJobConnection jobConnection,
        bool isActive)
    {
        Source = source;
        Target = target;
        SourceNode = sourceNode;
        TargetNode = targetNode;
        DistanceSquared = math.distancesq(sourceNode.Position, targetNode.Position);
        Distance = math.sqrt(DistanceSquared);
        IsConnected = jobConnection.IsConnected;
        SelectedBand = jobConnection.HasMatchingBand ? jobConnection.SelectedBand : null;
        OccludingBody = jobConnection.IsOccluded ? jobConnection.OccludingBody : null;
        IsBandMissingRange = jobConnection.IsBandMissingRange;
        IsBandNotAvailable = jobConnection.SelectedBand == -1;
        IsActive = isActive;
    }

    public bool IsSource(NetworkNode node)
    {
        return Source == node;
    }

    public bool IsConnectedInbound(NetworkNode node)
    {
        return Target == node && IsConnected;
    }

    public bool IsPowered()
    {
        return this is { Source.HasEnoughResources: true, Target.HasEnoughResources: true };
    }

    /// <summary>
    /// Checks if the connection has a band in common.
    /// </summary>
    /// <returns></returns>
    public bool HasMatchingBand()
    {
        for (var i = 0; i < Source.BandRanges.Length; i++)
            if (Source.BandRanges[i] > 0 && Target.BandRanges[i] > 0)
                return true;

        return false;
    }

    public NetworkNode GetOther(NetworkNode node)
    {
        return Source == node ? Target : Source;
    }

    public double SignalStrength()
    {
        var minRange = math.min(SourceNode.MaxRange, TargetNode.MaxRange);

        // See: https://wiki.kerbalspaceprogram.com/wiki/CommNet
        var relativeDistance = math.clamp(1 - Distance / minRange, 0, 1);
        var strength = (3 - 2 * relativeDistance) * relativeDistance * relativeDistance;
        return strength;
    }
}