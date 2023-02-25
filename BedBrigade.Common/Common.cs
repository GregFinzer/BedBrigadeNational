
using static BedBrigade.Common.Common;

namespace BedBrigade.Common
{

    public class EnumItem
    {
        public BedRequestStatus Value { get; set; }
        public string? Name { get; set; }

    }

    public static class Common
    {
        public enum PersistGrid
        {
            Configuration = 1,
            User = 2,
            Location = 3,
            Volunteer = 4,
            Donation = 5,
            Content = 6,
            BedRequest = 7

        }

        public enum BedRequestStatus
        {
           Requested = 1,
           Scheduled = 2,
           Delivered = 3,
        }

        /// <summary>
        /// Get a list of Enum Items suitable for a dropdown list from the BedRequestStatusEnum
        /// </summary>
        /// <returns>List<EnumItem></EnumItem></returns>
        public static List<EnumItem> GetBedRequestStatusItems() 
        {
            var type = typeof(BedRequestStatus);
            return Enum.GetValues(type).OfType<BedRequestStatus>().ToList()
                            .Select(x => new EnumItem
                            { 
                                Value = (BedRequestStatus)x,
                                Name = Enum.GetName(type, x) 
                            })
                            .ToList();
        }

    }
}
