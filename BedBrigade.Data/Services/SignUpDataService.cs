using BedBrigade.Client.Services;
using BedBrigade.Common.Models;

using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class SignUpDataService : Repository<SignUp>, ISignUpDataService
{
    private readonly ICachingService _cachingService;
    private readonly ICommonService _commonService;

    public SignUpDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        IAuthService authService, ICommonService commonService) : base(contextFactory, cachingService, authService)
    {
        _cachingService = cachingService;
        _commonService = commonService;
    }

    public override async Task<ServiceResponse<SignUp>> CreateAsync(SignUp entity)
    {
        var result = await base.CreateAsync(entity);
        _cachingService.ClearScheduleRelated();
        return result;
    }

    public override async Task<ServiceResponse<SignUp>> UpdateAsync(SignUp entity)
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

    public async Task<ServiceResponse<List<SignUp>>> GetAllForLocationAsync(int locationId)
    {
        return await _commonService.GetAllForLocationAsync(this, locationId);
    }

}



