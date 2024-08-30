using BedBrigade.Common.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class VolunteerEventsDataService : Repository<VolunteerEvent>, IVolunteerEventsDataService
{
    private readonly ICachingService _cachingService;
    private readonly ICommonService _commonService;

    public VolunteerEventsDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        AuthenticationStateProvider authProvider, ICommonService commonService) : base(contextFactory, cachingService, authProvider)
    {
        _cachingService = cachingService;
        _commonService = commonService;
    }

    public override async Task<ServiceResponse<VolunteerEvent>> CreateAsync(VolunteerEvent entity)
    {
        var result = await base.CreateAsync(entity);
        _cachingService.ClearScheduleRelated();
        return result;
    }

    public override async Task<ServiceResponse<VolunteerEvent>> UpdateAsync(VolunteerEvent entity)
    {
        var result = await base.UpdateAsync(entity);
        _cachingService.ClearScheduleRelated();
        return result;
    }

    public override async Task<ServiceResponse<bool>> DeleteAsync(object id)
    {
        var result = await base.DeleteAsync(id);
        _cachingService.ClearScheduleRelated();
        return result;
    }

    public async Task<ServiceResponse<List<VolunteerEvent>>> GetAllForLocationAsync(int locationId)
    {
        return await _commonService.GetAllForLocationAsync(this, locationId);
    }

}



