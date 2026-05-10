using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface ITeamSheetService
    {
        string CreateTeamSheetFileName(Location location, List<BedRequest> bedRequests);
        Stream CreateTeamSheet(Location location, List<BedRequest> bedRequests, string? deliveryChecklist = null);
    }
}
