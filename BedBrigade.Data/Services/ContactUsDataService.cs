using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace BedBrigade.Data.Services;

public class ContactUsDataService : Repository<ContactUs>, IContactUsDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    private readonly ICommonService _commonService;

    public ContactUsDataService(IDbContextFactory<DataContext> contextFactory, 
        ICachingService cachingService,
        IAuthService authService,
        ICommonService commonService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
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

    public async Task<ServiceResponse<List<string>>> GetDistinctPhone()
    {
        return await _commonService.GetDistinctPhone(this);
    }

    public async Task<ServiceResponse<List<string>>> GetDistinctPhoneByLocation(int locationId)
    {
        return await _commonService.GetDistinctPhoneByLocation(this, locationId);
    }

    public async Task<ServiceResponse<List<ContactUs>>> GetAllForLocationList(List<int> locationIds)
    {
        return await _commonService.GetAllForLocationList(this, locationIds);
    }

    public async Task<int> CancelContactRequestedForBouncedEmail(List<string> emailList)
    {
        string userName = GetUserName() ?? Defaults.DefaultUserNameAndEmail;
        using (var ctx = _contextFactory.CreateDbContext())
        {
            var lowerEmailList = emailList.Select(e => e.ToLower()).ToList();

            int updated = await ctx.Set<ContactUs>()
                .Where(o => lowerEmailList.Contains(o.Email.ToLower())
                            && o.Status == ContactUsStatus.ContactRequested)
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(o => o.UpdateUser, userName)
                    .SetProperty(o => o.UpdateDate, DateTime.UtcNow)
                    .SetProperty(o => o.MachineName, Environment.MachineName)
                    .SetProperty(o => o.Status, o => ContactUsStatus.Cancelled)
                    .SetProperty(o => o.Message,
                        o => (o.Message ?? "") + " | Cancelled due to bounced email"));

            if (updated > 0)
            {
                _cachingService.ClearByEntityName(GetEntityName());
            }

            return updated;
        }
    }
}



