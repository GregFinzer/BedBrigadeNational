using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageService
{
    public class Sms
    {        
        public string From { get; set; }
        public List<string> To { get; set; }
        public string Message { get; set; }

        public Sms()
        {

        }

        public Sms(IEnumerable<string> to, string from, string message)
        {
            To = new List<string>();
            To.AddRange(to.Select(x => x));
            From = from;
            Message = message;
        }
    }
}
