using AwesomeTechnologies;
using HarmonyLib;
using KSP.Game;
using KSP.Sim;
using Unity.Collections;

namespace CommNext.Patches;

public static class CommNetManagerPatches
{
    // This is the time in seconds that the CommNetManager will wait
    // before rebuilding the graph.
    private const float RebuildGraphTimerSeconds = 0.2f;
    
    private const string KerbinCommNetOriginName = "kerbin_CommNetOrigin";
    private const string KerbinSpaceCenterName = "kerbin_KSC_Object";
    
    [HarmonyPatch(typeof(CommNetManager), "SetSourceNode", [typeof(ConnectionGraphNode)])]
    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    public static void SetSourceNode(CommNetManager __instance, ref ConnectionGraphNode newSourceNode)
    {
        var simObj = GameManager.Instance.Game.UniverseModel.FindSimObject(newSourceNode.Owner);
        if (simObj is not { Name: KerbinCommNetOriginName }) return;

        var kscSimObj = GameManager.Instance.Game.UniverseModel.FindSimObjectByNameKey(KerbinSpaceCenterName);
        if (kscSimObj == null)
        {
            CommNextPlugin.Instance.SWLogger.LogWarning("KSC SimObject not found");
            return;
        }
        
        simObj.transform.Position = kscSimObj.transform.Position;
    }
    
    [HarmonyPatch(typeof(CommNetManager), "OnUpdate")]
    [HarmonyPostfix]
    // ReSharper disable InconsistentNaming
    public static void OnUpdateShortenTime(CommNetManager __instance, ref float ____timerRemaining)
    {
        if (____timerRemaining > RebuildGraphTimerSeconds)
            ____timerRemaining = RebuildGraphTimerSeconds;
    }
}