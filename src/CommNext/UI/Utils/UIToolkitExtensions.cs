using BepInEx.Logging;
using KSP.Game;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CommNext.UI.Utils;

public static class UIToolkitExtensions
{
    private static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource("CommNext.UIToolkitExtensions");

    private static GameInstance Game => GameManager.Instance.Game;

    private static List<InputAction> _maskedInputActions = [];

    private static List<InputAction> MaskedInputActions
    {
        get
        {
            if (_maskedInputActions.Count == 0)
                _maskedInputActions =
                [
                    Game.Input.Flight.CameraZoom,
                    Game.Input.Flight.mouseDoubleTap,
                    Game.Input.Flight.mouseSecondaryTap,

                    Game.Input.MapView.cameraZoom,
                    Game.Input.MapView.Focus,
                    Game.Input.MapView.mousePrimary,
                    Game.Input.MapView.mouseSecondary,
                    Game.Input.MapView.mouseTertiary,
                    Game.Input.MapView.mousePosition,

                    Game.Input.VAB.cameraZoom,
                    Game.Input.VAB.mousePrimary,
                    Game.Input.VAB.mouseSecondary
                ];

            return _maskedInputActions;
        }
    }

    private static readonly Dictionary<int, bool> MaskedInputActionsState = new();

    /// <summary>
    /// Stop the mouse events (scroll and click) from propagating to the game (e.g. zoom).
    /// The only place where the Click still doesn't get stopped is in the MapView, neither the Focus or the Orbit mouse events.
    /// </summary>
    public static void StopMouseEventsPropagation(this VisualElement element)
    {
        element.RegisterCallback<PointerEnterEvent>(OnVisualElementPointerEnter);
        element.RegisterCallback<PointerLeaveEvent>(OnVisualElementPointerLeave);
    }

    private static void OnVisualElementPointerEnter(PointerEnterEvent evt)
    {
        for (var i = 0; i < MaskedInputActions.Count; i++)
        {
            var inputAction = MaskedInputActions[i];
            MaskedInputActionsState[i] = inputAction.enabled;
            inputAction.Disable();
        }
    }

    private static void OnVisualElementPointerLeave(PointerLeaveEvent evt)
    {
        for (var i = 0; i < MaskedInputActions.Count; i++)
        {
            var inputAction = MaskedInputActions[i];
            if (MaskedInputActionsState[i])
                inputAction.Enable();
        }
    }

    /// <summary>
    /// Toggles between the classes provided, based on the toggle value.
    /// </summary>
    public static void ToggleClassesIf(this VisualElement element, bool toggle,
        IEnumerable<string> classesIf,
        IEnumerable<string> classesOtherwise
    )
    {
        foreach (var name in classesOtherwise)
            if (toggle)
                element.RemoveFromClassList(name);
            else
                element.AddToClassList(name);

        foreach (var name in classesIf)
            if (toggle)
                element.AddToClassList(name);
            else
                element.RemoveFromClassList(name);
    }

    /// <summary>
    /// Creates a colored string for UI.
    /// </summary>
    public static string UIColored(this string text, string color)
    {
        return $"<color={color}>{text}</color>";
    }
}