using BepInEx.Logging;
using CommNext.Network.Bands;
using CommNext.UI;
using CommNext.Utils;
using I2.Loc;
using KSP.Game;
using KSP.Sim;
using KSP.Sim.Definitions;
using KSP.Sim.ResourceSystem;
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

        // Add resource consumption info (EC/s)
        if (PluginSettings.RelaysRequirePower.Value)
            delegateList.Add(new OABPartData.PartInfoModuleEntry(
                LocalizationManager.GetTranslation("PartModules/Generic/Tooltip/Resources"),
                _ =>
                {
                    var subEntries = new List<OABPartData.PartInfoModuleSubEntry>
                    {
                        new(
                            LocalizedStrings.ElectricCharge,
                            PartModuleTooltipLocalization.FormatResourceRate(
                                RequiredResource.Rate,
                                PartModuleTooltipLocalization.GetTooltipResourceUnits(RequiredResource.ResourceName)
                            )
                        )
                    };
                    return subEntries;
                }));

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

    #region Resources

    public override void SetupResourceRequest(ResourceFlowRequestBroker resourceFlowRequestBroker)
    {
        if (!PluginSettings.RelaysRequirePower.Value) return;

        var resourceIDFromName =
            GameManager.Instance.Game.ResourceDefinitionDatabase.GetResourceIDFromName(RequiredResource.ResourceName);
        if (resourceIDFromName == ResourceDefinitionID.InvalidID)
        {
            Logger.LogError($"There are no resources with name {RequiredResource.ResourceName}");
            return;
        }

        RequestConfig = new ResourceFlowRequestCommandConfig
        {
            FlowResource = resourceIDFromName,
            FlowDirection = FlowDirection.FLOW_OUTBOUND,
            FlowUnits = 0.0
        };
        RequestHandle = resourceFlowRequestBroker.AllocateOrGetRequest("ModuleCommNextRelay", default);
        resourceFlowRequestBroker.SetCommands(
            RequestHandle,
            1.0,
            [RequestConfig]
        );
    }

    // [KSPDefinition]
    // [Tooltip("Whether the module consumes resources")]
    // public bool UseResources = true;

    public bool HasResourcesToOperate = true;

    [KSPDefinition]
    [Tooltip("Resource required to operate this module if it consumes resources")]
    public PartModuleResourceSetting RequiredResource;

    public ResourceFlowRequestCommandConfig RequestConfig;

    #endregion
}