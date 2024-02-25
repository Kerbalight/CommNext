using KSP.Sim.Definitions;
using UnityEngine;

namespace CommNext.Modules.Modulator;

/// <summary>
/// The modulator is responsible for the modulation of the signal, aka
/// to provide Network Bands configuration.
/// </summary>
[DisallowMultipleComponent]
// ReSharper disable once InconsistentNaming
public class Module_NextModulator : PartBehaviourModule
{
    public override Type PartComponentModuleType => typeof(PartComponentModule_NextModulator);

    [SerializeField]
    protected Data_NextModulator? dataModulator;

    public override void AddDataModules()
    {
        base.AddDataModules();
        dataModulator ??= new Data_NextModulator();
        DataModules.TryAddUnique(dataModulator, out dataModulator);
    }

    public override void OnInitialize()
    {
        base.OnInitialize();

        var modulator = dataModulator;
        if (modulator != null)
        {
            modulator.OmniBand.OnChangedValue += OnOmniBandChangedValue;
            modulator.Band.OnChangedValue += OnBandChangedValue;
            modulator.SecondaryBand.OnChangedValue += OnBandChangedValue;
        }
    }

    /// <summary>
    /// Band is not interactable if the antenna is an omni-band.
    /// </summary>
    private void OnOmniBandChangedValue(bool isOmniBand)
    {
        part.partOwner.SimObjectComponent.SimulationObject.Telemetry.RefreshCommNetNode();
    }

    private void OnBandChangedValue(string band)
    {
        part.partOwner.SimObjectComponent.SimulationObject.Telemetry.RefreshCommNetNode();
    }

    public override void OnShutdown()
    {
        base.OnShutdown();
        var modulator = dataModulator;
        if (modulator != null)
        {
            modulator.OmniBand.OnChangedValue -= OnOmniBandChangedValue;
            modulator.Band.OnChangedValue -= OnBandChangedValue;
            modulator.SecondaryBand.OnChangedValue -= OnBandChangedValue;
        }
    }
}