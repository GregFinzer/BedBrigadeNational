using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Serilog;
using Syncfusion.Blazor.Grids;

namespace BedBrigade.Client.Services;

/// <summary>
/// Provides safe loading of Syncfusion grid persistence data.
/// Handles the case where persisted data is stale (e.g., after grid columns are added)
/// by clearing the stale data and returning false so the grid falls back to its default state.
/// </summary>
public static class GridPersistenceHelper
{
    /// <summary>
    /// Loads grid persistence data and applies it to the specified grid.
    /// If the persisted state is incompatible with the current grid definition,
    /// the stale data is deleted and false is returned so the grid can use its defaults.
    /// </summary>
    /// <typeparam name="T">The grid row model type.</typeparam>
    /// <param name="grid">The Syncfusion grid instance.</param>
    /// <param name="userPersistDataService">The user persistence data service.</param>
    /// <param name="userName">The current user name.</param>
    /// <param name="gridType">The grid type identifier.</param>
    /// <returns>True if persistence data was successfully applied; false otherwise.</returns>
    public static async Task<bool> LoadGridPersistenceAsync<T>(
        SfGrid<T>? grid,
        IUserPersistDataService? userPersistDataService,
        string userName,
        PersistGrid gridType) where T : class
    {
        if (grid == null || userPersistDataService == null)
            return false;

        UserPersist persist = new UserPersist { UserName = userName, Grid = gridType };
        var result = await userPersistDataService.GetGridPersistence(persist);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Data))
            return false;

        try
        {
            await grid.SetPersistDataAsync(result.Data);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning(ex,
                "Stale grid persistence data for {UserName}/{GridType}. Clearing so the grid falls back to defaults.",
                userName, gridType);
            await userPersistDataService.DeleteGridPersistenceAsync(userName, gridType);
            return false;
        }
    }
}

