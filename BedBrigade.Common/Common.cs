namespace BedBrigade.Common
{
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
    }
}
