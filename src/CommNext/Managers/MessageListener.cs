﻿using CommNext.Network;
using CommNext.Rendering;
using CommNext.UI;
using KSP.Game;
using KSP.Messages;

namespace CommNext.Managers;

public static class MessageListener
{
    public static bool IsInMapView { get; private set; }

    public static MessageCenter Messages => GameManager.Instance.Game.Messages;

    public static void StartListening()
    {
        var messageCenter = GameManager.Instance.Game.Messages;
        messageCenter.PersistentSubscribe<MapInitializedMessage>(OnMapInitialized);
        messageCenter.PersistentSubscribe<MapViewEnteredMessage>(OnMapViewEntered);
        messageCenter.PersistentSubscribe<MapViewLeftMessage>(OnMapViewLeft);
        messageCenter.PersistentSubscribe<GameStateChangedMessage>(OnGameStateChanged);
        messageCenter.PersistentSubscribe<GameLoadFinishedMessage>(OnGameLoadFinished);
        messageCenter.PersistentSubscribe<VesselChangedMessage>(OnVesselChangedMessage);

        NetworkManager.Instance.SetupListeners();
    }

    private static void OnGameLoadFinished(MessageCenterMessage _)
    {
        IsInMapView = false;

        // Delete previous connections
        ConnectionsRenderer.Instance.Initialize();

        // Load game data
        SaveManager.Instance.LoadDataIntoUI();
    }

    private static void OnMapInitialized(MessageCenterMessage _)
    {
        IsInMapView = true;
    }

    private static void OnMapViewEntered(MessageCenterMessage _)
    {
        IsInMapView = true;
        MainUIManager.Instance.MapToolbarWindow!.IsWindowOpen = true;
    }

    private static void OnGameStateChanged(MessageCenterMessage message)
    {
        var gameStateChangedMessage = (GameStateChangedMessage)message;
        if (gameStateChangedMessage.CurrentState
            is GameState.TrackingStation
            or GameState.Map3DView
            or GameState.PlanetViewer)
        {
            IsInMapView = true;
            MainUIManager.Instance.MapToolbarWindow!.IsWindowOpen = true;
        }
        else
        {
            IsInMapView = false;
            MainUIManager.Instance.MapToolbarWindow!.IsWindowOpen = false;
        }
    }

    private static void OnMapViewLeft(MessageCenterMessage _)
    {
        IsInMapView = false;
        MainUIManager.Instance.MapToolbarWindow!.IsWindowOpen = false;
    }

    /// <summary>
    /// Updates the vessel in the VesselReportWindow when the vessel changes,
    /// only if the VesselReportWindow is open.
    /// </summary>
    private static void OnVesselChangedMessage(MessageCenterMessage message)
    {
        var vesselChangedMessage = (VesselChangedMessage)message;
        if (MainUIManager.Instance.VesselReportWindow!.Vessel != null)
            MainUIManager.Instance.VesselReportWindow!.Vessel = vesselChangedMessage.Vessel;
    }
}