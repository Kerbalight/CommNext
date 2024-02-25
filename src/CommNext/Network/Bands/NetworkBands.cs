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
        new NetworkBand("K", "K Band", new Color(0.44f, 0.39f, 1f, 1f)),
        new NetworkBand("Ka", "Ka Band", new Color(0.84f, 0.15f, 0.92f)),
        new NetworkBand("V", "V Band", new Color(0.17f, 0.85f, 0.58f, 1f))
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

    private Dictionary<string, Sprite> _bandIconSprites = new();

    public Sprite GetIconSprite(string bandCode)
    {
        if (_bandIconSprites.TryGetValue(bandCode, out var sprite)) return sprite;

        var texture = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        var pixels = new Color[16 * 16];
        var transparent = new Color(0, 0, 0, 0);
        for (var i = 0; i < pixels.Length; i++)
        {
            // Corners
            var x = i % 16;
            var y = i / 16;
            if ((x < 2 && y is < 2 or > 13) || (x > 13 && y is < 2 or > 13))
                pixels[i] = transparent;
            else
                pixels[i] = BandsByCode[bandCode].Color;
        }

        texture.SetPixels(pixels);

        sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), 100.0f);

        _bandIconSprites[bandCode] = sprite;
        return sprite;
    }
}