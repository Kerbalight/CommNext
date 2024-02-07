using System.ComponentModel;
using BepInEx.Configuration;

namespace CommNext.Utils;

public static class Settings
{
    private static CommNextPlugin Plugin => CommNextPlugin.Instance;

    // Debug
    public static ConfigEntry<bool> EnableProfileLogs { get; private set; } = null!;
    

    public static void SetupConfig()
    {
        // UI Ribbons
        EnableProfileLogs = Plugin.Config.Bind(
            "Debug",
            "Prints in Player.log the time it takes to compute the network",
            true,
            "If true, you can help me profiling this thing."
        );
        // ShowAlwaysBigRibbons.SettingChanged += OnSettingChanged;
    }

    /// <summary>
    /// Refresh the UI when a setting is changed.
    /// </summary>
    private static void OnSettingChanged(object sender, EventArgs e)
    {
    }
}