namespace BedBrigade.Common.Constants
{
    public static class RoleNames
    {
        public const string NationalAdmin = "National Admin";
        public const string NationalEditor = "National Editor";
        public const string LocationAdmin = "Location Admin";
        public const string LocationEditor = "Location Editor";
        public const string LocationAuthor = "Location Author";
        public const string LocationScheduler = "Location Scheduler";
        public const string LocationContributor = "Location Contributor";
        public const string LocationTreasurer = "Location Treasurer";
        public const string LocationCommunications = "Location Communications";

        public const string CanCreatePages = "National Admin, National Editor, Location Admin, Location Editor, Location Contributor";
        public const string CanCreateBlogPost = "National Admin, National Editor, Location Admin, Location Editor, Location Contributor";
        public const string CanPublishPages = "National Admin, National Editor, Location Admin, Location Editor";
        public const string CanPublishBlogPosts = "National Admin, National Editor, Location Admin, Location Editor";
        public const string CanManageMedia = "National Admin, National Editor, Location Admin, Location Editor";
        public const string CanManageVolunteers = "National Admin, Location Admin, Location Scheduler";
        public const string CanManageBedRequests = "National Admin, Location Admin, Location Scheduler";
        public const string CanManageContacts = "National Admin, Location Admin, Location Scheduler";
        public const string CanManageSchedule = "National Admin, Location Admin, Location Scheduler";
        public const string CanSendBulkEmail = "National Admin, Location Admin, Location Scheduler, Location Communications";
        public const string CanManageDonations = "National Admin, Location Admin, Location Treasurer";
        public const string CanManageUsers = "National Admin, Location Admin";
        public const string CanCreateNationalNewsPost = "National Admin, National Editor";
        public const string CanCreateContentForLocations = "National Admin, National Editor";
        public const string CanManagePages = "National Admin, Location Admin, Location Author, National Author";
        public const string CanViewPages =
            "National Admin, National Editor, Location Admin, Location Editor, Location Contributor";
        public const string CanViewAdminDashboard =
            "National Admin, National Editor, Location Admin, Location Editor, Location Author, Location Scheduler, Location Contributor, Location Treasurer, Location Communications";

        public const string CanSendSms = "National Admin, Location Admin, Location Scheduler, Location Communications";
        public const string CanManageNewsletters = "National Admin, Location Admin, Location Scheduler, Location Communications";

        public const string CanViewBedRequests = CanViewAdminDashboard;
        public const string CanViewContacts = CanViewAdminDashboard;
        public const string CanViewLocations = CanViewAdminDashboard;
        public const string CanViewMetroAreas = CanViewAdminDashboard;
        public const string CanViewSignUps = CanViewAdminDashboard;
        public const string CanViewVolunteers = CanViewAdminDashboard;
        public const string CanViewSchedule = CanViewAdminDashboard;
        public const string CanViewUsers = CanViewAdminDashboard;
        public const string CanViewSmsSummary = CanViewAdminDashboard;

    }
}
