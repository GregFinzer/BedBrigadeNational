namespace BedBrigade.Client.Services
{
    public interface ILocationState
    {
        string Location { get; set; }
        event Func<Task> OnChange;
    }
}
