using CommNext.Modules.Modulator;
using CommNext.Modules.Relay;
using CommNext.Network;
using CommNext.Network.Bands;
using HarmonyLib;
using KSP.Game;
using KSP.Modules;
using KSP.Sim.Definitions;
using KSP.Sim.impl;
using Unity.Mathematics;

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
        networkNode.HasEnoughResources = true;
        networkNode.UpdateFromVessel(simulationObject.Vessel);
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
        var partOwner = __instance.SimulationObject.PartOwner;
        if (partOwner == null) return;

        var isRelay = false;
        // By default, we assume the part has enough resources to operate if it's not a relay
        var hasEnoughResources = true;
        Dictionary<int, double> bandRanges = new();

        foreach (var part in partOwner.Parts)
        {
            if (part.TryGetModuleData<PartComponentModule_NextRelay, Data_NextRelay>(out var data))
            {
                var isPartRelay = data.EnableRelay.GetValue();
                isRelay |= isPartRelay;

                if (isPartRelay)
                    hasEnoughResources &= data.HasResourcesToOperate;
            }

            if (!part.TryGetModule<PartComponentModule_DataTransmitter>(out var transmitterModule))
                continue;

            if (!part.TryGetModule<PartComponentModule_NextModulator>(out var modulatorModule))
                continue;

            if (!transmitterModule.IsTransmitterActive()) continue;

            // Bands processing
            var modulator = modulatorModule.DataModulator;

            // 1. Omni-bands
            if (modulator.OmniBand.GetValue())
            {
                for (var i = 0; i < NetworkBands.Instance.AllBands.Count; i++)
                    bandRanges[i] = math.max(bandRanges.GetValueOrDefault(i, 0),
                        transmitterModule.CommunicationRangeMeters);
                continue;
            }

            // 2. Specific bands
            var bandIndex = NetworkBands.Instance.GetBandIndex(modulator.Band.GetValue());
            var secondaryBandIndex = NetworkBands.Instance.GetBandIndex(modulator.SecondaryBand.GetValue());

            if (bandIndex >= 0)
                bandRanges[bandIndex] =
                    math.max(bandRanges.GetValueOrDefault(bandIndex, 0),
                        transmitterModule.CommunicationRangeMeters);


            if (secondaryBandIndex >= 0)
                bandRanges[secondaryBandIndex] =
                    math.max(bandRanges.GetValueOrDefault(secondaryBandIndex, 0),
                        transmitterModule.CommunicationRangeMeters);
        }

        networkNode.IsRelay = isRelay;
        networkNode.HasEnoughResources = hasEnoughResources;
        networkNode.UpdateFromVessel(__instance.SimulationObject.Vessel);
        networkNode.SetBandRanges(bandRanges);
    }
}