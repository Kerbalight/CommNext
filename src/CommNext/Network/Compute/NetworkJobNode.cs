namespace CommNext.Network.Compute;

public struct NetworkJobNode
{
    public NetworkNodeFlags Flags;
    public string? Name;

    public NetworkJobNode(NetworkNodeFlags flags)
    {
        Flags = flags;
    }
}