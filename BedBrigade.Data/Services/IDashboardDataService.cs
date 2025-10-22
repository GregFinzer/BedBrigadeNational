using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface IDashboardDataService
    {
        Task<ServiceResponse<List<BedRequestDashboardRow>>> GetWaitingDashboard(int userLocationId);
        Task<ServiceResponse<List<BedRequestHistoryRow>>> GetBedRequestHistory(int locationId);
        Task<ServiceResponse<List<BedRequestHistoryRow>>> GetBedDeliveryHistory(int locationId);
        Task<ServiceResponse<string>> GetEstimatedWaitTime(int locationId);
        Task<ServiceResponse<List<NationalDelivery>>> GetNationalDeliveries();
        Task<ServiceResponse<List<DeliveryPlan>>> GetDeliveryPlan(int locationId);
    }
}
