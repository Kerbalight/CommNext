using BepInEx.Logging;
using CommNext.Modules.Relay;
using CommNext.Network.Bands;
using CommNext.UI;
using CommNext.UI.Utils;
using KSP.Sim;
using KSP.Sim.Definitions;
using KSP.UI.Binding;

namespace CommNext.Modules.Modulator;

[Serializable]
// ReSharper disable once InconsistentNaming
public class Data_NextModulator : ModuleData
{
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CommNext.Data_NextRelay");

    public override Type ModuleType => typeof(Module_NextModulator);

    [KSPDefinition]
    // ReSharper disable once InconsistentNaming
    public ModulatorKind ModulatorKind;

    [KSPState]
    [LocalizedField(LocalizedStrings.OmniBandKey)]
    [PAMDisplayControl(SortIndex = 1)]
    public ModuleProperty<bool> OmniBand = new(false);

    [KSPState]
    [LocalizedField(LocalizedStrings.BandKey)]
    [PAMDisplayControl(SortIndex = 2)]
    public ModuleProperty<string> Band = new(NetworkBands.DefaultBand, band => $"{band}");

    [LocalizedField(LocalizedStrings.SecondaryBandKey)]
    [PAMDisplayControl(SortIndex = 3)]
    public ModuleProperty<string> SecondaryBand = new("", band => $"{band}");


    public override void OnPartBehaviourModuleInit()
    {
        // Setup the Dropdown for Band
        var bandOptions = new DropdownItemList();
        var secondaryBandOptions = new DropdownItemList();
        secondaryBandOptions.Add("", new DropdownItem() { key = "", text = LocalizedStrings.NoneBand });

        foreach (var band in NetworkBands.Instance.AllBands)
        {
            // TODO Add image? Sprite is supported, should test it
            bandOptions.Add(band.Code,
                new DropdownItem()
                {
                    key = band.Code,
                    text = band.DisplayName.RTEColor(band.Color)
                    // image = NetworkBands.Instance.GetIconSprite(band.Code)
                });

            secondaryBandOptions.Add(band.Code,
                new DropdownItem()
                {
                    key = band.Code,
                    text = band.DisplayName.RTEColor(band.Color)
                    // image = NetworkBands.Instance.GetIconSprite(band.Code)
                });
        }

        SetDropdownData(Band, bandOptions);
        SetDropdownData(SecondaryBand, secondaryBandOptions);

        if (ModulatorKind != ModulatorKind.OmniBand) SetVisible(OmniBand, false);
        if (ModulatorKind == ModulatorKind.MonoBand) SetVisible(SecondaryBand, false);
    }

    public override List<OABPartData.PartInfoModuleEntry> GetPartInfoEntries(
        Type partBehaviourModuleType,
        List<OABPartData.PartInfoModuleEntry> delegateList)
    {
        if (partBehaviourModuleType != ModuleType) return delegateList;

        delegateList.Add(new OABPartData.PartInfoModuleEntry(
            LocalizedStrings.ModulatorKind,
            s => ModulatorKind switch
            {
                ModulatorKind.MonoBand => LocalizedStrings.ModulatorKindMonoBand,
                ModulatorKind.DualBand => LocalizedStrings.ModulatorKindDualBand,
                ModulatorKind.OmniBand => LocalizedStrings.ModulatorKindOmniBand,
                _ => "N/A"
            }));

        return delegateList;
    }

    #region Utils

    /// <summary>
    /// Keep the module configuration in sync with the part
    /// </summary>
    public override void Copy(ModuleData sourceModuleData)
    {
        var dataModulator = (Data_NextModulator?)sourceModuleData;
        if (dataModulator == null) return;
        OmniBand.SetValue(dataModulator.OmniBand.GetValue());
        Band.SetValue(dataModulator.Band.GetValue());
        SecondaryBand.SetValue(dataModulator.SecondaryBand.GetValue());
    }

    #endregion
}