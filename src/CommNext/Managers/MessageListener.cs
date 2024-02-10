using CommNext.Network;
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
    }

    private static void OnGameLoadFinished(MessageCenterMessage _)
    {
        IsInMapView = false;

        // Precompute some references
        CommunicationsManager.Instance.Initialize();
        // Delete previous connections
        ConnectionsRenderer.Instance.Initialize();
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
}