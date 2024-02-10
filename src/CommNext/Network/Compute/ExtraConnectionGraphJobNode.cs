namespace CommNext.Network.Compute;

public struct ExtraConnectionGraphJobNode
{
    public readonly ExtraConnectionGraphNodeFlags Flags;

    public ExtraConnectionGraphJobNode(ExtraConnectionGraphNodeFlags flags)
    {
        Flags = flags;
    }
}