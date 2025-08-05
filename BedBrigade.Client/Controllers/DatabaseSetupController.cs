using Microsoft.AspNetCore.Mvc;
using Serilog;
using BedBrigade.Data.Services;

[ApiController]
[Route("api/[controller]")]
public class DatabaseSetupController : ControllerBase
{
    private readonly IMigrationDataService _migrationDataService;

    public DatabaseSetupController(IMigrationDataService migrationDataService)
    {
        _migrationDataService = migrationDataService;
    }

    [HttpGet("PerformSetup")]
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