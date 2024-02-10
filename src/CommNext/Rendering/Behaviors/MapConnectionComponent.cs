using KSP.Map;
using UnityEngine;

namespace CommNext.Rendering.Behaviors;

public class MapConnectionComponent : MonoBehaviour, IMapComponent
{
    public static Material? LineMaterial;

    public Map3DFocusItem? SourceItem { get; set; }
    public Map3DFocusItem? TargetItem { get; set; }

    private LineRenderer _lineRenderer = null!;

    public string Id { get; set; } = null!;

    private bool _isConnected;
    private static readonly int MainColor = Shader.PropertyToID("_Color");

    /// <summary>
    /// Encodes the connection ID based on the source and target items,
    /// considering the order of the items.
    /// </summary>
    public static string GetID(Map3DFocusItem source, Map3DFocusItem target)
    {
        var sourceId = source.AssociatedMapItem.SimGUID.ToString();
        var targetId = target.AssociatedMapItem.SimGUID.ToString();
        return $"{sourceId}-{targetId}";
        // return string.Compare(sourceId, targetId, StringComparison.Ordinal) < 0
        //     ? $"{sourceId}-{targetId}"
        //     : $"{targetId}-{sourceId}";
    }

    private void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Map");

        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 10;
        _lineRenderer.startWidth = 0.045f;
        _lineRenderer.endWidth = 0.04f;
        _lineRenderer.material = LineMaterial;
        // _lineRenderer.startColor = Color.green;
        // _lineRenderer.endColor = Color.green;
        var connectionGradient = new Gradient();
        connectionGradient.SetKeys(
            [new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.green, 1.0f)],
            [new GradientAlphaKey(0.9f, 0.0f), new GradientAlphaKey(0.1f, 1.0f)]
        );
        _lineRenderer.colorGradient = connectionGradient;
    }

    public void Configure(Map3DFocusItem source, Map3DFocusItem target)
    {
        SourceItem = source;
        TargetItem = target;
        Id = GetID(source, target);
        _isConnected = true;
    }

    public void Update()
    {
        if (!_isConnected) return;

        if (SourceItem == null || TargetItem == null)
        {
            ConnectionsRenderer.Instance.OnMapConnectionDestroyed(this);
            return;
        }

        var positions = new Vector3[10];
        for (var i = 0; i < 10; i++)
        {
            var t = i / 9f;
            positions[i] = Vector3.Lerp(SourceItem.transform.position, TargetItem.transform.position, t);
        }

        _lineRenderer.SetPositions(positions);
    }

    public void OnDestroy()
    {
        if (!_isConnected) return;
        ConnectionsRenderer.Instance.OnMapConnectionDestroyed(this, false);
    }
}