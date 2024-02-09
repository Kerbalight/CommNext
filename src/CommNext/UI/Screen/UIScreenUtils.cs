using UitkForKsp2;
using UitkForKsp2.API;

namespace CommNext.UI.Screen;

public static class UIScreenUtils
{
    public static float GetReferenceScreenScaledWidth()
    {
        return Configuration.IsAutomaticScalingEnabled
            ? ReferenceResolution.Width
            : ReferenceResolution.Width / Configuration.ManualUiScale;
    }

    /// <summary>
    /// Transforms a coordinate from the reference resolution to the current
    /// screen (scaled reference resolution).
    /// </summary>
    public static float GetScaledReferenceCoordinate(float coordinate)
    {
        return Configuration.IsAutomaticScalingEnabled
            ? coordinate
            : coordinate / Configuration.ManualUiScale;
    }
}