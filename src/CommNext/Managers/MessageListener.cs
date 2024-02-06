using KSP.Game;
using KSP.Messages;

namespace CommNext.Managers;

public static class MessageListener
{
    public static bool IsInMapView { get; private set; }

    public static void StartListening()
    {
        var messageCenter = GameManager.Instance.Game.Messages;
        messageCenter.PersistentSubscribe<MapInitializedMessage>(OnMapInitialized);
        messageCenter.PersistentSubscribe<MapViewLeftMessage>(OnMapViewLeft);
        messageCenter.PersistentSubscribe<GameLoadFinishedMessage>(OnGameLoadFinished);
    }
    
    private static void OnGameLoadFinished(MessageCenterMessage _)
    {
        IsInMapView = false;
        
        // Precompute some references
        CommunicationsManager.Instance.Initialize();
    }

    private static void OnMapInitialized(MessageCenterMessage _)
    {
        IsInMapView = true;
    }

    private static void OnMapViewLeft(MessageCenterMessage _)
    {
        IsInMapView = false;
    }
    
}