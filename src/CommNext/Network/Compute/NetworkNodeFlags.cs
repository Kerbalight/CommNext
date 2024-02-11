namespace CommNext.Network.Compute;

[Flags]
public enum NetworkNodeFlags
{
    None = 0,
    IsRelay = 1,
    HasEnoughResources = 2
}