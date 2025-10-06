using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using Microsoft.Extensions.Logging;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;

namespace BedBrigade.Data.Services;

public class EmailBounceService : IEmailBounceService
{
    private readonly IConfigurationDataService _configurationDataService;
    private readonly IBedRequestDataService _bedRequestDataService;
    private readonly IContactUsDataService _contactUsDataService;
    private readonly ILogger<EmailBounceService> _logger;

    private readonly List<string> _bounceKeywords = new();

    public EmailBounceService(IConfigurationDataService configurationDataService,
        IBedRequestDataService bedRequestDataService,
        IContactUsDataService contactUsDataService,
        ILogger<EmailBounceService> logger)
    {
        _configurationDataService = configurationDataService;
        _bedRequestDataService = bedRequestDataService;
        _contactUsDataService = contactUsDataService;
        _logger = logger;

        Init();
    }

    private void Init()
    {
        _bounceKeywords.Add("Delivery Failure");
        _bounceKeywords.Add("Delivery has failed");
        _bounceKeywords.Add("Mail System Error");
        _bounceKeywords.Add("Returned mail");
        _bounceKeywords.Add("Undeliverable Mail");
        _bounceKeywords.Add("Mail delivery failed");
        _bounceKeywords.Add("Delivery Status Notification (Failure)");
        _bounceKeywords.Add("DELIVERY FAILURE");
        _bounceKeywords.Add("Failed mail");
        _bounceKeywords.Add("failure notice");
        _bounceKeywords.Add("Message Delivery Failure");
        _bounceKeywords.Add("undelivered mail");
    }

    public async Task ProcessBounces(CancellationToken cancellationToken = default)
    {
        try
        {
            bool useFileMock = await _configurationDataService.GetConfigValueAsBoolAsync(ConfigSection.Email, ConfigNames.EmailUseFileMock);
            if (useFileMock)
            {
                _logger.LogInformation("Email bounce processing is disabled because EmailUseFileMock is true.");
                return;
            }

            List<string> bouncedEmails = await GetBounces(cancellationToken);
            if (bouncedEmails.Count == 0)
            {
                _logger.LogInformation("No bounced emails found.");
                return;
            }

            _logger.LogInformation($"{bouncedEmails.Count} Bounced emails: {String.Join(',', bouncedEmails)}");
            int cancelledBeRequests = await _bedRequestDataService.CancelWaitingForBouncedEmail(bouncedEmails);

            if (cancelledBeRequests > 0)
            {
                _logger.LogInformation($"{cancelledBeRequests} Bed Requests cancelled due to bounced email.");
            }
            int cancelledContactUs = await _contactUsDataService.CancelContactRequestedForBouncedEmail(bouncedEmails);

            if (cancelledContactUs > 0)
            {
                _logger.LogInformation($"{cancelledContactUs} Contact Requests cancelled due to bounced email.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProcessBounces");
        }
    }

    private async Task<List<string>> GetBounces(CancellationToken cancellationToken = default)
    {
        var bounces = new List<string>();

        string host = await _configurationDataService.GetConfigValueAsync(ConfigSection.Email, ConfigNames.EmailHost);
        int port = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.Email,
            ConfigNames.EmailImapPort);
        string username = await _configurationDataService.GetConfigValueAsync(ConfigSection.Email, ConfigNames.EmailUserName);
        string password = await _configurationDataService.GetConfigValueAsync(ConfigSection.Email, ConfigNames.EmailPassword);
        bool useSsl =
            await _configurationDataService.GetConfigValueAsBoolAsync(ConfigSection.Email, ConfigNames.EmailImapUseSsl);

        using (var client = new ImapClient())
        {
            // Accept all SSL certificates 
            client.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
            {
                return true;
            };

            // Set timeouts (if you like)
            client.Timeout = 60_000; // e.g. 60 seconds

            await client.ConnectAsync(host, port, useSsl, cancellationToken);
            await client.AuthenticateAsync(username, password, cancellationToken);

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var sinceDate = DateTime.UtcNow.AddDays(-10);
            var query = SearchQuery.DeliveredAfter(sinceDate);
            var uids = await inbox.SearchAsync(query, cancellationToken);

            foreach (var uid in uids)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var message = await inbox.GetMessageAsync(uid, cancellationToken);

                if (IsBounce(message))
                {
                    var failedAddress = ExtractFailedRecipient(message);
                    if (!string.IsNullOrEmpty(failedAddress))
                    {
                        bounces.Add(failedAddress);
                    }

                    // Mark for deletion
                    await inbox.AddFlagsAsync(uid, MessageFlags.Deleted, true, cancellationToken);
                }
            }

            // Actually delete (expunge) messages
            await inbox.ExpungeAsync(cancellationToken);

            await client.DisconnectAsync(true, cancellationToken);
        }

        return bounces;
    }

    private bool IsBounce(MimeMessage message)
    {
        var subject = message.Subject ?? string.Empty;
        var body = message.TextBody ?? string.Empty;

        return _bounceKeywords.Any(k =>
            subject.Contains(k, StringComparison.OrdinalIgnoreCase) ||
            body.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private string ExtractFailedRecipient(MimeMessage message)
    {
        if (message.Headers.Contains("Final-Recipient"))
            return message.Headers["Final-Recipient"];

        var text = message.TextBody ?? "";
        var match = System.Text.RegularExpressions.Regex.Match(
            text,
            @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-z]{2,}");

        return match.Success ? match.Value : string.Empty;
    }
}
