using System.Net.Mail;

namespace BedBrigade.MessageService
{
    public class EmailConfiguration
    {
        public string From { get; set; } = "national.admin@bedbrigade.org";
        public string SmtpServer { get; set; } = "localhost";
        public int Port { get; set; } = 25;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public SmtpDeliveryMethod DeliveryMethod { get; set; } = SmtpDeliveryMethod.Network;
        public bool UseFileMock { get; set; } = true;
        public string FileMockPath { get; set; } = "EmailFileMock.txt";
    }
}
