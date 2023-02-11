using System.Collections.Generic;
using System.Linq;

namespace BedBrigade.MessageService
{
    public class Message
    {
        public List<string> To { get; set; }
        public string From { get; set; }
        public string Subject  { get; set; }
        public string Content { get; set; }
        //public IFormFileCollection Attachments { get; set; }

        public Message()
        {

        }

        public Message(IEnumerable<string> to, string subject, string content)
        {
            To = new List<string>();
            To.AddRange(to.Select(x => x ));
            Subject = subject;
            Content = content;
            //Attachments = attachments;
        }
    }
}
