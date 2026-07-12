using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace BedBrigade.Client.Controllers;

public abstract class RepositoryControllerBase<TEntity, TKey, TService> : ControllerBase
    where TEntity : class
    where TService : IRepository<TEntity>
{
    protected RepositoryControllerBase(TService dataService, Func<TEntity, TKey> getId)
    {
        DataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        GetId = getId ?? throw new ArgumentNullException(nameof(getId));
    }

    protected TService DataService { get; }

    protected Func<TEntity, TKey> GetId { get; }

    protected async Task<ActionResult<List<TEntity>>> GetAllCoreAsync(
        Func<Task<ServiceResponse<List<TEntity>>>>? getAll = null,
        string? errorDisplayName = null)
    {
        try
        {
            ServiceResponse<List<TEntity>> result =
                getAll == null ? await DataService.GetAllAsync() : await getAll();
            if (!result.Success || result.Data == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, CreateApiError(result.Message));
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", GetType().Name, nameof(GetAllCoreAsync));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError($"There was an error getting {errorDisplayName ?? GetEntityDisplayName()}, try again later."));
        }
    }

    protected async Task<ActionResult<PageResponse<TEntity>>> GetPageCoreAsync(
        int pageNumber,
        int itemsPerPage,
        IConfigurationDataService configurationDataService,
        List<TEntity> entities,
        string? errorDisplayName = null)
    {
        ArgumentNullException.ThrowIfNull(configurationDataService);
        ArgumentNullException.ThrowIfNull(entities);

        try
        {
            int maxItemsPerPage = await configurationDataService.GetConfigValueAsIntAsync(
                ConfigSection.System, ConfigNames.MaxItemsPerPage);
            ActionResult? validationResult = ValidatePagingParameters(pageNumber, itemsPerPage, maxItemsPerPage);
            if (validationResult != null)
            {
                return validationResult;
            }

            return Ok(CreatePageResponse(entities, pageNumber, itemsPerPage, maxItemsPerPage));
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", GetType().Name, nameof(GetPageCoreAsync));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError($"There was an error getting {errorDisplayName ?? GetEntityDisplayName()}, try again later."));
        }
    }

    protected async Task<ActionResult<TEntity>> GetByIdCoreAsync(TKey id)
    {
        try
        {
            ServiceResponse<TEntity> result = await DataService.GetByIdAsync(id!);
            if (!result.Success || result.Data == null)
            {
                return NotFound(CreateApiError(result.Message));
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", GetType().Name, nameof(GetByIdCoreAsync));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError($"There was an error getting the {GetEntityDisplayName()}, try again later."));
        }
    }

    protected async Task<ActionResult<TEntity>> CreateCoreAsync(TEntity entity)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(CreateApiError(GetValidationMessage()));
        }

        try
        {
            ServiceResponse<TEntity> result = await DataService.CreateAsync(entity);
            if (!result.Success || result.Data == null)
            {
                return BadRequest(CreateApiError(result.Message));
            }

            return CreatedAtAction("GetByIdAsync", new { id = GetId(result.Data) }, result.Data);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", GetType().Name, nameof(CreateCoreAsync));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError($"There was an error creating the {GetEntityDisplayName()}, try again later."));
        }
    }

    protected async Task<ActionResult<TEntity>> UpdateCoreAsync(TKey id, TEntity entity)
    {
        if (!EqualityComparer<TKey>.Default.Equals(id, GetId(entity)))
        {
            return BadRequest(CreateApiError($"The route id must match the {GetEntityDisplayName()} id."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateApiError(GetValidationMessage()));
        }

        try
        {
            ServiceResponse<TEntity> result = await DataService.UpdateAsync(entity);
            if (!result.Success || result.Data == null)
            {
                return BadRequest(CreateApiError(result.Message));
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", GetType().Name, nameof(UpdateCoreAsync));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError($"There was an error updating the {GetEntityDisplayName()}, try again later."));
        }
    }

    protected async Task<IActionResult> DeleteCoreAsync(TKey id)
    {
        try
        {
            ServiceResponse<bool> result = await DataService.DeleteAsync(id!);
            if (!result.Success)
            {
                return NotFound(CreateApiError(result.Message));
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in {Controller}.{Action}", GetType().Name, nameof(DeleteCoreAsync));
            return StatusCode(StatusCodes.Status500InternalServerError,
                CreateApiError($"There was an error deleting the {GetEntityDisplayName()}, try again later."));
        }
    }

    protected static ApiError CreateApiError(string message)
    {
        return new ApiError { Message = message };
    }

    protected ActionResult? ValidatePagingParameters(int pageNumber, int itemsPerPage, int maxItemsPerPage)
    {
        if (pageNumber < 1)
        {
            return StatusCode(StatusCodes.Status400BadRequest,
                CreateApiError("pageNumber must be greater than or equal to 1."));
        }

        if (itemsPerPage < 1 || itemsPerPage > maxItemsPerPage)
        {
            return StatusCode(StatusCodes.Status400BadRequest,
                CreateApiError($"itemsPerPage must be between 1 and {maxItemsPerPage}."));
        }

        return null;
    }

    protected static PageResponse<TEntity> CreatePageResponse(List<TEntity> entities,
        int pageNumber, int itemsPerPage, int maxItemsPerPage)
    {
        int normalizedPageNumber = Math.Max(pageNumber, 1);
        int normalizedItemsPerPage = Math.Clamp(itemsPerPage, 1, maxItemsPerPage);
        int numberOfItems = entities.Count;
        int maxPage = numberOfItems == 0
            ? 0
            : (int)Math.Ceiling(numberOfItems / (double)normalizedItemsPerPage);
        long itemsToSkip = ((long)normalizedPageNumber - 1) * normalizedItemsPerPage;
        List<TEntity> pageItems = itemsToSkip > int.MaxValue
            ? []
            : entities
                .Skip((int)itemsToSkip)
                .Take(normalizedItemsPerPage)
                .ToList();

        return new PageResponse<TEntity>
        {
            PageNumber = normalizedPageNumber,
            MaxPage = maxPage,
            NumberOfItems = numberOfItems,
            ItemsPerPage = normalizedItemsPerPage,
            Items = pageItems
        };
    }

    protected virtual string GetEntityDisplayName()
    {
        return typeof(TEntity).Name.ToLowerInvariant();
    }

    protected string GetValidationMessage()
    {
        return ModelState.Values
                   .SelectMany(modelState => modelState.Errors)
                   .Select(error => error.ErrorMessage)
                   .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
               ?? $"The {GetEntityDisplayName()} is invalid.";
    }
}
