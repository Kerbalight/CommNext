using System.Collections;
using System.Reflection;
using BepInEx.Logging;
using CommNext.Managers;
using CommNext.Network;
using CommNext.Network.Bands;
using CommNext.Network.Compute;
using CommNext.Patches;
using CommNext.Rendering.Behaviors;
using CommNext.UI;
using CommNext.Unity.Runtime;
using KSP.Game;
using KSP.Map;
using KSP.Sim;
using KSP.Sim.impl;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace CommNext.Rendering;

public class ConnectionsRenderer : MonoBehaviour
{
    private static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource("CommNext.ConnectionsRenderer");

    public static GameObject RulerSpherePrefab = null!;
    public static GameObject TestSpherePrefab = null!;

    public static ConnectionsRenderer Instance { get; private set; } = null!;

    private readonly Dictionary<string, MapConnectionComponent> _connections = new();
    private readonly Dictionary<string, MapRulerComponent> _rulers = new();
    private readonly Dictionary<string, MapConnectionComponent> _reportConnections = new();

    private static Dictionary<IGGuid, Map3DFocusItem>? AllMapItems => _mapCore.map3D.AllMapSelectableItems;
    private static MapCore _mapCore = null!;

    private GameObject _debugPositionsObject = null!;
    private GameObject _debugPositionsBodyObject = null!;

    private IEnumerator? _updateTask;

    private ConnectionsDisplayMode _connectionsDisplayMode = ConnectionsDisplayMode.Lines;
    private bool _isRulersEnabled = true;

    public bool IsConnectionsEnabled => _connectionsDisplayMode != ConnectionsDisplayMode.None;

    private int? _selectedBandIndex;
    private Color? _selectedBandColor;

    public int? SelectedBandIndex
    {
        get => _selectedBandIndex;
        set
        {
            if (_selectedBandIndex == value) return;

            _selectedBandIndex = value;
            if (_selectedBandIndex.HasValue)
            {
                _selectedBandColor = NetworkBands.Instance.AllBands[_selectedBandIndex.Value].Color;
                IsRulersEnabled = true;
            }
            else
            {
                _selectedBandColor = null;
                IsRulersEnabled = false;
            }
        }
    }

    /// <summary>
    /// Connections are the lines between the nodes (vessels, ground stations, etc).
    /// </summary>
    public ConnectionsDisplayMode ConnectionsDisplayMode
    {
        get => _connectionsDisplayMode;
        set
        {
            Logger.LogInfo("Setting ConnectionsDisplayMode " + value);
            _connectionsDisplayMode = value;
            ClearConnections();
            ToggleUpdateTaskIfNeeded();
        }
    }

    /// <summary>
    /// Rulers are the spheres that show the range of the nodes
    /// (vessels, ground stations, etc).
    /// </summary>
    public bool IsRulersEnabled
    {
        get => _isRulersEnabled;
        set
        {
            Logger.LogInfo("Setting IsRulersEnabled to " + value);
            _isRulersEnabled = value;
            ClearRulers();
            ToggleUpdateTaskIfNeeded();
        }
    }

    private VesselComponent? _reportVessel;

    public VesselComponent? ReportVessel
    {
        get => _reportVessel;
        set
        {
            ClearReportConnections();
            _reportVessel = value;
        }
    }

    private void Start()
    {
        Instance = this;
    }

    private void ToggleUpdateTaskIfNeeded()
    {
        // We want to trigger them right away so that UI is updated
        // right after the UI click.
        // if (IsConnectionsEnabled || _isRulersEnabled) UpdateRenderings();
        var shouldBeRunning = IsConnectionsEnabled || _isRulersEnabled || ReportVessel != null;

        switch (shouldBeRunning)
        {
            case true when _updateTask == null:
                Logger.LogInfo("Starting update task");
                _updateTask = RunUpdateTask();
                StartCoroutine(_updateTask);
                break;

            case false when _updateTask != null:
                Logger.LogInfo("Stopping update task");
                StopCoroutine(_updateTask);
                _updateTask = null;

                ClearConnections();
                ClearReportConnections();
                ClearRulers();
                break;
        }
    }

    public void Initialize()
    {
        Logger.LogInfo("Initializing ConnectionsRenderer");
        // TODO MapCore loading
        ClearConnections();
        ClearReportConnections();
        ClearRulers();

        ConnectionsDisplayMode = ConnectionsDisplayMode.Lines;
    }

