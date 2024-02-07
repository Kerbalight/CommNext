using BepInEx.Logging;
using CommNext.Rendering;
using KSP.Game;
using KSP.Messages;
using KSP.Sim.impl;
using UitkForKsp2;
using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.UIElements;

namespace CommNext.UI;

/// <summary>
/// Mission summary shown on vessel recovery.
/// </summary>
public class MapToolbarWindowController : MonoBehaviour
{
    private static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource("CommNext.MapToolbarWindowController");

    private UIDocument _window = null!;

    public static WindowOptions WindowOptions = new()
    {
        WindowId = "CommNext_MapToolbarWindow",
        Parent = null,
        IsHidingEnabled = true,
        DisableGameInputForTextFields = true,
        MoveOptions = new MoveOptions
        {
            IsMovingEnabled = true,
            CheckScreenBounds = true
        }
    };

    // The elements of the window that we need to access
    private VisualElement _root = null!;
    private Button _linesButton = null!;

    private bool _isWindowOpen;

    public bool IsWindowOpen
    {
        get => _isWindowOpen;
        set
        {
            _isWindowOpen = value;
            _root.style.display = _isWindowOpen ? DisplayStyle.Flex : DisplayStyle.None;
            
            if (_isWindowOpen) UpdateButtonState();
        }
    }
    
    private void UpdateButtonState()
    {
        if (ConnectionsRenderer.Instance.IsEnabled) _linesButton.AddToClassList("toggled");
        else _linesButton.RemoveFromClassList("toggled");
    }

    /// <summary>
    /// Prepares the common Mission Window layout.
    /// </summary>
    private void OnEnable()
    {
        // Get the UIDocument component from the game object
        _window = GetComponent<UIDocument>();

        _root = _window.rootVisualElement[0];
        _root.SetDefaultPosition(size => new Vector2(Configuration.CurrentScreenWidth - size.x - 28, 300));

        // Content
        _linesButton = _root.Q<Button>("lines-button");
        _linesButton.clicked += () =>
        {
            ConnectionsRenderer.Instance.IsEnabled = !ConnectionsRenderer.Instance.IsEnabled;
            UpdateButtonState();
        };

        IsWindowOpen = false;
    }
}