using System.ComponentModel;

namespace BedBrigade.Common.Enums
{
    public enum SmsRecipientOption
    {
        [Description("Myself (used for testing)")]
        Myself,
        [Description("Bed Brigade Leaders Nationwide")]
        BedBrigadeLeadersNationwide,
        [Description("Bed Brigade Leaders for Location")]
        BedBrigadeLeadersForLocation,
        [Description("Bed Requestors for Location")]
        BedRequestorsForLocation,
        [Description("Bed Requestors Who Have NOT Received A Bed for Location")]
        BedRequestorsWhoHaveNotRecievedABed,
        [Description("Bed Requestors Who Have Received A Bed for Location")]
        BedRequestorsWhoHaveRecievedABed,
        [Description("Bed Requestors For An Event")]
        BedRequestorsForAnEvent,
        Everyone,
        [Description("Contact Us Requests for Location")]
        ContactUsForLocation,
        [Description("Volunteers for Location")]
        VolunteersForLocation,
        [Description("Volunteers With Delivery Vehicles for Location")]
        VolunteersWithDeliveryVehicles,
        [Description("Volunteers For An Event")]
        VolunteersForAnEvent,
    }
}
