using BepInEx.Logging;
using CommNext.Network;
using CommNext.Network.Bands;
using CommNext.Rendering;
using CommNext.UI.Tooltip;
using CommNext.UI.Utils;
using CommNext.Unity.Runtime.Controls;
using KSP;
using UnityEngine.UIElements;

namespace CommNext.UI.Components;

/// <summary>
/// Details the UI for a band row in the vessel report window.
/// Each band row show its name, the range for the current vessel,
/// and a toggle to activate or deactivate the band in the map view.
/// </summary>
public class BandRowController : UIToolkitElement, IPoolingElement
{
    private static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource("CommNext.BandRowController");

    private readonly Label _nameLabel;
    private readonly Label _rangeLabel;
    private readonly Toggle _activateToggle;
    private readonly BandIcon _bandIcon;

    private NetworkNode _currentNode = null!;
    private NetworkBand _band = null!;

    private static event Action<NetworkNode, NetworkBand, bool>? ActivateToggled;

    public BandRowController() : base("Components/BandRow.uxml")
    {
        _nameLabel = _root.Q<Label>("name-label");
        _rangeLabel = _root.Q<Label>("range-label");
        _bandIcon = _root.Q<BandIcon>("band-icon");
        _activateToggle = _root.Q<Toggle>("activate-toggle");
        _activateToggle.AddManipulator(new TooltipManipulator(LocalizedStrings.TooltipActivateBandRulers));
        _activateToggle.RegisterValueChangedCallback(OnActivateToggleChanged);

        ActivateToggled += OnOtherBandActivated;
    }

    ~BandRowController()
    {
        ActivateToggled -= OnOtherBandActivated;
    }

    public void Bind(NetworkNode currentNode, int bandIndex, double nodeBandRange)
    {
        _currentNode = currentNode;
        var band = NetworkBands.Instance.AllBands[bandIndex];
        _band = band;

        _nameLabel.text = band.DisplayName;
        _rangeLabel.text = Units.PrintSI(nodeBandRange, Units.SymbolMeters).RTEColor("#E7CA76");
        _bandIcon.SetBand(band.Code, band.Color);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void OnActivateToggleChanged(ChangeEvent<bool> evt)
    {
        ActivateToggled?.Invoke(_currentNode, _band, evt.newValue);
        if (evt.newValue == false)
        {
            ConnectionsRenderer.Instance.SelectedBandIndex = null;
            MainUIManager.Instance.MapToolbarWindow.UpdateButtonState();
            return;
        }

        var bandIndex = NetworkBands.Instance.BandIndexByCode[_band.Code];
        ConnectionsRenderer.Instance.SelectedBandIndex = bandIndex;
        MainUIManager.Instance.MapToolbarWindow.UpdateButtonState();
    }

    private void OnOtherBandActivated(NetworkNode node, NetworkBand band, bool active)
    {
        if (band == _band || node != _currentNode || active == false) return;
        _activateToggle.SetValueWithoutNotify(false);
    }
}