    public void ClearConnections()
    {
        foreach (var connection in _connections.Values) Destroy(connection.gameObject);
        _connections.Clear();
    }

    public void ClearReportConnections()
    {
        foreach (var connection in _reportConnections.Values) Destroy(connection.gameObject);
        _reportConnections.Clear();
    }

    public void ClearRulers()
    {
        foreach (var ruler in _rulers.Values) Destroy(ruler.gameObject);
        _rulers.Clear();
    }

    private IEnumerator? RunUpdateTask()
    {
        while (true)
        {
            UpdateRenderings();
            yield return new WaitForSeconds(0.25f);
        }
        // ReSharper disable once IteratorNeverReturns
    }

    private void UpdateRenderings()
    {
        if (!MessageListener.IsInMapView ||
            !NetworkManager.Instance.TryGetConnectionGraphNodesAndIndexes(
                out var nodes,
                out var prevIndexes,
                out var networkJobConnections) ||
            nodes == null || !networkJobConnections.HasValue) return;

        try
        {
            if (IsConnectionsEnabled) UpdateConnections(nodes!, prevIndexes, networkJobConnections.Value);
            if (ReportVessel != null) UpdateReportConnections();
            if (_isRulersEnabled) UpdateRulers(nodes!, prevIndexes);
        }
        catch (Exception e)
        {
            Logger.LogError("Error updating connections: " + e);
        }
    }

    private void UpdateConnections(List<ConnectionGraphNode> nodes, int[] prevIndexes,
        NativeArray<NetworkJobConnection> networkJobConnections)
    {
        // TODO Add some events-based logic.
        if (!GameManager.Instance.Game.Map.TryGetMapCore(out _mapCore))
            // Logger.LogError("MapCore not found");
            return;

#if DEBUG_MAP_POSITIONS
        UpdateDebugPositions();
#endif

        HashSet<(int, int)>? activeVesselPath = null;

        if (_connectionsDisplayMode == ConnectionsDisplayMode.Active
            && GameManager.Instance.Game.ViewController.TryGetActiveSimVessel(out var activeVessel)
            && !NetworkManager.Instance.TryGetNetworkPath(activeVessel.GlobalId, nodes, prevIndexes,
                out activeVesselPath))
        {
            // We want to display only the path of the active vessel.
            activeVesselPath = [];
            Logger.LogWarning($"No active vessel path found for {activeVessel.GlobalId}");
        }


        List<string> keepIds = [];

        for (var i = 0; i < prevIndexes.Length; i++)
        {
            var sourceIndex = prevIndexes[i];
            var targetIndex = i;
            if (sourceIndex < 0 || sourceIndex >= nodes.Count) continue;

            var sourceNode = nodes[sourceIndex];
            var targetNode = nodes[targetIndex];
            if (sourceNode == null || targetNode == null) continue;

            // Highlight only the active vessel path.
            if (activeVesselPath != null && !activeVesselPath.Contains((sourceIndex, targetIndex))) continue;

            var sourceItem = GetMapItem(sourceNode);
            var targetItem = GetMapItem(targetNode);
            if (sourceItem == null || targetItem == null) continue;

            var networkJobConnection = networkJobConnections[targetIndex * nodes.Count + sourceIndex];

            var connectionId = MapConnectionComponent.GetID(sourceItem, targetItem);
            if (!_connections.TryGetValue(connectionId, out var connection))
            {
                var sourceNetworkNode = NetworkManager.Instance.Nodes[sourceNode.Owner];
                var targetNetworkNode = NetworkManager.Instance.Nodes[targetNode.Owner];

                var connectionObject = new GameObject($"MapCommConnection_{connectionId}");
                connectionObject.transform.SetParent(_mapCore.map3D.transform);
                connection = connectionObject.AddComponent<MapConnectionComponent>();
                connection.Configure(
                    sourceItem, targetItem,
                    sourceNetworkNode, targetNetworkNode,
                    sourceNode, targetNode
                );
                _connections.Add(connectionId, connection);
            }

            keepIds.Add(connectionId);
            connection.Band = networkJobConnection.HasMatchingBand
                ? NetworkBands.Instance.AllBands[networkJobConnection.SelectedBand]
                : null;
        }

        var removeIds = _connections.Keys.Except(keepIds).ToList();
        foreach (var connectionId in removeIds)
        {
            var connection = _connections[connectionId];
            Destroy(connection.gameObject);
            _connections.Remove(connectionId);
        }
    }

