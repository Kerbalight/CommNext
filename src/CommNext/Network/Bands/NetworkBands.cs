using UnityEngine;

namespace CommNext.Network.Bands;

public class NetworkBands
{
    public static NetworkBands Instance { get; private set; } = new();

    public const string DefaultBand = "X";

    // TODO This should be saved to the save file with UI to customize it.
    public List<NetworkBand> AllBands { get; private set; } =
    [
        new NetworkBand(DefaultBand, "X Band", new Color(0.174f, 0.783f, 0.777f, 1.000f)),
        new NetworkBand("S", "S Band", new Color(0.09f, 0.57f, 0.97f)),
        new NetworkBand("K", "K Band", new Color(0.84f, 0.15f, 0.92f)),
        new NetworkBand("Ka", "Ka Band", new Color(0.25f, 0.18f, 0.98f))
    ];

    /// <summary>
    /// We cache all bands to avoid unnecessary conversions, for
    /// performance reasons.
    /// </summary>
    private NetworkBand[] _allBandsCache = null!;

    // TODO When bands will be editable, we need to dirty this cache
    public NetworkBand[] AllBandsCache => _allBandsCache ??= AllBands.ToArray();

    public Dictionary<string, NetworkBand> BandsByCode { get; private set; }
    public Dictionary<string, int> BandIndexByCode { get; private set; }

    private NetworkBands()
    {
        BandsByCode = AllBands.ToDictionary(band => band.Code);
        BandIndexByCode = AllBands.Select((band, index) => new { band.Code, index })
            .ToDictionary(band => band.Code, band => band.index);
    }

    public int GetBandIndex(string bandCode)
    {
        var index = BandIndexByCode.GetValueOrDefault(bandCode, -1);
        return index;
    }
}