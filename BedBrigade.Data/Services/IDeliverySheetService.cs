using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IDeliverySheetService
    {
        string CreateDeliverySheetFileName(Location location, List<BedRequest> bedRequests);
        Stream CreateDeliverySheet(Location location, List<BedRequest> bedRequests, string? deliveryChecklist = null);
    }
}