    private void UpdateReportConnections()
    {
        if (ReportVessel == null) return;
        if (!NetworkManager.Instance.Nodes.TryGetValue(ReportVessel.GlobalId, out var vesselNode))
        {
            Logger.LogWarning($"Vessel node not found for {ReportVessel.GlobalId}. Untracking.");
            ReportVessel = null;
            return;
        }

        // I don't like this.
        var vesselConnections = MainUIManager.Instance.VesselReportWindow!.ReportVesselConnections;

        var keepIds = new List<string>();
        foreach (var connection in vesselConnections)
        {
            var sourceItem = GetMapItem(connection.SourceNode);
            var targetItem = GetMapItem(connection.TargetNode);
            if (sourceItem == null || targetItem == null) continue;

            var connectionId = MapConnectionComponent.GetID(sourceItem, targetItem);
            if (!_reportConnections.TryGetValue(connectionId, out var connectionComponent))
            {
                var connectionObject = new GameObject($"MapCommReportConnection_{connectionId}");
                connectionObject.transform.SetParent(_mapCore.map3D.transform);
                connectionComponent = connectionObject.AddComponent<MapConnectionComponent>();
                connectionComponent.ConfigureForReport(
                    sourceItem, targetItem,
                    connection, vesselNode
                );
                _reportConnections.Add(connectionId, connectionComponent);
            }
            else
            {
                connectionComponent.SetNetworkConnection(connection);
            }

            keepIds.Add(connectionId);
        }

        var removeIds = _reportConnections.Keys.Except(keepIds).ToList();
        foreach (var connectionId in removeIds)
        {
            var connection = _reportConnections[connectionId];
            Destroy(connection.gameObject);
            _reportConnections.Remove(connectionId);
        }
    }

    private void UpdateRulers(List<ConnectionGraphNode> nodes, int[] prevIndexes)
    {
        if (!GameManager.Instance.Game.Map.TryGetMapCore(out _mapCore))
            // Logger.LogError("MapCore not found");
            return;

        var keepIds = new List<string>();
        for (var i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node == null) continue;
            // if (prevIndexes[i] < 0 || prevIndexes[i] >= nodes.Count) continue;
            // if (node.MaxRange <= 0) continue;
            var item = GetMapItem(node);
            if (item == null) continue;

            // We want to show only relays as rulers.
            var networkNode = NetworkManager.Instance.Nodes.GetValueOrDefault(node.Owner);
            if (networkNode is not { IsRelay: true }) continue;

            if (_selectedBandIndex.HasValue && networkNode.BandRanges[_selectedBandIndex.Value] <= 0) continue;

            var isConnected = prevIndexes[i] >= 0;
            var connectedColor = _selectedBandColor;
            var commRange = _selectedBandIndex.HasValue
                ? networkNode.BandRanges[_selectedBandIndex.Value]
                : node.MaxRange;

            if (!_rulers.TryGetValue(item.AssociatedMapItem.SimGUID.ToString(), out var ruler))
            {
                var rulerObject =
                    new GameObject($"Ruler_{item.AssociatedMapItem.ItemName}_{item.AssociatedMapItem.SimGUID}");
                rulerObject.transform.SetParent(_mapCore.map3D.transform);
                ruler = rulerObject.AddComponent<MapRulerComponent>();
                // We need to pass all the available data to the ruler right now,
                // to avoid glitches when the ruler is rendered
                ruler.Track(item, isConnected, networkNode, node, connectedColor, commRange);
                _rulers.Add(ruler.Id, ruler);
            }
            else
            {
                // Update the ruler
                ruler.IsConnected = isConnected;
                ruler.ConnectedColor = connectedColor;
                ruler.CommRange = commRange;
            }

            keepIds.Add(ruler.Id);
        }

