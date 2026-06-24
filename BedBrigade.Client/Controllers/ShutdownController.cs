using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;

namespace BedBrigade.Client.Controllers
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/[controller]")]
    /// <summary>
    /// Provides a hidden maintenance endpoint for stopping background services.
    /// </summary>
    public class ShutdownController : Controller
    {
        private TranslationBackgroundService _translationBackgroundService;
        private GeoLocationBackgroundService _geoLocationBackgroundService;
        private readonly ISmsState _smsState;
        private EmailQueueBackgroundService _emailQueueBackgroundService;
        private SmsQueueBackgroundService _smsQueueBackgroundService;

        public ShutdownController(TranslationBackgroundService translationBackgroundService, GeoLocationBackgroundService geoLocationBackgroundService, ISmsState smsState, EmailQueueBackgroundService emailQueueBackgroundService, SmsQueueBackgroundService smsQueueBackgroundService)
        {
            _translationBackgroundService = translationBackgroundService;
            _geoLocationBackgroundService = geoLocationBackgroundService;
            _smsState = smsState;
            _emailQueueBackgroundService = emailQueueBackgroundService;
            _smsQueueBackgroundService = smsQueueBackgroundService;
        }

        /// <summary>
        /// Stops background services after validating the shutdown password in production.
        /// </summary>
        [HttpGet("PerformShutdown")]
        [Produces("application/json")]
        [SwaggerResponse(statusCode: 200, type: typeof(object), description: "Background services stopped")]
        [SwaggerResponse(statusCode: 400, type: typeof(ApiError), description: "Shutdown request failed")]
        [SwaggerResponse(statusCode: 401, type: typeof(ApiError), description: "Invalid shutdown password")]
        [SwaggerResponse(statusCode: 500, type: typeof(ApiError), description: "An unexpected error occurred")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PerformShutdown([FromQuery] string? password)
        {
            try
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

                _smsState.StopService();
                _translationBackgroundService.StopService();
                _geoLocationBackgroundService.StopService();
                _emailQueueBackgroundService.StopService();
                _smsQueueBackgroundService.StopService();
                return Ok(new { Message = "Shutdown of Background Services completed successfully." });
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error in shutdown: " + ex.Message);
                return StatusCode(500, new { Message = $"An error occurred during database setup: {ex.Message}" });
            }
        }
    }
}
