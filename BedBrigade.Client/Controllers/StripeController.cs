using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Stripe;

namespace BedBrigade.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StripeController : ControllerBase
{
    private readonly IConfigurationDataService _configurationDataService;
    private readonly IPaymentService _paymentService;
    private readonly ILocationDataService _locationDataService;

    public StripeController(
        IConfigurationDataService configurationDataService,
        IPaymentService paymentService, 
        ILocationDataService locationDataService)
    {
        _configurationDataService = configurationDataService;
        _paymentService = paymentService;
        _locationDataService = locationDataService;
    }

    [HttpPost("webhook/{locationRoute}")]
    public async Task<IActionResult> Index(string locationRoute)
    {
        try
        {
            var locationResult = await _locationDataService.GetLocationByRouteAsync(locationRoute);

            if (!locationResult.Success || locationResult.Data == null)
                return BadRequest();

            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            string endpointSecret = await _configurationDataService.GetConfigValueAsync(
                ConfigSection.Payments,
                ConfigNames.StripeLocationWebhookSecret,
                locationResult.Data.LocationId
            );
            Event stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                endpointSecret
            );

            var result = await _paymentService.HandleWebhook(stripeEvent);

            if (!result.Success)
                return BadRequest();

            return Ok();
        }
        catch (StripeException e)
        {
            Log.Error($"Stripe webhook error: {e.Message}");
            return BadRequest();
        }
    }
}
