namespace BedBrigade.Client.Services
{
    public interface IScheduleControlService
    {
        Task<string> ReplaceScheduleControl(string htmlText, int locationId);
    }
}
