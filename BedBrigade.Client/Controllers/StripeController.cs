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

    public StripeController(
        IConfigurationDataService configurationDataService,
        IPaymentService paymentService)
    {
        _configurationDataService = configurationDataService;
        _paymentService = paymentService;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            string endpointSecret = await _configurationDataService.GetConfigValueAsync(
                ConfigSection.Payments,
                ConfigNames.StripeWebhookSecret
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
