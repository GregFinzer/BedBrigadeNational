using BedBrigade.Data.Models;
using FluentEmail.Core.Models;
using System.Threading.Tasks;

namespace BedBrigade.MessageService.Services
{
    public interface IEmailService
    {
        Task<ServiceResponse<SendResponse>> SendEmailAsync(string toEmail, string fromEmail, string subject, string body);
    }
}