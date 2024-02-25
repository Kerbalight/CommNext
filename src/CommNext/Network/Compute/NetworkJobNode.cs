namespace CommNext.Network.Compute;

public struct NetworkJobNode
{
    public NetworkNodeFlags Flags;

    /// <summary>
    /// Bitmask of bands indexes
    /// </summary>
    public int BandsFlags;

    public string? Name;

    public NetworkJobNode(NetworkNodeFlags flags)
    {
        Flags = flags;
    }
}