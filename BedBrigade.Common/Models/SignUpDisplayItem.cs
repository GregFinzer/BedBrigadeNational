using BedBrigade.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedBrigade.Common.Logic;

namespace BedBrigade.Common.Models
{
    public class SignUpDisplayItem : BaseEntity
    {
        private string _signUpGridId = Guid.NewGuid().ToString();

        public string SignUpGridId
        {
            get
            {
                return _signUpGridId;
            }
        }

        public int ScheduleId { get; set; } = 0;
        public int SignUpId { get; set; } = 0;
        public int ScheduleLocationId { get; set; } = 0;
        public string ScheduleLocationName { get; set; } = string.Empty;
        public String? ScheduleEventName { get; set; } = string.Empty;
        public DateTime? ScheduleEventDate { get; set; }
        public EventType? ScheduleEventType { get; set; } 
        public int SignUpNumberOfVolunteers { get; set; } = 0;
        public string? VolunteerFirstName { get; set; }
        public string? VolunteerLastName { get; set; }

        public string? VolunteerFullName
        {
            get
            {
                return VolunteerFirstName + " " + VolunteerLastName;
            }
        }

        public int VolunteerId = 0;
        public string? VolunteerEmail { get; set; }
        public string? VolunteerPhone { get; set; }

        public string? VolunteerFormattedPhone
        {
            get
            {
                return VolunteerPhone.FormatPhoneNumber();
            }
        }

        public string? VolunteerOrganization { get; set; }
        public VehicleType? VehicleType { get; set; }

        public string VehicleTypeString
        {
            get
            {
                return StringUtil.InsertSpaces((VehicleType ?? Enums.VehicleType.None).ToString());
            }
        }

        public string? SignUpNote { get; set; }

        public bool? IHaveVolunteeredBefore { get; set; }

    }
}
