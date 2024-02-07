using System.Collections;
using System.Reflection;
using BepInEx.Logging;
using CommNext.Managers;
using CommNext.Rendering.Behaviors;
using KSP.Game;
using KSP.Map;
using KSP.Sim;
using KSP.Sim.impl;
using UnityEngine;

namespace CommNext.Rendering;

public class ConnectionsRenderer : MonoBehaviour
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CommNext.ConnectionsRenderer");
    
    public static ConnectionsRenderer Instance { get; private set; } = null!;

    private readonly Dictionary<string, MapCommConnection> _connections = new();

    private static Dictionary<IGGuid, Map3DFocusItem>? AllMapItems => _mapCore.map3D.AllMapSelectableItems;
    private static MapCore _mapCore = null!;
    
    private IEnumerator? _updateTask;
    
    private void Start()
    {
        Instance = this;
        _updateTask = RunUpdateConnectionsTask();
        StartCoroutine(_updateTask);
    }

    public void Initialize()
    {
        // TODO MapCore loading
        foreach (var connection in _connections.Values)
        {
            Destroy(connection.gameObject);
        }
        _connections.Clear();
    }
    
    
    private IEnumerator? RunUpdateConnectionsTask()
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
                    UpdateConnections(nodes!, prevIndexes);
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