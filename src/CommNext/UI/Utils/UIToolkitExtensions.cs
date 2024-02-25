using BepInEx.Logging;
using KSP.Game;
using UnityEngine;
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
    public static string RTEColor(this string text, string color)
    {
        return $"<color={color}>{text}</color>";
    }

    public static string RTEColor(this string text, Color color)
    {
        return RTEColor(text, $"#{ColorUtility.ToHtmlStringRGB(color)}");
    }

    /// <summary>
    /// Replace the VisualElement children with the provided items, using the binder to
    /// bind the item to the VisualElement.
    /// If some items are already present, they are reused, avoiding the cost of creating
    /// new VisualElements.
    /// </summary>
    /// <param name="parent">The parent element</param>
    /// <param name="items">items data source</param>
    /// <param name="binder">Binds an item to its visual element</param>
    /// <typeparam name="TItem"></typeparam>
    /// <typeparam name="TElement"></typeparam>
    public static void PoolChildren<TItem, TElement>(
        this VisualElement parent,
        IEnumerable<TItem> items,
        Action<TItem, TElement> binder) where TElement : IPoolingElement, new()
    {
        var children = parent.Children().ToArray();
        var itemsArray = items as TItem[] ?? items.ToArray();
        for (var i = 0; i < itemsArray.Length; i++)
        {
            TElement itemElement;
            if (i < children.Length)
            {
                itemElement = (TElement)children[i].userData;
            }
            else
            {
                itemElement = new TElement();
                parent.Add(itemElement.Root);
            }

            binder(itemsArray[i], itemElement);
        }

        for (var i = itemsArray.Length; i < children.Length; i++)
            parent.Remove(children[i]);
    }
}