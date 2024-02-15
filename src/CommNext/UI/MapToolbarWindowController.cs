using BepInEx.Logging;
using CommNext.Rendering;
using CommNext.UI.Screen;
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
    private Button _rulersButton = null!;
    private Button _vesselReportButton = null!;
    public VisualElement Root => _root;

    public float Width => _root.resolvedStyle.width;
    public float Height => _root.resolvedStyle.height;

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
        _linesButton.RemoveFromClassList("toolbar-comm-icon--none");
        _linesButton.RemoveFromClassList("toolbar-comm-icon--lines");
        _linesButton.RemoveFromClassList("toolbar-comm-icon--active");
        var selectedClassName = ConnectionsRenderer.Instance.ConnectionsDisplayMode switch
        {
            ConnectionsDisplayMode.None => "toolbar-comm-icon--none",
            ConnectionsDisplayMode.Lines => "toolbar-comm-icon--lines",
            ConnectionsDisplayMode.Active => "toolbar-comm-icon--active",
            _ => "toolbar-comm-icon--none"
        };
        _linesButton.AddToClassList(selectedClassName);

        if (ConnectionsRenderer.Instance.IsRulersEnabled) _rulersButton.AddToClassList("toggled");
        else _rulersButton.RemoveFromClassList("toggled");
    }

    /// <summary>
    /// Prepares the common Mission Window layout.
    /// </summary>
    private void OnEnable()
    {
        // Get the UIDocument component from the game object
        _window = GetComponent<UIDocument>();

        _root = _window.rootVisualElement[0];
        _root.SetDefaultPosition(size => new Vector2(
            UIScreenUtils.GetReferenceScreenScaledWidth() - size.x - UIScreenUtils.GetScaledReferenceCoordinate(28f),
            UIScreenUtils.GetScaledReferenceCoordinate(300f)
        ));

        // Content
        _linesButton = _root.Q<Button>("lines-button");
        _linesButton.clicked += () =>
        {
            ConnectionsRenderer.Instance.ConnectionsDisplayMode =
                ConnectionsRenderer.Instance.ConnectionsDisplayMode.Next();
            UpdateButtonState();
        };

        _rulersButton = _root.Q<Button>("rulers-button");
        _rulersButton.clicked += () =>
        {
            ConnectionsRenderer.Instance.IsRulersEnabled = !ConnectionsRenderer.Instance.IsRulersEnabled;
            UpdateButtonState();
        };

        _vesselReportButton = _root.Q<Button>("vessel-report-button");
        _vesselReportButton.clicked += () =>
        {
            if (!GameManager.Instance.Game.ViewController.TryGetActiveSimVessel(out var vessel))
            {
                Logger.LogWarning("No active vessel found");
                return;
            }

            MainUIManager.Instance.VesselReportWindow!.OpenForVessel(vessel);
        };

        IsWindowOpen = false;
    }
}