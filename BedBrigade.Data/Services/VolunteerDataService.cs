
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Common;
using System.Security.Claims;
using System.Security.Principal;
using BedBrigade.Common;

namespace BedBrigade.Data.Services;

public class VolunteerDataService : Repository<Volunteer>, IVolunteerDataService
{

    private readonly ICachingService _cachingService;
    private readonly IDbContextFactory<DataContext> _contextFactory;
    private readonly AuthenticationStateProvider _auth;
    private readonly ICommonService _commonService;

    public VolunteerDataService(IDbContextFactory<DataContext> contextFactory, ICachingService cachingService,
        AuthenticationStateProvider authProvider, ICommonService commonService) : base(contextFactory, cachingService, authProvider)
    {
        _contextFactory = contextFactory;
        _cachingService = cachingService;
        _auth = authProvider;
        _commonService = commonService;
    }

    public async Task<ServiceResponse<List<Volunteer>>> GetAllForLocationAsync()
    {
        return await _commonService.GetAllForLocationAsync(this);
    }











}



