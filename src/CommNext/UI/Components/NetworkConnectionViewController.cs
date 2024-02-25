﻿using CommNext.Managers;
using CommNext.Network;
using CommNext.Network.Bands;
using CommNext.UI.Tooltip;
using CommNext.UI.Utils;
using CommNext.Unity.Runtime.Controls;
using I2.Loc;
using KSP;
using KSP.Game;
using KSP.Sim;
using KSP.Sim.impl;
using UnityEngine;
using UnityEngine.UIElements;

namespace CommNext.UI.Components;

public class NetworkConnectionViewController : UIToolkitElement, IPoolingElement
{
    private Label _nameLabel;
    private Label _detailsLabel;
    private VisualElement _connectionIcon;
    private VisualElement _directionIcon;
    private VisualElement _directionTag;
    private Label _directionLabel;
    private VisualElement _powerIcon;
    private SignalStrengthIcon _signalStrengthIcon;
    private TooltipManipulator _signalStrengthTooltip;
    private VisualElement _rowContainer;

    private NetworkNode _currentNode = null!;
    private NetworkConnection _connection = null!;

    private static Color ActiveColor => new(0f, 1f, 0.4f, 1f);
    private static Color InactiveColor => new(1f, 0.36f, 0.28f, 1f);

    public NetworkConnectionViewController() : base("Components/NetworkConnectionView.uxml")
    {
        _connectionIcon = _root.Q<VisualElement>("connection-icon");
        _directionTag = _root.Q<VisualElement>("direction-tag");
        _directionIcon = _root.Q<VisualElement>("direction-icon");
        _nameLabel = _root.Q<Label>("name-label");
        _detailsLabel = _root.Q<Label>("details-label");
        _directionLabel = _root.Q<Label>("direction-label");
        _powerIcon = _root.Q<VisualElement>("power-icon");
        _powerIcon.AddTooltip(LocalizedStrings.NoPower);
        _signalStrengthIcon = _root.Q<SignalStrengthIcon>("signal-strength-icon");
        _signalStrengthTooltip = new TooltipManipulator("");
        _signalStrengthIcon.AddManipulator(_signalStrengthTooltip);
        _rowContainer = _root.Q<VisualElement>("row__container");
        _rowContainer.AddManipulator(new Clickable(OnClick));
    }

    /// <summary>
    /// See `MapUISelectableItem.HandleVesselControl`. This implementation
    /// is pretty much the same (except for the 3D Map view part).
    /// </summary>
    private void OnClick()
    {
        var game = GameManager.Instance.Game;
        if (!game.ViewController.CanObserverLeaveTheActiveVessel()) return;

        game.UI.SetCurtainContext(CurtainContext.EnterGamePlay);
        game.UI.SetCurtainVisibility(true, () =>
        {
            game.GlobalGameState.SetState(GameState.Map3DView);
            Mouse.EnableVirtualCursor(false);

            var targetVessel = game.UniverseModel.FindVesselComponent(_connection.GetOther(_currentNode).Owner);
            game.ViewController.SetActiveVehicle(targetVessel, true, true);
            game.UI.SetCurtainVisibility(false);
        });
    }


    public void Bind(NetworkNode currentNode, NetworkConnection connection)
    {
        _currentNode = currentNode;
        _connection = connection;

        var otherNode = connection.GetOther(currentNode);
        _nameLabel.text = otherNode.DebugVesselName;
        _directionLabel.text = connection.IsSource(currentNode)
            ? LocalizedStrings.OutDirection
            : LocalizedStrings.InDirection;

        _directionTag.style.display = connection.IsActive ? DisplayStyle.Flex : DisplayStyle.None;
        _directionTag.ToggleClassesIf(
            connection.IsSource(currentNode),
            ["direction__tag--outbound"],
            ["direction__tag--inbound"]
        );

        _powerIcon.style.display = connection.IsPowered() ? DisplayStyle.None : DisplayStyle.Flex;

        var distanceText = LocalizationManager.GetTranslation(LocalizedStrings.DistanceLabelKey, [
            $"<color=#E7CA76>{Units.PrintSI(connection.Distance, Units.SymbolMeters)}</color>"
        ]);

        var occludedText = connection.OccludingBody.HasValue
            ? " | " +
              LocalizationManager.GetTranslation(LocalizedStrings.OccludedByKey, [
                  CelestialBodiesHelper.GetBodyName(connection.OccludingBody).RTEColor("#E7CA76")
              ]).RTEColor("#FF5B48")
            : "";

        var bandText = "";
        if (connection.SelectedBand.HasValue)
        {
            bandText = " | ";
            var networkBand = NetworkBands.Instance.AllBands[connection.SelectedBand.Value];

            if (!connection.IsBandMissingRange)
                bandText += networkBand.DisplayName.RTEColor("#" + ColorUtility.ToHtmlStringRGB(networkBand.Color));
            else
                bandText += LocalizationManager.GetTranslation(LocalizedStrings.BandMissingRangeKey, [
                    networkBand.Code.RTEColor("#E7CA76")
                ]).RTEColor("#FF5B48");
        }
        else if (connection is { IsConnected: false, IsBandNotAvailable: true })
        {
            bandText = " | " + LocalizationManager.GetTranslation(LocalizedStrings.NoAvailableBand).RTEColor("#FF5B48");
        }

        _detailsLabel.text = distanceText + bandText + occludedText;

        var signalStrength = connection.SignalStrength();
        _signalStrengthIcon.SetStrengthPercentage(signalStrength);
        _signalStrengthTooltip.TooltipText = signalStrength.ToString("P0");

        _connectionIcon.style.unityBackgroundImageTintColor = connection.IsConnected ? ActiveColor : InactiveColor;
    }
}