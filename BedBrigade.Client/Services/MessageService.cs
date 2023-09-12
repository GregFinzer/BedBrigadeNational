using Microsoft.AspNetCore.Components.Authorization;
using BedBrigade.Data.Models;
using FluentEmail.Core.Models;
using BedBrigade.MessageService.Services;

namespace BedBrigade.Client.Services;

public class MessageService : IMessageService
{
    private readonly AuthenticationStateProvider _auth;
    private readonly IEmailService _emailService;


    public MessageService(AuthenticationStateProvider authState, IEmailService messageService)
    {
        _auth = authState;
        _emailService = messageService;
    }

    /// <summary>
    /// Send Email Message 
    /// </summary>
    /// <param name="to"></param>
    /// <param name="from"></param>
    /// <param name="subject"></param>
    /// <param name="template"></param>
    /// <param name="model"></param>
    /// <returns>ServiceResponse<SendResponse></SendResponse></returns>
    public async Task<ServiceResponse<SendResponse>> SendEmailAsync(string to, string from, string subject, string body)
    {
        return await _emailService.SendEmailAsync(to, from, subject, body);
    }
}
