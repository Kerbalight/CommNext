using BepInEx.Logging;
using KSP.Modules;
using KSP.Sim.impl;

namespace CommNext.Modules.Modulator;

// ReSharper disable once InconsistentNaming
public class PartComponentModule_NextModulator : PartComponentModule
{
    private static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource("CommNext.PartComponentModule_NextModulator");

    public override Type PartBehaviourModuleType => typeof(Module_NextModulator);

    /// <summary>
    /// A modulator is always linked to a DataTransmitter, see Patch Manager patches
    /// </summary>
    public Data_Transmitter DataTransmitter { get; private set; } = null!;

    private Data_NextModulator _dataModulator;
    public Data_NextModulator DataModulator => _dataModulator;

    public override void OnStart(double universalTime)
    {
        if (!DataModules.TryGetByType<Data_NextModulator>(out _dataModulator))
        {
            Logger.LogError("Unable to find a Data_NextModulator in the PartComponentModule for " + Part.PartName);
            return;
        }

        Part!.TryGetModuleData<PartComponentModule_DataTransmitter, Data_Transmitter>(out var dataTransmitter);
        DataTransmitter = dataTransmitter;
    }
}