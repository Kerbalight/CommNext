using CommNext.Utils;
using KSP.Game;
using KSP.Sim.Definitions;
using KSP.Sim.impl;

namespace CommNext.Network;

public class NetworkNode
{
    public IGGuid Owner { get; private set; }

    public bool IsRelay { get; set; }
    public bool HasEnoughResources { get; set; }

    public string Band { get; set; } = "X";

    /// <summary>
    /// Used only for debugging purposes.
    /// </summary>
    public string DebugVesselName { get; set; } = "N/A";

    public NetworkNode(IGGuid owner)
    {
        Owner = owner;
    }

    /// <summary>
    /// We want to avoid a loop between "NoCommNet" status (which modifies VesselControlState)
    /// and the control state (which modifies the NetworkNode).
    /// We just want to know if electricity is available, so even if the vessel
    /// is in "NoCommNet" status, we will consider it as having enough resources.
    /// </summary>
    public void UpdateFromVessel(VesselComponent? vessel)
    {
        if (vessel == null)
        {
            HasEnoughResources = true;
            return;
        }

        DebugVesselName = vessel.Name;
        HasEnoughResources = DifficultyUtils.HasInfinitePower ||
                             !PluginSettings.RelaysRequirePower.Value ||
                             vessel.ControlStatus != VesselControlState.NoControl;
    }
}