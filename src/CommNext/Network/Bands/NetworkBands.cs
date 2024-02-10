namespace CommNext.Network.Bands;

public class NetworkBands
{
    public static List<NetworkBand> AllBands { get; private set; } =
    [
        new NetworkBand("X", "X-Band"),
        new NetworkBand("S", "S-Band"),
        new NetworkBand("K", "K-Band"),
        new NetworkBand("Ka", "Ka-Band")
    ];
}