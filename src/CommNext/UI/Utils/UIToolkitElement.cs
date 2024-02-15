using SpaceWarp.API.Assets;
using UnityEngine.UIElements;

namespace CommNext.UI.Utils;

public class UIToolkitElement
{
    protected VisualElement _root;
    public VisualElement Root => _root;

    public UIToolkitElement(string assetPath)
    {
        var asset = Load(assetPath);
        _root = asset.Instantiate();
        _root.userData = this;
    }

    /// <summary>
    /// Loads a UIToolkitElement from the asset bundle, avoiding the need to manually
    /// compose the asset path and to lowercase the asset path.
    /// </summary>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    public static VisualTreeAsset Load(string assetPath)
    {
        return AssetManager.GetAsset<VisualTreeAsset>(
            $"{CommNextPlugin.ModGuid}/commnext_ui/ui/{assetPath.ToLower()}");
    }
}