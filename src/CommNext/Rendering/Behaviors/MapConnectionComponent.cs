//#define INGAME_COLOR_UPDATE

using CommNext.Network;
using CommNext.Network.Bands;
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

    private bool _isReportConnection;
    private NetworkConnection? _networkConnection;
    private bool _isSource;

    public NetworkBand? Band { get; set; }
    private bool _isRelay;

    public string Id { get; set; } = null!;

    private bool _isConnected;

    private static readonly Color LinkColor = new(0.212f, 0.765f, 0.345f, 1.0f);
    private static readonly Color RelayColor = new(0.215f, 0.858f, 0.847f, 1.0f);

    private static readonly Color InboundColor = new(0.19f, 0.35f, 1f, 1f);
    private static readonly Color OutboundColor = new(0.71f, 0.35f, 0.86f, 1f);
    private static readonly Color DisconnectedColor = new(1f, 0.36f, 0.28f, 1f);

    private static readonly int MainColor = Shader.PropertyToID("_Color");
    private static readonly int ReportProperty = Shader.PropertyToID("_Report");
    private static readonly int LengthProperty = Shader.PropertyToID("_Length");
    private static readonly int ConnectedProperty = Shader.PropertyToID("_Connected");
    private MaterialPropertyBlock _materialPropertyBlock = null!;

    public static float ScalingFactor = 10000.0f;

    /// <summary>
    /// Encodes the connection ID based on the source and target items,
    /// considering the order of the items.
    /// </summary>
    public static string GetID(Map3DFocusItem source, Map3DFocusItem target)
    {
        var sourceId = source.AssociatedMapItem.SimGUID.ToString();
        var targetId = target.AssociatedMapItem.SimGUID.ToString();
        return $"{sourceId}-{targetId}";
    }

    private void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Map");
    }

    private void InternalConfigure(
        Map3DFocusItem source, Map3DFocusItem target,
        NetworkNode sourceNetworkNode, NetworkNode targetNetworkNode,
        ConnectionGraphNode sourceNode, ConnectionGraphNode targetNode)
    {
        SourceItem = source;
        TargetItem = target;
        Id = GetID(source, target);

        // If both nodes are relays, then this connection is a relay connection.
        // We use this information to determine the color of the connection.
        _isRelay = sourceNetworkNode.IsRelay && targetNetworkNode.IsRelay;

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

        _isConnected = true;
    }

    /// <summary>
    /// Highlights a link between two NetworkNodes on the map.
    /// </summary>
    public void Configure(Map3DFocusItem source, Map3DFocusItem target,
        NetworkNode sourceNetworkNode, NetworkNode targetNetworkNode,
        ConnectionGraphNode sourceNode, ConnectionGraphNode targetNode)
    {
        InternalConfigure(source, target, sourceNetworkNode, targetNetworkNode, sourceNode, targetNode);

        _materialPropertyBlock = new MaterialPropertyBlock();
        var color = _isRelay ? RelayColor : LinkColor;
        _materialPropertyBlock.SetColor(MainColor, color);
        _lineRenderer.SetPropertyBlock(_materialPropertyBlock);

        UpdateLinePositions();
    }

    /// <summary>
    /// Highlights a NetworkConnection on the map.
    /// It depends on the current NetworkNode to determine the color of the connection,
    /// if it's inbound or outbound.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    /// <param name="networkConnection"></param>
    /// <param name="currentNode"></param>
    public void ConfigureForReport(
        Map3DFocusItem source,
        Map3DFocusItem target,
        NetworkConnection networkConnection,
        NetworkNode currentNode)
    {
        InternalConfigure(source, target,
            networkConnection.Source, networkConnection.Target,
            networkConnection.SourceNode, networkConnection.TargetNode);

        _isReportConnection = true;
        _networkConnection = networkConnection;
        _isSource = networkConnection.IsSource(currentNode);

        _lineRenderer.startWidth = 0.3f;
        _lineRenderer.endWidth = 0.3f;

        var connectionGradient = new Gradient();
        connectionGradient.SetKeys(
            // Color is set in MainColor some lines below
            [new GradientColorKey(Color.black, 0.0f), new GradientColorKey(Color.black, 1.0f)],
            // We want quasi-constant alpha for these lines, since the direction is already
            // highlighted by the arrows.
            [new GradientAlphaKey(0.8f, 0.0f), new GradientAlphaKey(0.7f, 1.0f)]
        );
        _lineRenderer.colorGradient = connectionGradient;

        // Set fixed properties, other shader properties will be updated in UpdateLinePositions
        _materialPropertyBlock = new MaterialPropertyBlock();
        _materialPropertyBlock.SetFloat(ReportProperty, 1.0f);
        _lineRenderer.SetPropertyBlock(_materialPropertyBlock);

        UpdateLinePositions();
    }

    public void SetNetworkConnection(NetworkConnection networkConnection)
    {
        _networkConnection = networkConnection;
        // We assume "_isSource" cannot change since it's bound to active vessel.
        // Changing it means for sure destroying this Component and creating a new one.
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

        if (_isReportConnection)
        {
            var distance = Vector3.Distance(SourceItem!.transform.position, TargetItem!.transform.position);

            var color = !_networkConnection!.IsConnected ? DisconnectedColor :
                _isSource ? OutboundColor : InboundColor;

            // If the distance is too big, we need to adjust the scaling factor since
            // floating point precision is not enough.
            // This means the arrow will be bigger for longer distances, but it's not a problem
            // since this happens only when zooming in the map near the connection.
            // I don't like it however, so I'm commenting it out for now.
            var adjustedScalingFactor = /*distance > 1_000_000f ? ScalingFactor * 100f :
                distance > 100_000f ? ScalingFactor * 10f :*/ ScalingFactor;

            _materialPropertyBlock.SetColor(MainColor, color);
            _materialPropertyBlock.SetFloat(ConnectedProperty, _networkConnection.IsConnected ? 1.0f : 0.0f);
            _materialPropertyBlock.SetFloat(LengthProperty, distance / adjustedScalingFactor);
            _lineRenderer.SetPropertyBlock(_materialPropertyBlock);
        }
        else
        {
            var color = Band?.Color ?? (_isRelay ? RelayColor : LinkColor);
            _materialPropertyBlock.SetColor(MainColor, color);
            _lineRenderer.SetPropertyBlock(_materialPropertyBlock);
        }
    }

    public void Update()
    {
        if (!_isConnected) return;

        if (SourceItem == null || TargetItem == null)
        {
            if (_isReportConnection) ConnectionsRenderer.Instance.OnMapReportConnectionDestroyed(this);
            else ConnectionsRenderer.Instance.OnMapConnectionDestroyed(this);
            return;
        }

        UpdateLinePositions();
    }

    public void OnDestroy()
    {
        if (!_isConnected) return;
        if (_isReportConnection) ConnectionsRenderer.Instance.OnMapReportConnectionDestroyed(this);
        else ConnectionsRenderer.Instance.OnMapConnectionDestroyed(this);
    }
}