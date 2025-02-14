using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace BedBrigade.Client.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        [HttpGet("PerformShutdown")]
        public async Task<IActionResult> PerformShutdown([FromQuery] string? password)
        {
            try
            {
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
                {
                    string? shutdownPassword = Environment.GetEnvironmentVariable("ShutdownPassword");
                    if (string.IsNullOrEmpty(shutdownPassword) || password != shutdownPassword)
                    {
                        return BadRequest(new { Message = "Invalid shutdown password" });
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
