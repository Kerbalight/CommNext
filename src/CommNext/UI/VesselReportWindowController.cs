using BepInEx.Logging;
using CommNext.Network;
using CommNext.Rendering;
using CommNext.UI.Components;
using CommNext.UI.Screen;
using CommNext.UI.Utils;
using KSP.Sim.impl;
using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.UIElements;

namespace CommNext.UI;

public class VesselReportWindowController : MonoBehaviour
{
    private static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource("CommNext.VesselReportWindowController");

    private UIDocument _window = null!;

    public static WindowOptions WindowOptions = new()
    {
        WindowId = "CommNext_VesselReportWindow",
        Parent = null,
        IsHidingEnabled = true,
        DisableGameInputForTextFields = true,
        MoveOptions = new MoveOptions
        {
            IsMovingEnabled = true,
            CheckScreenBounds = false
        }
    };

    // The elements of the window that we need to access
    private VisualElement _root = null!;
    private Label _nameLabel = null!;
    private ScrollView _connectionsList = null!;

    private bool _isWindowOpen;

    private VesselComponent? _vessel;

    // Refresh
    private const float RefreshRate = 1f; // 1 second
    private float _lastRefresh = 0f;

    public bool IsWindowOpen
    {
        get => _isWindowOpen;
        set
        {
            _isWindowOpen = value;
            _root.style.display = _isWindowOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (value)
            {
                _lastRefresh = 0f;
                BuildUI();
            }
        }
    }

    public void OpenForVessel(VesselComponent vessel)
    {
        _vessel = vessel;
        AlignWindowToToolbar();
        IsWindowOpen = true;
    }

    /// <summary>
    /// When the window is opened, we align it to the right of the main window.
    /// </summary>
    private void AlignWindowToToolbar()
    {
        var toolbarWindow = MainUIManager.Instance.MapToolbarWindow;
        var appWindowPosition = toolbarWindow!.Root.transform.position;
        _root.transform.position = new Vector3(
            appWindowPosition.x - 400 + toolbarWindow.Width,
            appWindowPosition.y + 10 + toolbarWindow.Height,
            appWindowPosition.z
        );
    }

    /// <summary>
    /// Prepares the common Window layout.
    /// </summary>
    private void OnEnable()
    {
        // Get the UIDocument component from the game object
        _window = GetComponent<UIDocument>();

        _root = _window.rootVisualElement[0];
        _root.StopMouseEventsPropagation();
        _root.CenterByDefault();

        // Content
        _nameLabel = _root.Q<Label>("name-label");
        _connectionsList = _root.Q<ScrollView>("connections-list");

        // Get the close button from the window
        var closeButton = _root.Q<Button>("close-button");
        closeButton.clicked += () => IsWindowOpen = false;

        IsWindowOpen = false;
    }

    private void BuildUI()
    {
        if (_vessel == null) return;

        var networkNode = NetworkManager.Instance.Nodes[_vessel.GlobalId];
        _nameLabel.text = _vessel!.Name;

        var connections = NetworkManager.Instance.GetNodeConnections(networkNode);
        var connectionElements = _connectionsList.Children().ToList();

        // Some basic pooling
        for (var i = 0; i < connections.Count; i++)
            if (i < connectionElements.Count)
            {
                var connectionRow = (NetworkConnectionViewController)connectionElements[i].userData;
                connectionRow.Bind(networkNode, connections[i]);
            }
            else
            {
                var connectionRow = new NetworkConnectionViewController();
                connectionRow.Bind(networkNode, connections[i]);
                _connectionsList.Add(connectionRow.Root);
            }

        for (var i = connections.Count; i < connectionElements.Count; i++)
            _connectionsList.Remove(connectionElements[i]);
    }

    private void Update()
    {
        if (!IsWindowOpen) return;
        _lastRefresh += Time.deltaTime;
        if (_lastRefresh < RefreshRate) return;
        _lastRefresh = 0f;

        BuildUI();
    }
}