using BedBrigade.Data.Models;
using FluentEmail.Core.Models;
using System.Threading.Tasks;

namespace BedBrigade.MessageService.Services
{
    public interface IEmailService
    {
        Task<ServiceResponse<SendResponse>> SendEmailAsync(string To, string From, string Subject, string Template, object Model);
    }
}