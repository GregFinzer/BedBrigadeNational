using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Tests.Integration
{
    public class PolarisBedRequest
    {
        public string Group { get; set; }
        public string Name { get; set; }
        public string RequestersName { get; set; }
        public DateTime DateOfRequest { get; set; }
        public string BedForAdultOrChild { get; set; }
        public string BedType { get; set; }
        public string AdultName { get; set; }
        public string AdultGender { get; set; }
        public string? ChildAge { get; set; }
        public string ChildGender { get; set; }
        public string ChildName { get; set; }
        public string DeliveryAddress { get; set; }
        public string Email { get; set; }
        public string PrimaryLanguage { get; set; }
        public string PhoneNumber { get; set; }
        public string? SpeaksEnglish { get; set; }
        public string Status { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string Reference { get; set; }
    }
}
