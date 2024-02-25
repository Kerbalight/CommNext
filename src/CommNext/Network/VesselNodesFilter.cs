namespace CommNext.Network;

public enum VesselNodesFilter
{
    All,

    /// <summary>
    /// Only show connections which are used right now.
    /// </summary>
    Active,

    /// <summary>
    /// Show all connections available, even if not used
    /// </summary>
    Connected,
    InRange // Default
}