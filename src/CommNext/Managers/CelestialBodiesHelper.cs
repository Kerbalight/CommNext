using KSP.Game;

namespace CommNext.Managers;

public class CelestialBodiesHelper
{
    public static string GetBodyName(int? index)
    {
        if (!index.HasValue) return "<Unknown>";
        var bodies = GameManager.Instance.Game.UniverseModel.GetAllCelestialBodies();
        return bodies[index.Value].DisplayName;
    }
}