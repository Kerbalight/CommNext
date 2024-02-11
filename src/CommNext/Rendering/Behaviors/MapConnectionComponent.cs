//#define INGAME_COLOR_UPDATE

using CommNext.Network;
using KSP.Map;
using KSP.Sim;
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

    // private static readonly Color LinkColor = new(0.440f, 1.436f, 0.606f, 1.0f);
    private static readonly Color LinkColor = new(0.212f, 0.765f, 0.345f, 1.0f);

    // private static readonly Color RelayColor = new(0.43f, 1.755f, 1.619f, 1.0f);
    private static readonly Color RelayColor = new(0.215f, 0.858f, 0.847f, 1.0f);

    private MaterialPropertyBlock _materialPropertyBlock = null!;

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
    }

    public void Configure(Map3DFocusItem source, Map3DFocusItem target,
        NetworkNode sourceNetworkNode, NetworkNode targetNetworkNode,
        ConnectionGraphNode sourceNode, ConnectionGraphNode targetNode)
    {
        SourceItem = source;
        TargetItem = target;
        Id = GetID(source, target);

        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 10;
        _lineRenderer.startWidth = 0.045f;
        _lineRenderer.endWidth = 0.04f;
        _lineRenderer.material = LineMaterial;
        var connectionGradient = new Gradient();
        connectionGradient.SetKeys(
            // Color is set in MainColor some lines below
            [new GradientColorKey(Color.black, 0.0f), new GradientColorKey(Color.black, 1.0f)],
            [new GradientAlphaKey(0.8f, 0.0f), new GradientAlphaKey(0.1f, 1.0f)]
        );
        _lineRenderer.colorGradient = connectionGradient;

        _materialPropertyBlock = new MaterialPropertyBlock();
        var color = sourceNetworkNode.IsRelay && targetNetworkNode.IsRelay ? RelayColor : LinkColor;
        _materialPropertyBlock.SetColor(MainColor, color);
        _lineRenderer.SetPropertyBlock(_materialPropertyBlock);

        _isConnected = true;

        UpdateLinePositions();
    }

    private void UpdateLinePositions()
    {
        var positions = new Vector3[10];
        for (var i = 0; i < 10; i++)
        {
            var t = i / 9f;
            positions[i] = Vector3.Lerp(SourceItem!.transform.position, TargetItem!.transform.position, t);
        }

        _lineRenderer.SetPositions(positions);
    }

    public void Update()
    {
        if (!_isConnected) return;

        if (SourceItem == null || TargetItem == null)
        {
            ConnectionsRenderer.Instance.OnMapConnectionDestroyed(this);
            return;
        }

        UpdateLinePositions();
    }

    public void OnDestroy()
    {
        if (!_isConnected) return;
        ConnectionsRenderer.Instance.OnMapConnectionDestroyed(this, false);
    }
}