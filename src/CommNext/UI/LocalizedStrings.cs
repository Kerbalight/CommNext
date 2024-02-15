using I2.Loc;

namespace CommNext.UI;

public static class LocalizedStrings
{
    public static LocalizedString RelayDescription = "PartModules/NextRelay/RelayDescription";


    // Only keys
    public const string BandKey = "PartModules/NextRelay/Band";
    public const string ConnectionDetailsKey = "CommNext/UI/ConnectionDetails";

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