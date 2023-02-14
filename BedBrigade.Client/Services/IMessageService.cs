using BedBrigade.Data.Models;
using FluentEmail.Core.Models;

namespace BedBrigade.Client.Services
{
    public interface IMessageService
    {
        Task<ServiceResponse<SendResponse>> SendEmailAsync(string to, string from, string subject, string template, object model);
    }
}