using CommNext.UI;
using KSP.Game;

namespace CommNext.Managers;

public static class MapViewHelper
{
    private static GameInstance Game => GameManager.Instance.Game;

    /// <summary>
    /// Checks if the game is in map view or notify the player that the action
    /// requires map view.
    /// </summary>
    public static bool IsInMapViewOrNotify()
    {
        if (MessageListener.IsInMapView) return true;

        NotifyRequiresMapView();
        return false;
    }

    private static void NotifyRequiresMapView()
    {
        Game.Notifications.ProcessNotification(new NotificationData()
        {
            Tier = NotificationTier.Passive,
            Primary = new NotificationLineItemData()
            {
                LocKey = LocalizedStrings.ActionRequiresMapViewKey
            }
        });
    }
}