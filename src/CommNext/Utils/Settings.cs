using System.ComponentModel;
using BepInEx.Configuration;

namespace CommNext.Utils;

public static class Settings
{
    public enum BestPathMode
    {
        /// <summary>
        /// The best path is the one with lowest distance to KSC.
        /// </summary>
        [Description("Shortest to KSC")]
        ShortestKSC,

        /// <summary>
        /// The best path is the one with minimum distance between relays.
        /// </summary>
        [Description("Nearest relay")]
        NearestRelay
    }

    private static CommNextPlugin Plugin => CommNextPlugin.Instance;

    // Network
    public static ConfigEntry<BestPathMode> BestPath { get; private set; } = null!;

    // Debug
    public static ConfigEntry<bool> EnableProfileLogs { get; private set; } = null!;


    public static void SetupConfig()
    {
        // Network
        BestPath = Plugin.Config.Bind(
            "Network",
            "Best path mode",
            BestPathMode.NearestRelay,
            "How to compute the best path for the network. \n" +
            "Shortest to KSC: the best path is the one with lowest distance to KSC. \n" +
            "Nearest relay: the best path is the one with minimum distance between relays."
        );

        // Debug
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
    private static void OnSettingChanged(object sender, EventArgs e) { }
}