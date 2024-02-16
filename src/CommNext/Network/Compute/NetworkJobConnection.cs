using System.Runtime.InteropServices;

namespace CommNext.Network.Compute;

[StructLayout(LayoutKind.Sequential)]
public struct NetworkJobConnection
{
    public bool IsInRange;
    public bool IsConnected;
    public bool IsOccluded;
    public short OccludingBody;
}