using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class ContactUsDataService : Repository<ContactUs>, IContactUsDataService
{
    private readonly ICommonService _commonService;

    public ContactUsDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        AuthenticationStateProvider authProvider,
        ICommonService commonService) : base(contextFactory, cachingService, authProvider)
    {
        _commonService = commonService;
    }

    public async Task<ServiceResponse<List<ContactUs>>> GetAllForLocationAsync()
    {
        return await _commonService.GetAllForLocationAsync(this);
    }
}



