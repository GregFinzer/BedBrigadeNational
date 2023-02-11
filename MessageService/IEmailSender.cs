using System.Threading.Tasks;

namespace MessageService
{
    public interface IEmailSender
    {
        bool SendMail(string to, string from, string subject, string body, bool isHtml = true);
        Task<bool> SendMailAsync(string to, string from, string subject, string body, bool isHtml = true);
    }
}