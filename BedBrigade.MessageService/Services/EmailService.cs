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
using System.IO;
using System.Text;

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

    public async Task<ServiceResponse<SendResponse>> SendEmailAsync(string toEmail, string fromEmail, string subject, string body)
    {
        if (_emailConfig.UseFileMock)
        {
            return await SendEmailFileMockAsync(toEmail, fromEmail, subject, body);
        }

        return await SendLiveEmail(toEmail, fromEmail, subject, body);
    }

    private async Task<ServiceResponse<SendResponse>> SendLiveEmail(string toEmail, string fromEmail, string subject, string body)
    {
        try
        {
            SendResponse email = new SendResponse();

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
                .To(toEmail)
                .Subject(subject)
                .Body(body, false)
                .SendAsync();
            
            if (email.Successful)
            {
                return new ServiceResponse<SendResponse>($"Live Email sent from {fromEmail}, to {toEmail}.", true,
                    email);
            }

            return new ServiceResponse<SendResponse>("Live Email failed to send.", false, email);
        }
        catch (Exception ex)
        {
            Debug.Print(ex.Message);
            return new ServiceResponse<SendResponse>($"Live Email Caught Exception {ex.Message}", false);
        }
    }

    private async Task<ServiceResponse<SendResponse>> SendEmailFileMockAsync(string toEmail, string fromEmail,
        string subject, string body)
    {
        try
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Sent:    {DateTime.Now}");
            sb.AppendLine($"To:      {toEmail}");
            sb.AppendLine($"From:    {fromEmail}");
            sb.AppendLine($"Subject: {subject}");
            sb.AppendLine("Body:");
            sb.AppendLine(body);
            sb.AppendLine("=".PadRight(80, '='));
            sb.AppendLine();
            string filePath = $"..\\Logs\\{_emailConfig.FileMockName}";
            await File.AppendAllTextAsync(filePath, sb.ToString());

            return new ServiceResponse<SendResponse>($"Mock Email sent from {fromEmail}, to {toEmail}.", true);
        }
        catch (Exception ex)
        {
            return new ServiceResponse<SendResponse>($"Mock Email Caught Exception {ex.Message}", false);
        }
    }


}




