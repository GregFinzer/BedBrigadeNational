namespace BedBrigade.Client.Services
{
    public interface IFooterLocationState
    {
        string Location { get; set; }
        event Action OnChange;
    }
}
