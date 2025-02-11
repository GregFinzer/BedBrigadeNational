using BedBrigade.Client.Services;
using BedBrigade.Common.Models;

using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class ContactUsDataService : Repository<ContactUs>, IContactUsDataService
{
    private readonly ICommonService _commonService;

    public ContactUsDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        IAuthService authService,
        ICommonService commonService) : base(contextFactory, cachingService, authService)
    {
        _commonService = commonService;
    }

    public async Task<ServiceResponse<List<ContactUs>>> GetAllForLocationAsync(int locationId)
    {
        return await _commonService.GetAllForLocationAsync(this, locationId);
    }

    public async Task<ServiceResponse<List<string>>> GetDistinctEmail()
    {
        return await _commonService.GetDistinctEmail(this);
    }

    public async Task<ServiceResponse<List<string>>> GetDistinctEmailByLocation(int locationId)
    {
        return await _commonService.GetDistinctEmailByLocation(this, locationId);
    }

    public async Task<ServiceResponse<ContactUs>> GetByPhone(string phone)
    {
        return await _commonService.GetByPhone(this, phone);
    }
}



