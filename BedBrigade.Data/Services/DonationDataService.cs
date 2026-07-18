using System.Data.Common;
using BedBrigade.Common.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace BedBrigade.Data.Services;

public class DonationDataService : Repository<Donation>, IDonationDataService
{
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly ICachingService _cachingService;
    private readonly ICommonService _commonService;

    public DonationDataService(IDbContextFactory<DataContext> contextFactory,
        ICachingService cachingService,
        IAuthService authService,
        ICommonService commonService) : base(contextFactory, cachingService, authService)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _commonService = commonService;
    }

    public async Task<ServiceResponse<List<Donation>>> GetAllForLocationAsync(int locationId)
    {
        return await _commonService.GetAllForLocationAsync(this, locationId);
    }

    public async Task<ServiceResponse<List<Donation>>> GetByYearAsync(int year)
    {
        if (year < 1000 || year > 9999)
        {
            return new ServiceResponse<List<Donation>>("Year must be a four-digit number.");
        }

        string cacheKey = _cachingService.BuildCacheKey(GetEntityName(), $"GetByYearAsync({year})");
        List<Donation>? cachedContent = _cachingService.Get<List<Donation>>(cacheKey);
        if (cachedContent != null)
        {
            return new ServiceResponse<List<Donation>>($"Found {cachedContent.Count} {GetEntityName()} in cache", true,
                cachedContent);
        }

        try
        {
            using var ctx = _contextFactory.CreateDbContext();
            List<Donation> result = await ctx.Set<Donation>()
                .Where(donation => donation.DonationDate.HasValue && donation.DonationDate.Value.Year == year)
                .ToListAsync();

            _cachingService.Set(cacheKey, result);
            return new ServiceResponse<List<Donation>>($"Found {result.Count} {GetEntityName()} records for year {year}",
                true, result);
        }
        catch (DbException ex)
        {
            return new ServiceResponse<List<Donation>>(
                $"Error GetByYearAsync for {GetEntityName()} with year {year}: {ex.Message} ({ex.ErrorCode})");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving {EntityName} for year {Year}", GetEntityName(), year);
            return new ServiceResponse<List<Donation>>(
                $"Unexpected error GetByYearAsync for {GetEntityName()} with year {year}: {ex.Message}");
        }
    }
}
