﻿
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class DonationDataService : Repository<Donation>, IDonationDataService
{
    private readonly ICommonService _commonService;

    public DonationDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        AuthenticationStateProvider authProvider, ICommonService commonService) : base(contextFactory, cachingService, authProvider)
    {
        _commonService = commonService;
    }

    public async Task<ServiceResponse<List<Donation>>> GetAllForLocationAsync()
    {
        return await _commonService.GetAllForLocationAsync(this);
    }
}



