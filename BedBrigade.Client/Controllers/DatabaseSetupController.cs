using BedBrigade.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using BedBrigade.Data.Services;
using Swashbuckle.AspNetCore.Annotations;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/[controller]")]
/// <summary>
/// Provides a hidden maintenance endpoint for applying database migrations and seed data.
/// </summary>
public class DatabaseSetupController : ControllerBase
{
    private readonly IMigrationDataService _migrationDataService;

    public DatabaseSetupController(IMigrationDataService migrationDataService)
    {
        _migrationDataService = migrationDataService;
    }

    /// <summary>
    /// Applies pending database migrations and seed data after validating the setup password in production.
    /// </summary>
    [HttpGet("PerformSetup")]
    [Produces("application/json")]
    [SwaggerResponse(statusCode: 200, type: typeof(object), description: "Database setup completed")]
    [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Database setup request failed")]
    [SwaggerResponse(statusCode: 401, type: typeof(ApiError), description: "Invalid setup password")]
    [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PerformSetup([FromQuery] string? password)
    {
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
        {
            string? apiPassword = Environment.GetEnvironmentVariable("ApiPassword");
            if (string.IsNullOrEmpty(apiPassword))
            {
                return BadRequest(new { Message = "ApiPassword is not set in environment variables." });
            }
            if (password != apiPassword)
            {
                return Unauthorized(new { Message = "Invalid ApiPassword" });
            }
        }

        try
        {
            bool migrated = await _migrationDataService.MigrateAsync();
            if (migrated)
            {
                if (await _migrationDataService.SeedAsync())
                {
                    return Ok(new { Message = "Database setup completed successfully." });
                }
                else
                {
                    return BadRequest(new { Message = "Migration successful, but seeding failed." });
                }
            }
            else
            {
                return BadRequest(new { Message = "Migration failed." });
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error in database setup: " + ex.Message);
            return StatusCode(500, new { Message = "An error occurred during database setup." });
        }
    }
}
