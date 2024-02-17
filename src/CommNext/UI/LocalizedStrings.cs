using I2.Loc;

namespace CommNext.UI;

public static class LocalizedStrings
{
    public static LocalizedString RelayDescription = "PartModules/NextRelay/RelayDescription";
    public static LocalizedString Occluded = "CommNext/UI/Occluded";
    public static LocalizedString Powered = "CommNext/UI/Powered";
    public static LocalizedString Yes = "CommNext/UI/Yes";
    public static LocalizedString No = "CommNext/UI/No";
    public static LocalizedString Relay = "CommNext/UI/Relay";
    public static LocalizedString ConnectionInbound = "CommNext/UI/ConnectionInbound";
    public static LocalizedString ConnectionOutbound = "CommNext/UI/ConnectionOutbound";
    public static LocalizedString InDirection = "CommNext/UI/In";
    public static LocalizedString OutDirection = "CommNext/UI/Out";
    public static LocalizedString FilterConnected = "CommNext/UI/FilterConnected";
    public static LocalizedString FilterInRange = "CommNext/UI/FilterInRange";
    public static LocalizedString FilterAll = "CommNext/UI/FilterAll";
    public static LocalizedString SortByDistance = "CommNext/UI/SortByDistance";
    public static LocalizedString SortByBand = "CommNext/UI/SortByBand";
    public static LocalizedString SortBySignalStrength = "CommNext/UI/SortBySignalStrength";
    public static LocalizedString SortByName = "CommNext/UI/SortByName";
    public static LocalizedString KSCCommNet = "CommNext/Simulation/KSCCommNet";
    public static LocalizedString ConnectionsDisplayModeNone = "CommNext/UI/ConnectionsDisplayModeNone";
    public static LocalizedString ConnectionsDisplayModeLines = "CommNext/UI/ConnectionsDisplayModeLines";
    public static LocalizedString ConnectionsDisplayModeActive = "CommNext/UI/ConnectionsDisplayModeActive";
    public static LocalizedString RulersTooltip = "CommNext/UI/RulersTooltip";
    public static LocalizedString VesselReportTooltip = "CommNext/UI/VesselReportTooltip";
    public static LocalizedString FilterLabel = "CommNext/UI/FilterLabel";
    public static LocalizedString SortLabel = "CommNext/UI/SortLabel";
    public static LocalizedString NoPower = "CommNext/UI/NoPower";

    // Only keys
    public const string DistanceLabelKey = "CommNext/UI/DistanceLabel";
    public const string OccludedByKey = "CommNext/UI/OccludedBy";
    public const string BandKey = "PartModules/NextRelay/Band";

    public static string GetTranslationWithParams(string localizationKey, Dictionary<string, string>? parameters)
    {
        var translation = LocalizationManager.GetTranslation(localizationKey);
        if (translation == null) return localizationKey;
        if (parameters == null) return translation;

        foreach (var (key, value) in parameters)
        {
            // Allows substitution of other localization keys
            var substitution = value?.StartsWith("#") == true
                ? LocalizationManager.GetTranslation(value[1..]) ?? value
                : value;

            translation = translation.Replace($"{{{key}}}", substitution);
        }

        return translation;
    }
}