        var removeIds = _rulers.Keys.Except(keepIds).ToList();
        foreach (var rulerId in removeIds)
        {
            if (!_rulers.TryGetValue(rulerId, out var rulerComponent)) continue;
            Destroy(rulerComponent.gameObject);
            _rulers.Remove(rulerId);
        }
    }

    public static double GetMap3dScaleInv()
    {
        return _mapCore.map3D.GetSpaceProvider().Map3DScaleInv;
    }

    /// <summary>
    /// If MapConnectionComponent Source or Target item are destroyed, we need to
    /// remove the connection.
    /// 
    /// In general, after the `OnDestroy` inside the IMapComponent, it's absolutely
    /// necessary to call this method and to Destroy the GameObject.
    /// </summary>
    public void OnMapConnectionDestroyed(MapConnectionComponent connection)
    {
        // This is wanted.
        if (connection.gameObject != null) Destroy(connection.gameObject);
        if (!_connections.ContainsKey(connection.Id)) return;
        _connections.Remove(connection.Id);
    }

    public void OnMapReportConnectionDestroyed(MapConnectionComponent connection)
    {
        // This is wanted.
        if (connection.gameObject != null) Destroy(connection.gameObject);
        if (!_reportConnections.ContainsKey(connection.Id)) return;
        _reportConnections.Remove(connection.Id);
    }

    public void OnMapRulerDestroyed(MapRulerComponent ruler)
    {
        if (ruler.gameObject != null) Destroy(ruler.gameObject);
        if (!_rulers.ContainsKey(ruler.Id)) return;
        _rulers.Remove(ruler.Id);
    }

    private static Map3DFocusItem? GetMapItem(ConnectionGraphNode sourceNode)
    {
        return GetMapItem(sourceNode.Owner);
    }

    private static Map3DFocusItem? GetMapItem(IGGuid sourceGuid)
    {
        if (AllMapItems == null) return null;
        // Replace the source GUID with the KSC GUID. 
        // Control Center is the source of all connections, but it's not a map item.
        if (sourceGuid == NetworkManager.Instance.CommNetManager.GetSourceNode().Owner)
            sourceGuid = _mapCore.KSCGUID;
        return AllMapItems.GetValueOrDefault(sourceGuid);
    }

    /// <summary>
    /// Focus the vessel node in the Map view.
    /// </summary>
    public void FocusOnMap(IGGuid nodeOwner)
    {
        var item = GetMapItem(nodeOwner);
        if (item == null)
        {
            Logger.LogWarning($"Map item not found for {nodeOwner}");
            return;
        }

        item.FocusSimObject();
    }

    /// <summary>
    /// See `MapUISelectableItem.HandleVesselControl`.
    /// </summary>
    public void ControlVesselOnMap(NetworkNode node)
    {
        var item = GetMapItem(node.Owner);
        if (item == null)
        {
            Logger.LogWarning($"Map item not found for {node.Owner}");
            return;
        }

        item.ControlVessel();
    }

#if DEBUG_MAP_POSITIONS
    private void UpdateDebugPositions()
    {
        var kscItem = AllMapItems.GetValueOrDefault(_mapCore.KSCGUID);
        if (_debugPositionsObject == null)
        {
            _debugPositionsObject = new GameObject();
            _debugPositionsObject.name = "DebugPositions";
            _debugPositionsObject.transform.SetParent(kscItem.transform);
            _debugPositionsObject.layer = LayerMask.NameToLayer("Map");
            _debugPositionsObject.transform.localPosition = Vector3.zero;
            var lineRenderer = _debugPositionsObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.magenta;
            lineRenderer.startWidth = 0.03f;
            lineRenderer.endWidth = 0.03f;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = false;

            _debugPositionsBodyObject = Instantiate(TestSpherePrefab, kscItem.transform);
            _debugPositionsBodyObject.name = "DebugPositionsBody";
            // _debugPositionsBodyObject.transform.SetParent(_mapCore.map3D.transform);
            _debugPositionsBodyObject.layer = LayerMask.NameToLayer("Map");
        }

        var commNetOrigin = GameManager.Instance.Game.UniverseModel.FindCommNetOrigin();
        var scaleInvFactor = 1_000_000f;

        var source = ConnectionGraphPatches.debugPositions[0] / scaleInvFactor; // / GetMap3dScaleInv();
        var target = ConnectionGraphPatches.debugPositions[1] / scaleInvFactor; // / GetMap3dScaleInv();
        var body = ConnectionGraphPatches.debugPositions[2] / scaleInvFactor; // / GetMap3dScaleInv();

        _debugPositionsObject.GetComponent<LineRenderer>().SetPositions(new[]
        {
            new Vector3((float)target.x, (float)target.y, (float)target.z),
            new Vector3((float)source.x, (float)source.y, (float)source.z)
        });


        _debugPositionsBodyObject.transform.localPosition =
            new Vector3((float)body.x, (float)body.y, (float)body.z);
        _debugPositionsBodyObject.transform.localScale = new Vector3((float)(500000 / scaleInvFactor),
            (float)(500000 / scaleInvFactor), (float)(500000 / scaleInvFactor));
    }
#endif
}