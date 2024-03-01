using CommNext.Network;
using HarmonyLib;
using KSP.Sim.impl;

namespace CommNext.Patches;

public static class SimulationObjectModelPatches
{
    /// <summary>
    /// Update the NetworkNode's vessel name when the SimulationObjectModel's display name changes.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(SimulationObjectModel), nameof(SimulationObjectModel.UpdateDisplayName))]
    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    public static void UpdateNetworkNodeVesselName(SimulationObjectModel __instance)
    {
        if (!NetworkManager.Instance.Nodes.TryGetValue(__instance.GlobalId, out var networkNode)) return;
        networkNode.VesselName = __instance.DisplayName;
    }
}