using UnityEngine;

namespace CommNext.Network.Bands;

[Serializable]
public class NetworkBand
{
    /// <summary>
    /// This is the encoded name which will be saved to the save file.
    /// </summary>
    public string Code { get; set; }

    public string DisplayName { get; set; }

    public Color Color { get; set; }

    public NetworkBand(string code, string displayName, Color color)
    {
        Code = code;
        DisplayName = displayName;
        Color = color;
    }
}