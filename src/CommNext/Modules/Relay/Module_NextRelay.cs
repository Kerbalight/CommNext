using BepInEx.Logging;
using KSP.Sim.Definitions;
using UnityEngine;
using UnityEngine.Serialization;

namespace CommNext.Modules.Relay;

[DisallowMultipleComponent]
// ReSharper disable once InconsistentNaming
public class Module_NextRelay : PartBehaviourModule
{
    private static readonly ManualLogSource
        Logger = BepInEx.Logging.Logger.CreateLogSource("CommNext.Module_NextRelay");

    public override Type PartComponentModuleType => typeof(PartComponentModule_NextRelay);

    [SerializeField]
    protected Data_NextRelay? dataRelay;

    public override void AddDataModules()
    {
        base.AddDataModules();
        dataRelay ??= new Data_NextRelay();
        DataModules.TryAddUnique(dataRelay, out dataRelay);
    }
}