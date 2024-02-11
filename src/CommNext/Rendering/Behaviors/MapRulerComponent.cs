using CommNext.Network;
using KSP.Map;
using KSP.Sim;
using UnityEngine;

namespace CommNext.Rendering.Behaviors;

public class MapRulerComponent : MonoBehaviour, IMapComponent
{
    public string Id { get; set; } = null!;

    private bool _isTracking;
    private Map3DFocusItem? _target;

    private NetworkNode? _networkNode;

#if SHOW_RELAY_PLACEHOLDER
    private MapSphereRulerComponent? _placeholder;
#endif
    public void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Map");
    }

    public void Track(Map3DFocusItem target, NetworkNode networkNode, ConnectionGraphNode connectionNode)
    {
        Id = target.AssociatedMapItem.SimGUID.ToString();
        _networkNode = networkNode;
        _target = target;

        // Relay sphere
        var sphereObject = Instantiate(ConnectionsRenderer.RulerSpherePrefab, gameObject.transform);
        sphereObject.name = "RulerSphere";
        sphereObject.transform.localPosition = Vector3.zero;
        var sphereComponent = sphereObject.AddComponent<MapSphereRulerComponent>();
        sphereComponent.Configure(connectionNode.MaxRange,
            networkNode.IsRelay ? null : Color.gray);

        // Simple relay placeholder
#if SHOW_RELAY_PLACEHOLDER
        if (networkNode.IsRelay)
        {
            var placeholderObject = Instantiate(ConnectionsRenderer.RulerSpherePrefab, gameObject.transform);
            placeholderObject.name = "RulerPlaceholder";
            placeholderObject.transform.localPosition = Vector3.zero;
            _placeholder = placeholderObject.AddComponent<MapSphereRulerComponent>();
            _placeholder.Configure(50_000, Color.gray);
        }
#endif

        _isTracking = true;
    }

    private void Update()
    {
        if (!_isTracking) return;
        if (_target == null)
        {
            ConnectionsRenderer.Instance.OnMapRulerDestroyed(this);
            return;
        }

#if SHOW_RELAY_PLACEHOLDER
        if (_networkNode?.IsRelay == true)
        {
            if (_networkNode.HasEnoughResources != true)
                _placeholder!.SetColor(Color.red);
            else
                _placeholder!.SetColor(Color.green);
        }
#endif

        transform.position = _target.transform.position;
    }

    public void OnDestroy()
    {
        if (!_isTracking) return;
        ConnectionsRenderer.Instance.OnMapRulerDestroyed(this, false);
    }
}