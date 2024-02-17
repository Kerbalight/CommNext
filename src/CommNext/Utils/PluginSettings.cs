using System.ComponentModel;
using BepInEx.Configuration;

namespace CommNext.Utils;

public static class PluginSettings
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

    public enum KSCRangeMode
    {
        [Description("2Gm")]
        G2,

        [Description("10Gm")]
        G10,

        [Description("50Gm")]
        G50
    }

    private static CommNextPlugin Plugin => CommNextPlugin.Instance;

    // Network
    public static ConfigEntry<BestPathMode> BestPath { get; private set; } = null!;
    public static ConfigEntry<bool> RelaysRequirePower { get; private set; } = null!;
    public static ConfigEntry<KSCRangeMode> KSCRange { get; private set; } = null!;

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

        RelaysRequirePower = Plugin.Config.Bind(
            "Network",
            "Relays require power",
            true,
            "If true, relays will require power to function.\n" +
            "It requires game to be reloaded to take effect."
        );

        KSCRange = Plugin.Config.Bind(
            "Network",
            "KSC range",
            KSCRangeMode.G2,
            "The range of the KSC in the network.\n" +
            "It requires game to be reloaded to take effect."
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