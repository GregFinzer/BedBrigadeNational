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

        var locationResult = await _locationDataService.GetValidLocationIdsForUser();

        if (!locationResult.Success || locationResult.Data == null)
        {
            throw new Exception($"Failed to retrieve valid location IDs for user. Message: {locationResult.Message}");
        }

        return locationResult.Data.Contains(locationId);
    }


}
