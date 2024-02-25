using BepInEx.Logging;
using CommNext.Network.Bands;
using CommNext.UI;
using I2.Loc;
using KSP.Sim;
using KSP.Sim.Definitions;
using KSP.UI.Binding;
using UnityEngine;

namespace CommNext.Modules.Relay;

[Serializable]
// ReSharper disable once InconsistentNaming
public class Data_NextRelay : ModuleData
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CommNext.Data_NextRelay");

    public override Type ModuleType => typeof(Module_NextRelay);

    // [KSPDefinition] 
    // public string ModeValue;

    [KSPState]
    [LocalizedField(LocalizedStrings.EnableRelayKey)]
    [PAMDisplayControl(SortIndex = 0)]
    public ModuleProperty<bool> EnableRelay = new(true);

    public override void OnPartBehaviourModuleInit() { }

    public override List<OABPartData.PartInfoModuleEntry> GetPartInfoEntries(
        Type partBehaviourModuleType,
        List<OABPartData.PartInfoModuleEntry> delegateList)
    {
        if (partBehaviourModuleType != ModuleType) return delegateList;

        delegateList.Add(new OABPartData.PartInfoModuleEntry(
            "",
            s => LocalizedStrings.RelayDescription));
        return delegateList;
    }

    #region Utils

    /// <summary>
    /// Keep the relay configuration in sync with the part
    /// </summary>
    public override void Copy(ModuleData sourceModuleData)
    {
        var dataRelay = (Data_NextRelay?)sourceModuleData;
        if (dataRelay == null) return;
        EnableRelay.SetValue(dataRelay.EnableRelay.GetValue());
    }

    #endregion
}