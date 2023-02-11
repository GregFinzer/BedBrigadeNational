using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BedBrigade.MessageService
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailConfiguration _emailConfiguration;

        public EmailSender(EmailConfiguration emailConfig)
        {
            _emailConfiguration = emailConfig;
        }

        public bool SendMail(string to, string from, string subject, string body, bool isHtml = true)
        {
            //create the mail message 
            MailMessage mail = new MailMessage();


            //set the addresses 
            mail.From = new MailAddress(from);
            mail.To.Add(new MailAddress(to));

            //set the content 
            mail.Subject = subject;
            mail.IsBodyHtml = isHtml;
            mail.Body = body;
            //send the message 
            System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient(_emailConfiguration.SmtpServer);
            smtp.Port = _emailConfiguration.Port;
            smtp.EnableSsl = true;
            NetworkCredential credentials = new NetworkCredential(_emailConfiguration.UserName, _emailConfiguration.Password);
            smtp.Credentials = credentials;
            try
            {
                smtp.Send(mail);
                return true;
            }
            catch (SmtpException ex)
            {
                Debug.Print(ex.Message);
                return false;
            }

        }
        public async Task<bool> SendMailAsync(string to, string from, string subject, string body, bool isHtml)
        {
            //create the mail message 
            MailMessage mail = new MailMessage();


            //set the addresses 
            mail.From = new MailAddress(from);
            mail.To.Add(new MailAddress(to));

            //set the content 
            mail.Subject = subject;
            mail.IsBodyHtml = isHtml;
            mail.Body = body;
            //send the message 
            System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient(_emailConfiguration.SmtpServer);
            smtp.Port = _emailConfiguration.Port;
            //smtp.EnableSsl = true;
            //NetworkCredential credentials = new NetworkCredential(_emailConfiguration.UserName, _emailConfiguration.Password);
            //smtp.Credentials = credentials;
            try
            {
                smtp.Send(mail);
                return true;
            }
            catch (SmtpException ex)
            {
                Debug.Print(ex.Message);
                return false;
            }
        }


    }
}
