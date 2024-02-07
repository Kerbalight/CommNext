using BepInEx.Logging;
using SpaceWarp.API.Assets;
using UitkForKsp2.API;
using UnityEngine.UIElements;

namespace CommNext.UI;

public class MainUIManager
{
    public static MainUIManager Instance { get; set; } = new();
    
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CommNext.MainUIManager");
    
    public MapToolbarWindowController? MapToolbarWindow { get; set; }
    private UIDocument _mapToolbarDocument = null!;
    
    public void Initialize()
    {
        Logger.LogInfo("Initializing UI");
        
        // Map toolbar window
        _mapToolbarDocument = Window.Create(MapToolbarWindowController.WindowOptions,
            AssetManager.GetAsset<VisualTreeAsset>(
                $"{CommNextPlugin.ModGuid}/commnext_ui/ui/commnextmaptoolbar.uxml"));
        MapToolbarWindow = _mapToolbarDocument.gameObject.AddComponent<MapToolbarWindowController>();
    }
}