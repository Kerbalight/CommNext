using BepInEx.Logging;
using CommNext.Rendering;
using CommNext.UI.Screen;
using CommNext.UI.Tooltip;
using CommNext.UI.Utils;
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
    private TooltipManipulator _linesTooltip = null!;
    private Button _rulersButton = null!;
    private TooltipManipulator _rulersTooltip = null!;
    private Button _vesselReportButton = null!;
    public VisualElement Root => _root;

    public float Width => _root.resolvedStyle.width;
    public float Height => _root.resolvedStyle.height;

    private bool _isWindowPositionInitialized;

    public Vector3 Position
    {
        get => _root.transform.position;
        set
        {
            _isWindowPositionInitialized = true;
            _root.transform.position = value;
        }
    }

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

    public void UpdateButtonState()
    {
        // 1. Connections
        // TODO Move the _state_ inside a separate object like `ConnectionsRenderState`, which is not tied to MonoBehaviour lifecycle.
        var connectionsDisplayMode =
            ConnectionsRenderer.Instance?.ConnectionsDisplayMode ?? ConnectionsDisplayMode.None;

        _linesButton.RemoveFromClassList("toolbar__icon--comm-none");
        _linesButton.RemoveFromClassList("toolbar__icon--comm-lines");
        _linesButton.RemoveFromClassList("toolbar__icon--comm-active");
        var selectedClassName = connectionsDisplayMode switch
        {
            ConnectionsDisplayMode.None => "toolbar__icon--comm-none",
            ConnectionsDisplayMode.Lines => "toolbar__icon--comm-lines",
            ConnectionsDisplayMode.Active => "toolbar__icon--comm-active",
            _ => "toolbar__icon--comm-none"
        };
        _linesButton.AddToClassList(selectedClassName);

        _linesTooltip.TooltipText = connectionsDisplayMode switch
        {
            ConnectionsDisplayMode.None => LocalizedStrings.ConnectionsDisplayModeNone,
            ConnectionsDisplayMode.Lines => LocalizedStrings.ConnectionsDisplayModeLines,
            ConnectionsDisplayMode.Active => LocalizedStrings.ConnectionsDisplayModeActive,
            _ => "N/A"
        };


        // 2. Rulers
        var rulersDisplayMode = ConnectionsRenderer.Instance?.RulersDisplayMode ?? RulersDisplayMode.None;
        _rulersButton.RemoveFromClassList("toolbar__icon--rulers-none");
        _rulersButton.RemoveFromClassList("toolbar__icon--rulers-relays");
        _rulersButton.RemoveFromClassList("toolbar__icon--rulers-all");
        var rulersSelectedClassName = rulersDisplayMode switch
        {
            RulersDisplayMode.None => "toolbar__icon--rulers-none",
            RulersDisplayMode.Relays => "toolbar__icon--rulers-relays",
            RulersDisplayMode.All => "toolbar__icon--rulers-all",
            _ => "toolbar__icon--rulers-none"
        };
        _rulersButton.AddToClassList(rulersSelectedClassName);

        _rulersTooltip.TooltipText = rulersDisplayMode switch
        {
            RulersDisplayMode.None => LocalizedStrings.RulersDisplayModeNone,
            RulersDisplayMode.Relays => LocalizedStrings.RulersDisplayModeRelays,
            RulersDisplayMode.All => LocalizedStrings.RulersDisplayModeAll,
            _ => "N/A"
        };

        // 3. Vessel report
        // ReSharper disable once Unity.NoNullPropagation
        if (MainUIManager.Instance.VesselReportWindow?.IsWindowOpen == true)
            _vesselReportButton.AddToClassList("toggled");
        else _vesselReportButton.RemoveFromClassList("toggled");
    }

    /// <summary>
    /// Prepares the common Mission Window layout.
    /// </summary>
    private void OnEnable()
    {
        // Get the UIDocument component from the game object
        _window = GetComponent<UIDocument>();

        _root = _window.rootVisualElement[0];
        _root.SetDefaultPosition(size => _isWindowPositionInitialized
            ? _root.transform.position
            : new Vector2(
                UIScreenUtils.GetReferenceScreenScaledWidth() - size.x -
                UIScreenUtils.GetScaledReferenceCoordinate(28f),
                UIScreenUtils.GetScaledReferenceCoordinate(300f)
            ));

        // Content
        _linesButton = _root.Q<Button>("lines-button");
        _linesTooltip = new TooltipManipulator("All active connections");
        _linesButton.AddManipulator(_linesTooltip);
        _linesButton.clicked += () =>
        {
            ConnectionsRenderer.Instance.ConnectionsDisplayMode =
                ConnectionsRenderer.Instance.ConnectionsDisplayMode.Next();
            UpdateButtonState();
        };

        _rulersButton = _root.Q<Button>("rulers-button");
        _rulersTooltip = new TooltipManipulator(LocalizedStrings.RulersTooltip);
        _rulersButton.AddManipulator(_rulersTooltip);
        _rulersButton.clicked += () =>
        {
            ConnectionsRenderer.Instance.RulersDisplayMode = ConnectionsRenderer.Instance.RulersDisplayMode.Next();
            UpdateButtonState();
        };

        _vesselReportButton = _root.Q<Button>("vessel-report-button");
        _vesselReportButton.AddManipulator(new TooltipManipulator(LocalizedStrings.VesselReportTooltip));
        _vesselReportButton.clicked += () =>
        {
            // If the vessel report window is already open, close it
            if (MainUIManager.Instance.VesselReportWindow!.IsWindowOpen)
            {
                MainUIManager.Instance.VesselReportWindow.IsWindowOpen = false;
                return;
            }

            // Else, open it for the active vessel
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