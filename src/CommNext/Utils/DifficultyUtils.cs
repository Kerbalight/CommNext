using KSP.Game;
using KSP.Networking.MP.Utils;

namespace CommNext.Utils;

public static class DifficultyUtils
{
    private static bool _hasInfinitePower;
    private static long _lastCachedInfinityPowerAt = 0;

    /// <summary>
    /// We want to cache this to avoid expensive calculations.
    /// </summary>
    public static bool HasInfinitePower
    {
        get
        {
            if (DateTime.Now.ToUnixTimestamp() - _lastCachedInfinityPowerAt < 5) return _hasInfinitePower;

            _lastCachedInfinityPowerAt = DateTime.Now.ToUnixTimestamp();
            _hasInfinitePower = GameManager.Instance.Game.SessionManager.IsDifficultyOptionEnabled("InfinitePower");
            return _hasInfinitePower;
        }
    }
}