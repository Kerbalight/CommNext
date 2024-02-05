using KSP.Game;
using KSP.Messages;

namespace CommNext.Managers;

public class MessageListener
{
    public static class EventListener
    { 
        public static bool IsInMapView { get; private set; }

        public static void RegisterEvents()
        {
            var messageCenter = GameManager.Instance.Game.Messages;
            messageCenter.PersistentSubscribe<MapInitializedMessage>(OnMapInitialized);
            messageCenter.PersistentSubscribe<MapViewLeftMessage>(OnMapViewLeft);
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
}