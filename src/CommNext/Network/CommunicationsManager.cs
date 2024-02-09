using BepInEx.Logging;
using KSP.Game;
using KSP.Sim;

namespace CommNext.Network;

public class CommunicationsManager
{
    private static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource("CommNext.CommunicationsManager");

    public static CommunicationsManager Instance { get; private set; } = new();

    public static CommNetManager CommNetManager => GameManager.Instance.Game.SessionManager.CommNetManager;
    public ConnectionGraph? ConnectionGraph { get; private set; }

    public void Initialize()
    {
        Logger.LogInfo("Initializing CommunicationsManager");
        ConnectionGraph = CommNetManager._connectionGraph;
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

        nodes = connectionGraph._allNodes;

        var prevIndexesNative = connectionGraph._previousIndices;
        prevIndexes = prevIndexesNative is not { IsCreated: true }
            ? Array.Empty<int>()
            : prevIndexesNative.ToArray<int>();

        return true;
    }
}