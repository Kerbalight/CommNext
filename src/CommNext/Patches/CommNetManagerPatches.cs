using AwesomeTechnologies;
using CommNext.Network;
using CommNext.UI;
using CommNext.Utils;
using HarmonyLib;
using KSP.Game;
using KSP.Sim;
using KSP.Sim.impl;
using Unity.Collections;

namespace CommNext.Patches;

public static class CommNetManagerPatches
{
    public static SimulationObjectModel FindCommNetOrigin(this UniverseModel universeModel)
    {
        return universeModel.FindSimObjectByNameKey(KerbinCommNetOriginName);
    }

    private const string KerbinCommNetOriginName = "kerbin_CommNetOrigin";
    private const string KerbinSpaceCenterName = "kerbin_KSC_Object";
    private const int KSCFallbackMaxRange = 2_000_000_000; // 2Gm

    /// <summary>
    /// Fix KSCommNetOrigin position and max range. We place it right on KSC, with
    /// a max range of 2Gm.
    /// </summary>
    [HarmonyPatch(typeof(CommNetManager), nameof(CommNetManager.SetSourceNode), [typeof(ConnectionGraphNode)])]
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
        newSourceNode.MaxRange = PluginSettings.KSCRange.Value switch
        {
            PluginSettings.KSCRangeMode.G2 => 2_000_000_000, // 2Gm
            PluginSettings.KSCRangeMode.G10 => 10_000_000_000, // 10Gm
            PluginSettings.KSCRangeMode.G50 => 50_000_000_000, // 50Gm
            _ => KSCFallbackMaxRange
        };

        NetworkManager.Instance.Nodes[newSourceNode.Owner].DebugVesselName = LocalizedStrings.KSCCommNet;
    }

    /// <summary>
    /// We are skipping original `OnUpdate`, in favour of our `OnLateUpdate`.
    /// See `NetworkManager.OnLateUpdate` for more info.
    /// </summary>
    [HarmonyPatch(typeof(CommNetManager), nameof(CommNetManager.OnUpdate))]
    [HarmonyPrefix]
    // ReSharper disable InconsistentNaming
    public static bool OnUpdateSkip(CommNetManager __instance)
    {
        return false;
    }

    /// <summary>
    /// We want to keep in sync CommNetManager lifetime with our NetworkManager.
    /// </summary>
    /// <param name="__instance">Initialized CommNetManager</param>
    [HarmonyPatch(typeof(CommNetManager), nameof(CommNetManager.Initialize))]
    [HarmonyPostfix]
    public static void Initialize(CommNetManager __instance)
    {
        NetworkManager.Instance.Initialize(__instance);
    }

    [HarmonyPatch(typeof(CommNetManager), nameof(CommNetManager.Shutdown))]
    [HarmonyPostfix]
    public static void Shutdown(CommNetManager __instance)
    {
        NetworkManager.Instance.Shutdown();
    }
}