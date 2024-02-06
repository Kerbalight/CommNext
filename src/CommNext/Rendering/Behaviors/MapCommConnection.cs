using KSP.Map;
using UnityEngine;

namespace CommNext.Rendering.Behaviors;

public class MapCommConnection : MonoBehaviour
{
    private static readonly Material LineMaterial = new Material(Shader.Find("Sprites/Default"));
    
    public Map3DFocusItem SourceItem { get; set; }
    public Map3DFocusItem TargetItem { get; set; }
    
    private LineRenderer _lineRenderer;
    private bool _isConnected;
    
    /// <summary>
    /// Encodes the connection ID based on the source and target items,
    /// without considering the order of the items.
    /// </summary>
    public static string GetID(Map3DFocusItem source, Map3DFocusItem target)
    {
        var sourceId = source.AssociatedMapItem.SimGUID.ToString();
        var targetId = target.AssociatedMapItem.SimGUID.ToString();
        return string.Compare(sourceId, targetId, StringComparison.Ordinal) < 0 ? $"{sourceId}-{targetId}" : $"{targetId}-{sourceId}";
    }

    private void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Map");
        
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = 0.1f;
        _lineRenderer.endWidth = 0.1f;
        _lineRenderer.material = LineMaterial;
        // _lineRenderer.startColor = Color.green;
        // _lineRenderer.endColor = Color.green;
        var connectionGradient = new Gradient();
        connectionGradient.SetKeys(
            [new GradientColorKey(Color.green, 0.0f), new GradientColorKey(Color.green, 1.0f)],
            [new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.7f, 1.0f)]
        );
        _lineRenderer.colorGradient = connectionGradient;
    }
    
    public void Configure(Map3DFocusItem source, Map3DFocusItem target)
    {
        SourceItem = source;
        TargetItem = target;
        _isConnected = true;
    }

    public void Update()
    {
        if (!_isConnected) return;
        _lineRenderer.SetPositions(new Vector3[] { SourceItem.transform.position, TargetItem.transform.position });
    }
}