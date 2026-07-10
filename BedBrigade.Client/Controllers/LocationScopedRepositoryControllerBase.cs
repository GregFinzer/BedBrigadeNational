using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Mvc;

namespace BedBrigade.Client.Controllers;

public abstract class LocationScopedRepositoryControllerBase<TEntity, TKey, TService>
    : RepositoryControllerBase<TEntity, TKey, TService>
    where TEntity : class, ILocationId
    where TService : IRepository<TEntity>
{
    private readonly ILocationDataService _locationDataService;

    protected LocationScopedRepositoryControllerBase(
        TService dataService,
        ILocationDataService locationDataService,
        Func<TEntity, TKey> getId) : base(dataService, getId)
    {
        _locationDataService = locationDataService ?? throw new ArgumentNullException(nameof(locationDataService));
    }

    protected async Task<ActionResult<List<TEntity>>> GetScopedAllCoreAsync(
        Func<Task<ServiceResponse<List<TEntity>>>>? getAll = null,
        string? errorDisplayName = null)
    {
        if (DataService.IsUserNationalAdmin())
        {
            return await GetAllCoreAsync(getAll, errorDisplayName);
        }

        ActionResult<List<int>> locationIdsResult = await GetAllowedLocationIdsAsync();
        if (locationIdsResult.Result != null)
        {
            return locationIdsResult.Result;
        }

        List<int> locationIds = locationIdsResult.Value!;
        return await GetAllCoreAsync(async () =>
        {
            ServiceResponse<List<TEntity>> result =
                getAll == null ? await DataService.GetAllAsync() : await getAll();
            if (!result.Success || result.Data == null)
            {
                return result;
            }

            List<TEntity> scopedEntities = result.Data
                .Where(entity => locationIds.Contains(entity.LocationId))
                .ToList();
            return new ServiceResponse<List<TEntity>>(result.Message, true, scopedEntities);
        }, errorDisplayName);
    }

    protected async Task<ActionResult<List<TEntity>>> GetLocationAllCoreAsync(
        Func<Task<ServiceResponse<List<TEntity>>>>? getAll = null,
        string? errorDisplayName = null)
    {
        int locationId = DataService.GetUserLocationId();
        return await GetAllCoreAsync(async () =>
        {
            ServiceResponse<List<TEntity>> result =
                getAll == null ? await DataService.GetAllAsync() : await getAll();
            if (!result.Success || result.Data == null)
            {
                return result;
            }

            List<TEntity> scopedEntities = result.Data
                .Where(entity => entity.LocationId == locationId)
                .ToList();
            return new ServiceResponse<List<TEntity>>(result.Message, true, scopedEntities);
        }, errorDisplayName);
    }
    
    // protected async Task<ActionResult<PageResponse<TEntity>>> GetLocationPageCoreAsync(
    //     int pageNumber,
    //     int itemsPerPage,
    //     IConfigurationDataService configurationDataService,
    //     Func<Task<ServiceResponse<List<TEntity>>>>? getAll = null,
    //     string? errorDisplayName = null)
    // {
    //
    //     int locationId = configurationDataService.GetUserLocationId();
    //     return await GetPageCoreAsync(pageNumber, itemsPerPage, configurationDataService, async () =>
    //     {
    //         ServiceResponse<List<TEntity>> result =
    //             getAll == null ? await DataService.GetAllAsync() : await getAll();
    //         if (!result.Success || result.Data == null)
    //         {
    //             return result;
    //         }
    //
    //         List<TEntity> scopedEntities = result.Data
    //             .Where(entity => entity.LocationId == locationId)
    //             .ToList();
    //         return new ServiceResponse<List<TEntity>>(result.Message, true, scopedEntities);
    //     }, errorDisplayName);
    // }
    
    protected async Task<ActionResult<PageResponse<TEntity>>> GetScopedPageCoreAsync(
        int pageNumber,
        int itemsPerPage,
        IConfigurationDataService configurationDataService,
        Func<Task<ServiceResponse<List<TEntity>>>>? getAll = null,
        string? errorDisplayName = null)
    {
        if (DataService.IsUserNationalAdmin())
        {
            return await GetPageCoreAsync(pageNumber, itemsPerPage, configurationDataService, getAll, errorDisplayName);
        }

        ActionResult<List<int>> locationIdsResult = await GetAllowedLocationIdsAsync();
        if (locationIdsResult.Result != null)
        {
            return locationIdsResult.Result;
        }

        List<int> locationIds = locationIdsResult.Value!;
        return await GetPageCoreAsync(pageNumber, itemsPerPage, configurationDataService, async () =>
        {
            ServiceResponse<List<TEntity>> result =
                getAll == null ? await DataService.GetAllAsync() : await getAll();
            if (!result.Success || result.Data == null)
            {
                return result;
            }

            List<TEntity> scopedEntities = result.Data
                .Where(entity => locationIds.Contains(entity.LocationId))
                .ToList();
            return new ServiceResponse<List<TEntity>>(result.Message, true, scopedEntities);
        }, errorDisplayName);
    }

    protected async Task<ActionResult<TEntity>> GetScopedByIdCoreAsync(TKey id)
    {
        ActionResult<TEntity> result = await GetByIdCoreAsync(id);
        if (result.Result is not OkObjectResult okResult || okResult.Value is not TEntity entity)
        {
            return result;
        }

        if (!await CanAccessLocationAsync(entity.LocationId))
        {
            return Forbid();
        }

        return result;
    }

    protected async Task<ActionResult<TEntity>> CreateScopedCoreAsync(TEntity entity)
    {
        if (!await CanAccessLocationAsync(entity.LocationId))
        {
            return Forbid();
        }

        return await CreateCoreAsync(entity);
    }

    protected async Task<ActionResult<TEntity>> UpdateScopedCoreAsync(TKey id, TEntity entity)
    {
        if (!EqualityComparer<TKey>.Default.Equals(id, GetId(entity)))
        {
            return BadRequest(CreateApiError($"The route id must match the {GetEntityDisplayName()} id."));
        }

        ActionResult<TEntity> existingResult = await GetByIdCoreAsync(id);
        if (existingResult.Result is not OkObjectResult okResult || okResult.Value is not TEntity existingEntity)
        {
            return existingResult;
        }

        if (!await CanAccessLocationAsync(existingEntity.LocationId)
            || !await CanAccessLocationAsync(entity.LocationId))
        {
            return Forbid();
        }

        return await UpdateCoreAsync(id, entity);
    }

    protected async Task<IActionResult> DeleteScopedCoreAsync(TKey id)
    {
        ActionResult<TEntity> existingResult = await GetByIdCoreAsync(id);
        if (existingResult.Result is not OkObjectResult okResult || okResult.Value is not TEntity entity)
        {
            return existingResult.Result ?? StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError($"There was an error deleting the {GetEntityDisplayName()}, try again later."));
        }

        if (!await CanAccessLocationAsync(entity.LocationId))
        {
            return Forbid();
        }

        return await DeleteCoreAsync(id);
    }

    private async Task<bool> CanAccessLocationAsync(int locationId)
    {
        if (DataService.IsUserNationalAdmin())
        {
            return true;
        }

        ActionResult<List<int>> locationIdsResult = await GetAllowedLocationIdsAsync();
        return locationIdsResult.Result == null && locationIdsResult.Value!.Contains(locationId);
    }

    private async Task<ActionResult<List<int>>> GetAllowedLocationIdsAsync()
    {
        int userLocationId = DataService.GetUserLocationId();
        ServiceResponse<Location> userLocationResult = await _locationDataService.GetByIdAsync(userLocationId);
        if (!userLocationResult.Success || userLocationResult.Data == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError(userLocationResult.Message));
        }

        Location userLocation = userLocationResult.Data;
        if (userLocation.IsMetroLocation() && userLocation.MetroAreaId.HasValue)
        {
            ServiceResponse<List<Location>> metroLocationsResult =
                await _locationDataService.GetLocationsByMetroAreaId(userLocation.MetroAreaId.Value);
            if (!metroLocationsResult.Success || metroLocationsResult.Data == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    CreateApiError(metroLocationsResult.Message));
            }

            return metroLocationsResult.Data.Select(location => location.LocationId).Distinct().ToList();
        }

        return new List<int> { userLocation.LocationId };
    }
}
