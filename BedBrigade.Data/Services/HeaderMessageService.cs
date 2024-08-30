using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public class HeaderMessageService : IHeaderMessageService
    {
        private readonly ILocationDataService _locationDataService;
        private readonly IMetroAreaDataService _metroAreaDataService;

        public HeaderMessageService(ILocationDataService locationDataService, 
            IMetroAreaDataService metroAreaDataService)
        {
            _locationDataService = locationDataService;
            _metroAreaDataService = metroAreaDataService;
        }

        public async Task<ServiceResponse<string>> GetManageEntitiesLegendText(string entitiesName)
        {
            int locationId = await _locationDataService.GetUserLocationId();
            var locationResult = await _locationDataService.GetByIdAsync(locationId);

            if (!locationResult.Success || locationResult.Data == null)
            {
                return new ServiceResponse<string>($"Location {locationId} not found");
            }

            bool isNationalAdmin = await _locationDataService.IsUserNationalAdmin();
            if (isNationalAdmin)
            {
                return new ServiceResponse<string>("Success", true, $"Manage {entitiesName} for All Locations");
            }

            if (locationResult.Data.MetroAreaId.HasValue && locationResult.Data.MetroAreaId > Defaults.MetroAreaNoneId)
            {
                var metroAreaResult = await _metroAreaDataService.GetByIdAsync(locationResult.Data.MetroAreaId.Value);

                if (metroAreaResult.Success && metroAreaResult.Data != null)
                {
                    return new ServiceResponse<string>("Success", true, $"Manage {entitiesName} for the {metroAreaResult.Data.Name} Metro Area");
                }

                return new ServiceResponse<string>("Metro Area not found");
            }

            return new ServiceResponse<string>("Success", true, $"Manage {entitiesName} for {locationResult.Data.Name}");
        }
    }
}
