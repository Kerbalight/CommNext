using System.Reflection;
using BepInEx.Logging;
using KSP.Game;
using KSP.Sim;
using Unity.Collections;

namespace CommNext.Managers;

public class CommunicationsManager
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CommNext.CommunicationsManager");
    public static CommunicationsManager Instance { get; private set; } = new();
    
    public static CommNetManager CommNetManager => GameManager.Instance.Game.SessionManager.CommNetManager;
    public ConnectionGraph? ConnectionGraph { get; private set; }
    
    public void Initialize()
    {
        Logger.LogInfo("Initializing CommunicationsManager");
        ConnectionGraph = typeof(CommNetManager)
            .GetField("_connectionGraph", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(CommNetManager) as ConnectionGraph;
    }
    
    public bool TryGetConnectionGraphNodesAndIndexes(out List<ConnectionGraphNode>? nodes, out int[] prevIndexes)
    {
        var connectionGraph = ConnectionGraph;
        if (connectionGraph == null || connectionGraph.IsRunning)
        {
            nodes = null;
            prevIndexes = Array.Empty<int>();
            return false;
        }
        
        nodes = typeof(ConnectionGraph)
            .GetField("_allNodes", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(connectionGraph) as List<ConnectionGraphNode>;
        
        var prevIndexesNative = typeof(ConnectionGraph)
            .GetField("_previousIndices", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(connectionGraph) as NativeArray<int>?;
        
        prevIndexes = prevIndexesNative is not { IsCreated: true }
            ? Array.Empty<int>()
            : prevIndexesNative.ToArray<int>();
        
        return true;
    }
}