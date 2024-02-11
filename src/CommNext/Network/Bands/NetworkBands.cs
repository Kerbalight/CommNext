using UnityEngine;

namespace CommNext.Network.Bands;

public class NetworkBands
{
    public static NetworkBands Instance { get; private set; } = new();

    // TODO This should be saved to the save file with UI to customize it.
    public List<NetworkBand> AllBands { get; private set; } =
    [
        new NetworkBand("X", "X Band", new Color(0.174f, 0.783f, 0.777f, 1.000f)),
        new NetworkBand("S", "S Band", new Color(0.1735938f, 0.57f, 0.776589f, 1f)),
        new NetworkBand("K", "K Band", new Color(0.1735938f, 0.57f, 0.776589f, 1f)),
        new NetworkBand("Ka", "Ka Band", new Color(0.2210168f, 0.172549f, 0.7764706f, 1f))
    ];

    public Dictionary<string, NetworkBand> BandsByCode { get; private set; }

    private NetworkBands()
    {
        BandsByCode = AllBands.ToDictionary(band => band.Code);
    }
}