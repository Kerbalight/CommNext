using CommNext.Models;
using HarmonyLib;
using KSP.Game;
using Unity.Collections;

namespace CommNext.Patches;

public static class CommNetManagerPatches
{
    [HarmonyPatch(typeof(CommNetManager), "OnUpdate")]
    [HarmonyPostfix]
    public static void OnUpdateShortenTime(CommNetManager __instance, ref float ____timerRemaining)
    {
        if (____timerRemaining > 0.2f)
            ____timerRemaining = 0.2f;
    }
}