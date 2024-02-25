using CommNext.Network.Bands;
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

    /// <summary>
    /// We keep the band ranges in an array, for performance reasons.
    /// For each band, the value is the maximum range on that frequency.
    /// If the band is not present, the value is 0.
    /// </summary>
    public double[] BandRanges { get; private set; } = [];

    // TODO Rename without the "Debug" prefix
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

    /// <summary>
    /// We set the band ranges for this node, converting the dictionary to
    /// a plain array.
    /// </summary>
    public void SetBandRanges(Dictionary<int, double> bandRanges)
    {
        BandRanges = new double[NetworkBands.Instance.AllBands.Count];
        for (var i = 0; i < NetworkBands.Instance.AllBands.Count; i++)
            BandRanges[i] = bandRanges.GetValueOrDefault(i, 0);
    }
}