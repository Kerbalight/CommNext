﻿using I2.Loc;

namespace CommNext.UI;

public static class LocalizedStrings
{
    public static LocalizedString RelayDescription = "PartModules/NextRelay/RelayDescription";
    public static LocalizedString ElectricCharge = "PartModules/NextRelay/ElectricCharge";
    public static LocalizedString Occluded = "CommNext/UI/Occluded";
    public static LocalizedString Powered = "CommNext/UI/Powered";
    public static LocalizedString Yes = "CommNext/UI/Yes";
    public static LocalizedString No = "CommNext/UI/No";
    public static LocalizedString Relay = "CommNext/UI/Relay";
    public static LocalizedString ConnectionInbound = "CommNext/UI/ConnectionInbound";
    public static LocalizedString ConnectionOutbound = "CommNext/UI/ConnectionOutbound";
    public static LocalizedString InDirection = "CommNext/UI/In";
    public static LocalizedString OutDirection = "CommNext/UI/Out";
    public static LocalizedString FilterActive = "CommNext/UI/FilterActive";
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
    public static LocalizedString NoneBand = "CommNext/UI/NoneBand";
    public static LocalizedString NoAvailableBand = "CommNext/UI/CommNext/UI/NoAvailableBand";
    public static LocalizedString ModulatorKind = "PartModules/NextModulator/Kind";
    public static LocalizedString ModulatorKindMonoBand = "PartModules/NextModulator/KindMonoBand";
    public static LocalizedString ModulatorKindDualBand = "PartModules/NextModulator/KindDualBand";
    public static LocalizedString ModulatorKindOmniBand = "PartModules/NextModulator/KindOmniBand";
    public static LocalizedString TooltipActivateBandRulers = "CommNext/UI/TooltipActivateBandRulers";
    public static LocalizedString TooltipNoPowerCurrentVessel = "CommNext/UI/TooltipNoPowerCurrentVessel";
    public static LocalizedString RulersDisplayModeNone = "CommNext/UI/RulersDisplayModeNone";
    public static LocalizedString RulersDisplayModeRelays = "CommNext/UI/RulersDisplayModeRelays";
    public static LocalizedString RulersDisplayModeAll = "CommNext/UI/RulersDisplayModeAll";

    // Only keys
    public const string DistanceLabelKey = "CommNext/UI/DistanceLabel";
    public const string OccludedByKey = "CommNext/UI/OccludedBy";
    public const string RangeLabelKey = "CommNext/UI/RangeLabel";
    public const string BandKey = "PartModules/NextRelay/Band";
    public const string SecondaryBandKey = "PartModules/NextRelay/SecondaryBand";
    public const string EnableRelayKey = "CommNext/UI/EnableRelay";
    public const string OmniBandKey = "CommNext/UI/OmniBand";
    public const string BandMissingRangeKey = "CommNext/UI/BandMissingRange";
    public const string ActionRequiresMapViewKey = "CommNext/UI/ActionRequiresMapView";

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