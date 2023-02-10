using System;
using System.Collections.Generic;
using System.Text;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace MessageService
{
    public class SmsSender : ISmsSender
    {
        private readonly SmsConfiguration _smsConfiguration;
        public SmsSender(SmsConfiguration smsConfiguration)
        {
            _smsConfiguration = smsConfiguration;
        }

        public string SendSms(string from, string to, string body)
        {
            // Find your Account Sid and Token at twilio.com/console
            // DANGER! This is insecure. See http://twil.io/secure
            string accountSid = _smsConfiguration.AccountSid;
            string authToken = _smsConfiguration.AuthToken;

            TwilioClient.Init(accountSid, authToken);

            var message = MessageResource.Create(
                body: body,
                from: new Twilio.Types.PhoneNumber(from),
                to: new Twilio.Types.PhoneNumber(to)
            );

            Console.WriteLine(message.Sid);
            return message.Sid;
        }
    }
}
