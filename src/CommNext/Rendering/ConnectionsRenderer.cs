using System.Collections;
using System.Reflection;
using BepInEx.Logging;
using CommNext.Managers;
using CommNext.Rendering.Behaviors;
using CommNext.Unity.Runtime;
using KSP.Game;
using KSP.Map;
using KSP.Sim;
using KSP.Sim.impl;
using Unity.Mathematics;
using UnityEngine;

namespace CommNext.Rendering;

public class ConnectionsRenderer : MonoBehaviour
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CommNext.ConnectionsRenderer");
    
    public static GameObject RulerSpherePrefab = null!;
    
    public static ConnectionsRenderer Instance { get; private set; } = null!;

    private readonly Dictionary<string, MapCommConnection> _connections = new();
    private readonly Dictionary<string, RulerSphereComponent> _rulers = new();

    private static Dictionary<IGGuid, Map3DFocusItem>? AllMapItems => _mapCore.map3D.AllMapSelectableItems;
    private static MapCore _mapCore = null!;
    
    private IEnumerator? _updateTask;

    private bool _isConnectionsEnabled = true;
    private bool _isRulersEnabled = true;

    public bool IsConnectionsEnabled
    {
        get => _isConnectionsEnabled;
        set
        {
            _isConnectionsEnabled = value;
            ClearConnections();
            ToggleUpdateTaskIfNeeded();
        }
    }
    
    public bool IsRulersEnabled
    {
        get => _isRulersEnabled;
        set
        {
            _isRulersEnabled = value;
            ClearRulers();
            ToggleUpdateTaskIfNeeded();
        }
    }
    
    private void Start()
    {
        Instance = this;
    }

    private void ToggleUpdateTaskIfNeeded()
    {
        switch (_isConnectionsEnabled || _isRulersEnabled)
        {
            case true when _updateTask == null:
                _updateTask = RunUpdateTask();
                StartCoroutine(_updateTask);
                break;
                
            case false when _updateTask != null:
                StopCoroutine(_updateTask);
                _updateTask = null;
                
                ClearConnections();
                ClearRulers();
                break;
        }
    }

    public void Initialize()
    {
        // TODO MapCore loading
        ClearConnections();
        ClearRulers();
        
        IsConnectionsEnabled = true;
    }
    
    public void ClearConnections()
    {
        foreach (var connection in _connections.Values)
        {
            Destroy(connection.gameObject);
        }
        _connections.Clear();
    }
    
    public void ClearRulers()
    {
        foreach (var ruler in _rulers.Values)
        {
            Destroy(ruler.gameObject);
        }
        _rulers.Clear();
    }
    
    private IEnumerator? RunUpdateTask()
    {
        while (true)
        {
            // TODO Start/Stop tasks based on the state of the game
            if (MessageListener.IsInMapView &&
                CommunicationsManager.Instance.TryGetConnectionGraphNodesAndIndexes(out var nodes,
                    out var prevIndexes) &&
                nodes != null)
            {
                try
                {
                    if (_isConnectionsEnabled) UpdateConnections(nodes!, prevIndexes);
                    if (_isRulersEnabled) UpdateRulers(nodes!, prevIndexes);
                }
                catch (Exception e)
                {
                    Logger.LogError("Error updating connections: " + e);
                }
            }

            yield return new WaitForSeconds(0.5f);
        }
        // ReSharper disable once IteratorNeverReturns
    }

    private void UpdateConnections(List<ConnectionGraphNode> nodes, int[] prevIndexes)
    {
        // TODO Add some events-based logic.
        if (!GameManager.Instance.Game.Map.TryGetMapCore(out _mapCore))
        {
            // Logger.LogError("MapCore not found");
            return;
        }
        
        List<string> keepIds = [];
        
        for (var i = 0; i < prevIndexes.Length; i++) {
            var sourceIndex = prevIndexes[i];
            var targetIndex = i;
            if (sourceIndex < 0 || sourceIndex >= nodes.Count) continue;
            
            var sourceNode = nodes[sourceIndex];
            var targetNode = nodes[targetIndex];
            if (sourceNode == null || targetNode == null) continue;
            
            var sourceItem = GetMapItem(sourceNode);
            var targetItem = GetMapItem(targetNode);
            if (sourceItem == null || targetItem == null) continue;

            var connectionId = MapCommConnection.GetID(sourceItem, targetItem);
            if (_connections.TryGetValue(connectionId, out var connection))
            {
                keepIds.Add(connectionId);
            }
            else
            {
                var connectionObject = new GameObject($"MapCommConnection_{connectionId}");
                connectionObject.transform.SetParent(_mapCore.map3D.transform);
                connection = connectionObject.AddComponent<MapCommConnection>();
                connection.Configure(sourceItem, targetItem);
                _connections.Add(connectionId, connection);
                keepIds.Add(connectionId);
            }
        }
        
        var removeIds = _connections.Keys.Except(keepIds).ToList();
        foreach (var connectionId in removeIds)
        {
            var connection = _connections[connectionId];
            Destroy(connection.gameObject);
            _connections.Remove(connectionId);
        }
    }

    private void UpdateRulers(List<ConnectionGraphNode> nodes, int[] prevIndexes)
    {
        var keepIds = new List<string>();
        for (var i=0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (prevIndexes[i] < 0 || prevIndexes[i] >= nodes.Count) continue;
            if (node == null) continue;
            if (node.MaxRange <= 0) continue;
            var item = GetMapItem(node);
            if (item == null) continue;
            if (_rulers.TryGetValue(item.AssociatedMapItem.SimGUID.ToString(), out var ruler))
            {
                keepIds.Add(item.AssociatedMapItem.SimGUID.ToString());
            }
            else
            {
                var rulerObject = Instantiate(RulerSpherePrefab, _mapCore.map3D.transform);
                rulerObject.name = $"Ruler_{item.AssociatedMapItem.ItemName}_{item.AssociatedMapItem.SimGUID}";
                ruler = rulerObject.AddComponent<RulerSphereComponent>();
                ruler.Track(item, node.MaxRange, null, OnRulerMissingNode);
                _rulers.Add(item.AssociatedMapItem.SimGUID.ToString(), ruler);
                
                keepIds.Add(item.AssociatedMapItem.SimGUID.ToString());
            }
        }
        
        var removeIds = _rulers.Keys.Except(keepIds).ToList();
        foreach (var rulerId in removeIds)
        {
            if (!_rulers.TryGetValue(rulerId, out var rulerComponent)) continue;
            Destroy(rulerComponent.gameObject);
            _rulers.Remove(rulerId);
        }
    }
    
    public double GetMap3dScaleInv()
    {
        return _mapCore.map3D.GetSpaceProvider().Map3DScaleInv;
    }
    
    private void OnRulerMissingNode(RulerSphereComponent ruler)
    {
        var rulerId = ruler.gameObject.name.Replace("Ruler_", "");
        if (!_rulers.TryGetValue(rulerId, out var rulerComponent)) return;
        Destroy(rulerComponent.gameObject);
        _rulers.Remove(rulerId);
    }

    /// <summary>
    /// If MapCommConnection Source or Target item are destroyed, we need to
    /// remove the connection.
    /// </summary>
    public void OnMapItemDestroyed(MapCommConnection connection)
    {
       Destroy(connection.gameObject);
       _connections.Remove(connection.Id);
    }

    private static Map3DFocusItem? GetMapItem(ConnectionGraphNode sourceNode)
    {
        if (AllMapItems == null) return null;
        var sourceGuid = sourceNode.Owner;
        // Replace the source GUID with the KSC GUID. 
        // Control Center is the source of all connections, but it's not a map item.
        if (sourceGuid == CommunicationsManager.CommNetManager.GetSourceNode().Owner)
        {
            sourceGuid = _mapCore.KSCGUID;
        }
        return AllMapItems.GetValueOrDefault(sourceGuid);
    }
}