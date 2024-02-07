using HarmonyLib;
using KSP.Game;
using Unity.Collections;

namespace CommNext.Patches;

public static class CommNetManagerPatches
{
    // This is the time in seconds that the CommNetManager will wait
    // before rebuilding the graph.
    private const float RebuildGraphTimerSeconds = 0.2f;
    
    [HarmonyPatch(typeof(CommNetManager), "OnUpdate")]
    [HarmonyPostfix]
    // ReSharper disable InconsistentNaming
    public static void OnUpdateShortenTime(CommNetManager __instance, ref float ____timerRemaining)
    {
        if (____timerRemaining > RebuildGraphTimerSeconds)
            ____timerRemaining = RebuildGraphTimerSeconds;
    }
}