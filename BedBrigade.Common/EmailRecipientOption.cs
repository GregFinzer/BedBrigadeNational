using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Common
{
    public enum EmailRecipientOption
    {
        Everyone,
        [Description("All Volunteers")]
        AllVolunteers,
        [Description("All Bed Requestors")]
        AllBedRequestors,
        [Description("All Contacts Us Requests")]
        AllContactUs,
        [Description("All Bed Brigade Leaders Nationwide")]
        AllBedBrigadeLeadersNationwide,
        [Description("All Bed Brigade Leaders For My Location")]
        AllBedBrigadeLeadersForMyLocation,
        [Description("Volunteers With Delivery Vehicles")]
        VolunteersWithDeliveryVehicles,
        [Description("Volunteers For An Event")]
        VolunteersForAnEvent,
        [Description("Bed Requestors Who Have NOT Received A Bed")]
        BedRequestorsWhoHaveNotRecievedABed,
        [Description("Bed Requestors Who Have Received A Bed")]
        BedRequestorsWhoHaveRecievedABed,
        [Description("Bed Requestors For An Event")]
        BedRequestorsForAnEvent

    }
}
