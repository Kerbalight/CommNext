using CommNext.Network;
using CommNext.Unity.Runtime.Controls;
using UnityEngine.UIElements;
using SortDirection = UnityEngine.UIElements.SortDirection;

namespace CommNext.UI.Logic;

public class ConnectionsQuery
{
    public enum ConnectionsSort
    {
        Distance,
        Band,
        SignalStrength,
        Name
    }

    public event Action? Changed;

    public VesselNodesFilter Filter = VesselNodesFilter.InRange;
    public ConnectionsSort Sort = ConnectionsSort.Distance;
    public SortDirection Direction = SortDirection.Descending;

    public void ApplySort(NetworkNode current, List<NetworkConnection> connections)
    {
        connections.Sort((a, b) =>
        {
            var compared = Sort switch
            {
                ConnectionsSort.Distance => a.Distance.CompareTo(b.Distance),
                ConnectionsSort.Band => a.SelectedBand?.CompareTo(b.SelectedBand) ?? 0,
                ConnectionsSort.SignalStrength => a.SignalStrength().CompareTo(b.SignalStrength()),
                ConnectionsSort.Name => string.Compare(a.GetOther(current).DebugVesselName,
                    b.GetOther(current).DebugVesselName, StringComparison.Ordinal),
                _ => 0
            };
            return Direction == SortDirection.Ascending ? compared : -compared;
        });
    }

    public void BindFilter(DropdownField dropdownField)
    {
        dropdownField.choices = FilterChoices;
        dropdownField.value = AllFilters.Find(f => f.Item1 == Filter).Item2;
        dropdownField.RegisterValueChangedCallback(evt =>
        {
            Filter = AllFilters.Find(f => f.Item2 == evt.newValue).Item1;
            Changed?.Invoke();
        });
    }

    public void BindSort(DropdownField dropdownField)
    {
        dropdownField.choices = SortChoices;
        dropdownField.value = AllSorts.Find(f => f.Item1 == Sort).Item2;
        dropdownField.RegisterValueChangedCallback(evt =>
        {
            Sort = AllSorts.Find(f => f.Item2 == evt.newValue).Item1;
            Changed?.Invoke();
        });
    }

    public void BindDirection(SortDirectionButton button)
    {
        button.direction = Direction;
        button.directionChanged += direction => { Direction = direction; };
        Changed?.Invoke();
    }

    private static readonly List<(VesselNodesFilter, string)> AllFilters =
    [
        (VesselNodesFilter.Active, LocalizedStrings.FilterActive),
        (VesselNodesFilter.Connected, LocalizedStrings.FilterConnected),
        (VesselNodesFilter.InRange, LocalizedStrings.FilterInRange),
        (VesselNodesFilter.All, LocalizedStrings.FilterAll)
    ];

    private static List<string> FilterChoices => AllFilters.Select(x => x.Item2).ToList();

    private static readonly List<(ConnectionsSort, string)> AllSorts =
    [
        (ConnectionsSort.Distance, LocalizedStrings.SortByDistance),
        (ConnectionsSort.SignalStrength, LocalizedStrings.SortBySignalStrength),
        (ConnectionsSort.Name, LocalizedStrings.SortByName),
        (ConnectionsSort.Band, LocalizedStrings.SortByBand)
    ];

    private static List<string> SortChoices => AllSorts.Select(x => x.Item2).ToList();
}