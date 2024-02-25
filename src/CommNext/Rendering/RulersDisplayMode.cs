namespace CommNext.Rendering;

public enum RulersDisplayMode
{
    None,
    Relays,
    All // Relay + Antennas
}

public static class RulersDisplayModeExtensions
{
    public static bool IsEnabled(this RulersDisplayMode mode)
    {
        return mode != RulersDisplayMode.None;
    }

    /// <summary>
    /// Toggles between the three display modes.
    /// </summary>
    /// <param name="mode"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static RulersDisplayMode Next(this RulersDisplayMode mode)
    {
        return mode switch
        {
            RulersDisplayMode.None => RulersDisplayMode.Relays,
            RulersDisplayMode.Relays => RulersDisplayMode.All,
            RulersDisplayMode.All => RulersDisplayMode.None,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}