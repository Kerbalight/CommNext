using CommNext.Network;
using KSP.Map;
using KSP.Sim;
using UnityEngine;

namespace CommNext.Rendering.Behaviors;

[DisallowMultipleComponent]
public class MapRulerComponent : MonoBehaviour, IMapComponent
{
    public string Id { get; set; } = null!;

    private bool _isTracking;
    private Map3DFocusItem? _target;

    private NetworkNode? _networkNode;
    public bool IsConnected { get; set; } = false;

    private MapSphereRulerComponent _sphereRulerComponent = null!;

    public Color? ConnectedColor;

    private static Color DefaultConnectedColor = new(1.433962f, 0.8418202f, 0.1826273f, 0f);
    private static Color DisconnectedColor = new(0.06603771f, 0.01445956f, 0.01090245f, 1f);
#if SHOW_RELAY_PLACEHOLDER
    private MapSphereRulerComponent? _placeholder;
#endif
    public void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Map");
    }

    public void Track(Map3DFocusItem target,
        bool isConnected,
        NetworkNode networkNode,
        ConnectionGraphNode connectionNode,
        Color? connectedColor,
        double commRange)
    {
        Id = target.AssociatedMapItem.SimGUID.ToString();
        _networkNode = networkNode;
        _target = target;
        IsConnected = isConnected;
        ConnectedColor = connectedColor;

        // Relay sphere
        var sphereObject = Instantiate(ConnectionsRenderer.RulerSpherePrefab, gameObject.transform);
        sphereObject.name = "RulerSphere";
        sphereObject.transform.localPosition = Vector3.zero;
        _sphereRulerComponent = sphereObject.AddComponent<MapSphereRulerComponent>();
        _sphereRulerComponent.Configure(
            commRange,
            IsConnected ? ConnectedColor ?? DefaultConnectedColor : DisconnectedColor
        );

#if SHOW_RELAY_PLACEHOLDER
        // Simple relay placeholder
        if (networkNode.IsRelay)
        {
            var placeholderObject = Instantiate(ConnectionsRenderer.RulerSpherePrefab, gameObject.transform);
            placeholderObject.name = "RulerPlaceholder";
            placeholderObject.transform.localPosition = Vector3.zero;
            _placeholder = placeholderObject.AddComponent<MapSphereRulerComponent>();
            _placeholder.Configure(50_000, Color.gray);
        }
#endif

        transform.position = target.transform.position;
        _isTracking = true;
    }

    public double CommRange
    {
        get => _sphereRulerComponent.Range;
        set => _sphereRulerComponent.Range = value;
    }

    private void Update()
    {
        if (!_isTracking) return;
        if (_target == null)
        {
            ConnectionsRenderer.Instance.OnMapRulerDestroyed(this);
            return;
        }

        _sphereRulerComponent.SetColor(IsConnected ? ConnectedColor ?? DefaultConnectedColor : DisconnectedColor);

        transform.position = _target.transform.position;
    }

    public void OnDestroy()
    {
        if (!_isTracking) return;
        ConnectionsRenderer.Instance.OnMapRulerDestroyed(this);
    }
}