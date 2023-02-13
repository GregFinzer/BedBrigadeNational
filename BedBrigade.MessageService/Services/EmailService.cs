using BedBrigade.Data;
using BedBrigade.Data.Models;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using FluentEmail.Razor;
using FluentEmail.Smtp;
using System;
using System.Diagnostics;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BedBrigade.MessageService.Services;

public class EmailService : IEmailService
{
    private readonly DataContext _context;
    private readonly EmailConfiguration _emailConfig;

    public EmailService(DataContext context,EmailConfiguration emailConfig)
    {
        _context = context;
        _emailConfig = emailConfig;
    }

    public async Task<ServiceResponse<SendResponse>> SendEmailAsync(string To, string From, string Subject, string Template, object Model)
    {
        SendResponse email = new SendResponse();
        try
        {
            var sender = new SmtpSender(() => new SmtpClient(_emailConfig.SmtpServer)
            {
                EnableSsl = _emailConfig.EnableSsl,
                DeliveryMethod = _emailConfig.DeliveryMethod,
                Port = _emailConfig.Port
            });

            Email.DefaultSender = sender;
            Email.DefaultRenderer = new RazorRenderer();

            email = await Email               
                .From(_emailConfig.From)
                .To(To)
                .Subject(Subject)
                .UsingTemplate(Template, Model)
                .SendAsync();
            var result = await AuditEmailAsync(To, From, Subject, Template, Model, email);
            if (result.Success)
            {
                return new ServiceResponse<SendResponse>($"Email sent from {From}, to {To}.", true, email);
            }
            return new ServiceResponse<SendResponse>("Email failed to send.", false, email);

        }
        catch (Exception ex)
        {
            Debug.Print(ex.Message);
            return new ServiceResponse<SendResponse>($"Caught Exception {ex.Message}", false, email);
        }


    }

    private async Task<ServiceResponse<SendResponse>> AuditEmailAsync(string to, string from, string subject, string template, object model, SendResponse result)
    {
        EmailQueue emailqueue = new()
        {
            FromDisplayName = from,
            FromAddress = from,
            ToAddress = to,
            Subject = subject,
            HtmlBody = template,
            QueueDate = DateTime.UtcNow,
            SentDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow,
            Priority = 0,
            Status = result.Successful.ToString(),
            FailureMessage = result.ErrorMessages.Count > 0 ? string.Join(", ", result.ErrorMessages) : string.Empty,
            FirstName = from
        };
        try
        {
            await _context.EmailQueues.AddAsync(emailqueue);
            await _context.SaveChangesAsync();
            return new ServiceResponse<SendResponse>("Email Audited", true, result);
        }
        catch (Exception ex)
        {
            return new ServiceResponse<SendResponse>($"Write email audit {ex.Message}", false, result);
        }
    }
}




