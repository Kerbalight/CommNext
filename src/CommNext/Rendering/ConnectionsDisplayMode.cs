namespace CommNext.Rendering;

public enum ConnectionsDisplayMode
{
    None,
    Lines,
    Active // Active vessel
}

public static class ConnectionsDisplayModeExtensions
{
    public static bool IsEnabled(this ConnectionsDisplayMode mode)
    {
        return mode != ConnectionsDisplayMode.None;
    }

    /// <summary>
    /// Toggles between the three display modes.
    /// </summary>
    /// <param name="mode"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static ConnectionsDisplayMode Next(this ConnectionsDisplayMode mode)
    {
        return mode switch
        {
            ConnectionsDisplayMode.None => ConnectionsDisplayMode.Lines,
            ConnectionsDisplayMode.Lines => ConnectionsDisplayMode.Active,
            ConnectionsDisplayMode.Active => ConnectionsDisplayMode.None,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}