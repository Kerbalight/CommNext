using System.Reflection;
using BepInEx;
using CommNext.Managers;
using CommNext.Network;
using CommNext.Patches;
using CommNext.Rendering;
using CommNext.Rendering.Behaviors;
using JetBrains.Annotations;
using SpaceWarp;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Mods;
using SpaceWarp.API.UI.Appbar;
using CommNext.UI;
using CommNext.Utils;
using HarmonyLib;
using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.UIElements;

namespace CommNext;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
public class CommNextPlugin : BaseSpaceWarpPlugin
{
    // Useful in case some other mod wants to use this mod a dependency
    [PublicAPI] public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    [PublicAPI] public const string ModName = MyPluginInfo.PLUGIN_NAME;
    [PublicAPI] public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

    /// Singleton instance of the plugin class
    [PublicAPI] public static CommNextPlugin Instance { get; set; }

    // AppBar button IDs
    internal const string ToolbarFlightButtonID = "BTN-CommNextFlight";
    internal const string ToolbarOabButtonID = "BTN-CommNextOAB";
    internal const string ToolbarKscButtonID = "BTN-CommNextKSC";

    /// <summary>
    /// Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        base.OnInitialized();

        Instance = this;

        // Load all the other assemblies used by this mod
        LoadAssemblies();

        // // Register Flight AppBar button
        // Appbar.RegisterAppButton(
        //     ModName,
        //     ToolbarFlightButtonID,
        //     AssetManager.GetAsset<Texture2D>($"{ModGuid}/images/icon.png"),
        //     isOpen => myFirstWindowController.IsWindowOpen = isOpen
        // );

        // Events
        MessageListener.StartListening();

        // Managers
        NetworkManager.Instance.Initialize();

        // Patches
        Harmony.CreateAndPatchAll(typeof(CommNetManagerPatches));
        Harmony.CreateAndPatchAll(typeof(ConnectionGraphPatches));
        
        // Settings
        Settings.SetupConfig();

        // Providers
        var providers = new GameObject("CommNext_Providers");
        providers.transform.parent = transform;
        providers.AddComponent<ConnectionsRenderer>();

        // UI
        MainUIManager.Instance.Initialize();

        // Load Assets
        ConnectionsRenderer.RulerSpherePrefab = AssetManager.GetAsset<GameObject>(
            $"{ModGuid}/commnext_ui/meshes/rulersphere.prefab");
        MapConnectionComponent.LineMaterial = AssetManager.GetAsset<Material>(
            $"{ModGuid}/commnext_ui/shaders/commconnectionmat.mat");
    }

    /// <summary>
    /// Loads all the assemblies for the mod.
    /// </summary>
    private static void LoadAssemblies()
    {
        // Load the Unity project assembly
        var currentFolder = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.FullName;
        var unityAssembly = Assembly.LoadFrom(Path.Combine(currentFolder, "CommNext.Unity.dll"));
        // Register any custom UI controls from the loaded assembly
        CustomControls.RegisterFromAssembly(unityAssembly);
    }
}