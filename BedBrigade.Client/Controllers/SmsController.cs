using BedBrigade.Common.Logic;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Twilio.AspNet.Core;
using Twilio.TwiML;

namespace BedBrigade.Client.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/[controller]")]
/// <summary>
/// Provides hidden endpoints for receiving inbound SMS callbacks.
/// </summary>
public class SmsController : TwilioController
{
    private ISmsQueueDataService _smsQueueDataService;

    public SmsController(ISmsQueueDataService smsQueueDataService)
    {
        _smsQueueDataService = smsQueueDataService;
    }

    /// <summary>
    /// Receives an inbound Twilio SMS callback and queues the reply handling.
    /// </summary>
    [HttpPost("receive")]
    public async Task<IActionResult> ReceiveSms()
    {
        // Extract parameters sent by Twilio (sent as form POST data).
        string fromNumber = Request.Form["From"].ToString();
        string toNumber= Request.Form["To"].ToString();
        string messageBody = Request.Form["Body"].ToString();

        // Log or process the incoming message as needed.
        Log.Information($"SMS Received from {fromNumber.FormatPhoneNumber()} to {toNumber.FormatPhoneNumber()} | {messageBody}");

        await _smsQueueDataService.CreateSmsReply(fromNumber, toNumber, messageBody);

        // Create a TwiML response. This example sends an empty reply back
        var messagingResponse = new MessagingResponse();

        // Return the TwiML response to Twilio as XML.
        return TwiML(messagingResponse);
    }
}
