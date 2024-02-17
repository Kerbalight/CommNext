using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.UIElements;

namespace CommNext.UI.Tooltip;

/// <summary>
/// A static window overlay that can be used to display tooltips.
/// It's important that the Window is the _last_ element in the hierarchy,
/// so that it's drawn on top of everything else.
/// 
/// Please note that the Window in UXML has Pointer Events disabled
/// (Ignore).
/// </summary>
public class TooltipWindowController : MonoBehaviour
{
    private UIDocument _window = null!;

    public static WindowOptions WindowOptions = new()
    {
        WindowId = "CommNext_TooltipWindow",
        Parent = null,
        IsHidingEnabled = true,
        MoveOptions = new MoveOptions
        {
            IsMovingEnabled = false,
            CheckScreenBounds = false
        }
    };

    // The elements of the window that we need to access
    private VisualElement _root = null!;
    private VisualElement _tooltip = null!;
    private Label _tooltipText = null!;

    public void OnEnable()
    {
        _window = GetComponent<UIDocument>();
        _root = _window.rootVisualElement[0];

        // Tooltip elements
        _tooltip = _root.Q<VisualElement>("tooltip");
        _tooltipText = _tooltip.Q<Label>("tooltip__text");
    }

    public void ToggleTooltip(bool isVisible, VisualElement target, string text = "")
    {
        if (isVisible)
        {
            _tooltipText.text = text;
            _tooltip.style.opacity = 1;
            _tooltip.style.left = -_root.worldBound.xMin + target.worldBound.xMin + target.worldBound.width / 2;
            _tooltip.style.top = -_root.worldBound.yMin + target.worldBound.yMin - 5;
        }
        else
        {
            _tooltip.style.opacity = 0;
        }
    }

    public bool IsHiDPI()
    {
        return UnityEngine.Screen.width / _root.worldBound.width > 1.5;
    }
}