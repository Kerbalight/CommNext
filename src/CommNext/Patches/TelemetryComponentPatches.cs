using CommNext.Modules.Relay;
using CommNext.Network;
using HarmonyLib;
using KSP.Sim.impl;

namespace CommNext.Patches;

/// <summary>
/// We patch the TelemetryComponent to keep in sync the NetworkNode (our
/// internal representation of a network node) with the telemetry component
/// (which manages the stock CommNet nodes).
/// </summary>
public static class TelemetryComponentPatches
{
    [HarmonyPatch(typeof(TelemetryComponent), nameof(TelemetryComponent.OnAdded))]
    [HarmonyPrefix]
    public static void OnAdded(SimulationObjectModel simulationObject, double universalTime)
    {
        var networkNode = new NetworkNode(simulationObject.GlobalId);
        NetworkManager.Instance.RegisterNode(networkNode);
    }

    [HarmonyPatch(typeof(TelemetryComponent), nameof(TelemetryComponent.OnRemoved))]
    [HarmonyPrefix]
    public static void OnRemoved(SimulationObjectModel simulationObject, double universalTime)
    {
        NetworkManager.Instance.UnregisterNode(simulationObject.GlobalId);
    }

    [HarmonyPatch(typeof(TelemetryComponent), nameof(TelemetryComponent.RefreshCommNetNode))]
    [HarmonyPostfix]
    // ReSharper disable once InconsistentNaming
    public static void RefreshCommNetNode(TelemetryComponent __instance)
    {
        var networkNode = NetworkManager.Instance.Nodes[__instance.GlobalId];
        var isRelay = false;
        var partOwner = __instance.SimulationObject.PartOwner;
        if (partOwner == null) return;

        foreach (var part in partOwner.Parts)
        {
            if (!part.TryGetModule<PartComponentModule_NextRelay>(out var module)) continue;
            isRelay = true;
        }

        networkNode.IsRelay = isRelay;
    }
}