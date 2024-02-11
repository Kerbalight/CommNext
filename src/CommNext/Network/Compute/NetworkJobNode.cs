namespace CommNext.Network.Compute;

public struct NetworkJobNode
{
    public readonly NetworkNodeFlags Flags;
    public string? Name;

    public NetworkJobNode(NetworkNodeFlags flags)
    {
        Flags = flags;
    }
}