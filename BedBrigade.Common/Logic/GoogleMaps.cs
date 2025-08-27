using BedBrigade.Common.Models;

namespace BedBrigade.Common.Logic
{
    public static class GoogleMaps
    {
        public static string CreateGoogleMapsLinkForSchedule(Schedule schedule)
        {
            var address = schedule.Address + ", " + schedule.City + ", " + schedule.State + ", " + schedule.PostalCode;
            string urlEncodedAddress = System.Net.WebUtility.UrlEncode(address);
            string googleMapsLink = $"https://www.google.com/maps/search/?api=1&query={urlEncodedAddress}";
            return googleMapsLink;
        }
    }
}
