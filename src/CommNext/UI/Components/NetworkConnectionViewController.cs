using CommNext.Network;
using CommNext.UI.Utils;
using KSP.Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace CommNext.UI.Components;

public class NetworkConnectionViewController : UIToolkitElement
{
    private Label _nameLabel;
    private Label _detailsLabel;
    private VisualElement _icon;

    public NetworkConnectionViewController() : base("Components/NetworkConnectionView.uxml")
    {
        _icon = _root.Q<VisualElement>("icon");
        _nameLabel = _root.Q<Label>("name-label");
        _detailsLabel = _root.Q<Label>("details-label");
    }

    public void Bind(NetworkNode currentNode, NetworkConnection connection)
    {
        var otherNode = connection.GetOther(currentNode);
        _nameLabel.text = otherNode.DebugVesselName;
        var inboundOutboundText = connection.IsSource(currentNode) ? "Outbound" : "Inbound";
        _detailsLabel.text = LocalizedStrings.GetTranslationWithParams(
            LocalizedStrings.ConnectionDetailsKey, new Dictionary<string, string>
            {
                { "meters", $"<color=#E7CA76>{connection.Distance:F2}</color> {inboundOutboundText}" }
            });
        _icon.style.unityBackgroundImageTintColor = connection.IsConnected ? Color.green : Color.red;
    }
}