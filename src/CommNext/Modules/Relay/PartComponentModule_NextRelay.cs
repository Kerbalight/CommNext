using BepInEx.Logging;
using KSP.Modules;
using KSP.Sim.impl;

namespace CommNext.Modules.Relay;

// ReSharper disable once InconsistentNaming
public class PartComponentModule_NextRelay : PartComponentModule
{
    private static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource("CommNext.PartComponentModule_NextRelay");

    public override Type PartBehaviourModuleType => typeof(Module_NextRelay);

    /// <summary>
    /// A link to the DataTransmitter
    /// </summary>
    public Data_Transmitter DataTransmitter { get; private set; } = null!;

    private Data_NextRelay _dataRelay;


    public override void OnStart(double universalTime)
    {
        Logger.LogDebug($"OnStart triggered. Vessel '{Part?.PartOwner?.SimulationObject?.Vessel?.Name ?? "<none>"}'");

        if (!DataModules.TryGetByType<Data_NextRelay>(out _dataRelay))
        {
            Logger.LogError("Unable to find a Data_Relay in the PartComponentModule for " + Part.PartName);
            return;
        }

        Part!.TryGetModuleData<PartComponentModule_DataTransmitter, Data_Transmitter>(out var dataTransmitter);
        DataTransmitter = dataTransmitter;
    }
}