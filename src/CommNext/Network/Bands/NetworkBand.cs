namespace CommNext.Network.Bands;

public struct NetworkBand
{
    /// <summary>
    /// This is the encoded name which will be saved to the save file.
    /// </summary>
    public string Code { get; }

    public string DisplayName { get; }

    public NetworkBand(string code, string displayName)
    {
        Code = code;
        DisplayName = displayName;
    }
}