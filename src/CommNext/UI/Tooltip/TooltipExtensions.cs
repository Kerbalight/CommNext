using UnityEngine.UIElements;

namespace CommNext.UI.Tooltip;

public static class TooltipExtensions
{
    /// <summary>
    /// Adds a static tooltip to the target element.
    /// </summary>
    public static void AddTooltip(this VisualElement target, string tooltipText)
    {
        target.AddManipulator(new TooltipManipulator(tooltipText));
    }
}