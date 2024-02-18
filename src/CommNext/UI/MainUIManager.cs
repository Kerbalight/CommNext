using BepInEx.Logging;
using CommNext.UI.Tooltip;
using SpaceWarp.API.Assets;
using UitkForKsp2.API;
using UnityEngine.UIElements;

namespace CommNext.UI;

public class MainUIManager
{
    public static MainUIManager Instance { get; set; } = new();

    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CommNext.MainUIManager");

    public MapToolbarWindowController MapToolbarWindow { get; set; } = null!;
    private UIDocument _mapToolbarDocument = null!;

    public VesselReportWindowController? VesselReportWindow { get; set; }
    private UIDocument _vesselReportDocument = null!;

    public TooltipWindowController TooltipWindow { get; set; } = null!;
    private UIDocument _tooltipDocument = null!;

    public void Initialize()
    {
        Logger.LogInfo("Initializing UI");

        // Map toolbar window
        _mapToolbarDocument = Window.Create(MapToolbarWindowController.WindowOptions,
            AssetManager.GetAsset<VisualTreeAsset>(
                $"{CommNextPlugin.ModGuid}/commnext_ui/ui/commnextmaptoolbar.uxml"));
        MapToolbarWindow = _mapToolbarDocument.gameObject.AddComponent<MapToolbarWindowController>();

        // Vessel report window
        _vesselReportDocument = Window.Create(VesselReportWindowController.WindowOptions,
            AssetManager.GetAsset<VisualTreeAsset>(
                $"{CommNextPlugin.ModGuid}/commnext_ui/ui/vesselreportwindow.uxml"));
        VesselReportWindow = _vesselReportDocument.gameObject.AddComponent<VesselReportWindowController>();

        // Tooltip window (should be created last)
        _tooltipDocument = Window.Create(TooltipWindowController.WindowOptions,
            AssetManager.GetAsset<VisualTreeAsset>(
                $"{CommNextPlugin.ModGuid}/commnext_ui/ui/tooltipwindow.uxml"));
        TooltipWindow = _tooltipDocument.gameObject.AddComponent<TooltipWindowController>();
    }
}