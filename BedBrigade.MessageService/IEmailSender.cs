using System.Threading.Tasks;

namespace BedBrigade.MessageService
{
    public interface IEmailSender
    {
        bool SendMail(string to, string from, string subject, string body, bool isHtml = true);
        Task<bool> SendMailAsync(string to, string from, string subject, string body, bool isHtml = true);
    }
}