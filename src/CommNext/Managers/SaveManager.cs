using BepInEx.Logging;
using CommNext.Data;
using CommNext.Rendering;
using CommNext.UI;
using KSP.Game;
using SpaceWarp.API.SaveGameManager;
using UnityEngine;

namespace CommNext.Managers;

public class SaveManager
{
    public static SaveManager Instance { get; private set; } = new();
    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CommNext.SaveManager");

    private SaveData? _loadedSaveData;

    public void Register()
    {
        ModSaves.RegisterSaveLoadGameData<SaveData>(CommNextPlugin.ModGuid, SaveGameData, LoadGameData);
    }

    private static void SaveGameData(SaveData dataToSave)
    {
        if (MainUIManager.Instance.MapToolbarWindow!.Position != Vector3.zero)
            dataToSave.MapToolbarPosition = MainUIManager.Instance.MapToolbarWindow!.Position;

        dataToSave.ConnectionsDisplayMode = ConnectionsRenderer.Instance.ConnectionsDisplayMode;
        dataToSave.ShowRulers = ConnectionsRenderer.Instance.IsRulersEnabled;
    }

    private void LoadGameData(SaveData dataToLoad)
    {
        _loadedSaveData = dataToLoad;
        Logger.LogInfo("Loaded game data");
    }

    /// <summary>
    /// Called when the UI is built, to load the saved data into the UI.
    /// </summary>
    public void LoadDataIntoUI()
    {
        if (_loadedSaveData == null) return;

        if (_loadedSaveData.MapToolbarPosition.HasValue && _loadedSaveData.MapToolbarPosition != Vector3.zero)
            MainUIManager.Instance.MapToolbarWindow.Position = _loadedSaveData.MapToolbarPosition.Value;

        if (_loadedSaveData.ShowRulers != null)
            ConnectionsRenderer.Instance.IsRulersEnabled = _loadedSaveData.ShowRulers.Value;
        if (_loadedSaveData.ConnectionsDisplayMode != null)
            ConnectionsRenderer.Instance.ConnectionsDisplayMode = _loadedSaveData.ConnectionsDisplayMode.Value;

        MainUIManager.Instance.MapToolbarWindow.UpdateButtonState();
        _loadedSaveData = null;
    }
}