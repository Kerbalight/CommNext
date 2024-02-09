using KSP.Map;
using Unity.Mathematics;
using UnityEngine;

namespace CommNext.Rendering.Behaviors;

public class MapSphereRulerComponent : MonoBehaviour, IMapComponent
{
    public string Id { get; set; } = null!;

    private double _range;
    private bool _isTracking;

    private MeshRenderer _meshRenderer = null!;
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private Map3DFocusItem? _target;

    public void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Map");
    }

    public void Track(Map3DFocusItem target, double range, Color? color)
    {
        Id = target.AssociatedMapItem.SimGUID.ToString();

        _meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (color.HasValue) _meshRenderer.material.SetColor(ColorID, color.Value);

        _range = range;
        _target = target;
        _isTracking = true;
    }

    // Update is called once per frame
    private void Update()
    {
        if (!_isTracking) return;
        if (_target == null)
        {
            ConnectionsRenderer.Instance.OnMapSphereRulerDestroyed(this);
            return;
        }

        var radius = (float)(_range / ConnectionsRenderer.Instance.GetMap3dScaleInv());
        var currentTransform = transform;
        currentTransform.localScale = new Vector3(radius, radius, radius);
        currentTransform.position = _target.transform.position;
    }

    public void OnDestroy()
    {
        if (!_isTracking) return;
        ConnectionsRenderer.Instance.OnMapSphereRulerDestroyed(this, false);
    }
}