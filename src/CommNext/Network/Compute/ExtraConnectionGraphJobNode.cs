using KSP.Sim;
using Unity.Mathematics;

namespace CommNext.Compute;

public struct ExtraConnectionGraphJobNode
{
    public readonly ExtraConnectionGraphNodeFlags Flags;

    public ExtraConnectionGraphJobNode(ExtraConnectionGraphNodeFlags flags)
    {
        this.Flags = flags;
    }
}