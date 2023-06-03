using Microsoft.AspNetCore.Components;

namespace BedBrigade.Client.Pages.Administration
{
    public partial class Dashboard : ComponentBase
    {
        public int TotalVolunteers { get; private set; }
        public int TotalBedRequests { get; private set; }
        public int TotalScheduledDeliveries { get; private set; }
        public int TotalLocations { get; private set; }
        public int TotalUsers { get; private set; }
        public double TotalDonationsToday { get; private set; }
        public double TotalDonationsThisWeek { get; private set; }
        public double TotalDonationsThisMonth { get; private set; }
        public double TotalDonationsThisYear { get; private set; }

        public int? DonationSource = 10;
        public int? VolunteerSource = 15;

        private class DataModel
        {
            public string Id { get; set; }
            public string text { get; set; }
        };


        protected override async Task OnInitializedAsync()
        {
            TotalVolunteers = 132;
            TotalBedRequests = 23;
            TotalScheduledDeliveries = 10;
            TotalLocations = 12;
            TotalUsers = 6;
            TotalDonationsToday = 1232.00;
            TotalDonationsThisWeek = 2434.00;
            TotalDonationsThisMonth = 4232.00;
            TotalDonationsThisYear = 4232.00;

        }

        //        private async Task<List<GraphData>> GetUserRolesAsync()
        //        {
        //            List<GraphData> RoleSource = new();
        //            var users = await _svcUser.GetRegisteredUsersAsync();
        //            var roles = await _svcUser.GetRolesAsync();
        //            TotalUsers = (await _svcUser.UsersInRolesAsync(new List<string> { "Admin", "CareAdmin", "Clergy", "OfficeStaff", "Reports", "Caregiver" })).Count();
        //            foreach (var role in roles)
        //            {
        //                double count = (await _svcUser.UsersInRoleAsync(role.Name)).Count();
        //                if (count > 0)
        //                {
        //                    RoleSource.Add(new GraphData { xValue = role.Name, yValue = count, text = Math.Round(count / TotalUsers * 100).ToString() + "%" });
        //                }
        //            }
        //            return RoleSource;
        //        }

        //        private async Task<List<GraphData>> GetMyVisitStatsAsync()
        //        {
        //            List<GraphData> VisitSource = new();
        //            var recipients = await _svcRecipient.GetRecipientsAsync(Hide);
        //            var myRecipients = recipients.Where(r => r.PrimaryCaregiver == Identity.UserName);
        //            var statusCodes = await _svcStatus.GetAllAsync();
        //            var caregivers = await _svcCaregiver.GetAllNotesForCaregiver(Identity.UserName);

        //            var TotalCareVisits = caregivers.Count();
        //            double TotalRecipients = myRecipients.Count();
        //            foreach (var status in statusCodes)
        //            {
        //                var count = myRecipients.Where(r => r.StatusId == status.StatusId).Count();
        //                if (count > 0)
        //                {
        //                    VisitSource.Add(new GraphData { xValue = status.Name, yValue = count, text = Math.Round(count / TotalRecipients * 100).ToString() + "%" });
        //                }
        //            }

        //            var LastVisit = DateTime.Now;
        //            var NextVisit = DateTime.Now;
        //            return VisitSource;
        //        }

        //        private async Task<List<GraphData>> GetFacilitiesByTypeAsync()
        //        {
        //            List<GraphData> FacilitySource = new List<GraphData>();
        //            var facilities = await _svcFacility.GetAllAsync();
        //            var facilityTypeValues = Enum.GetNames(typeof(FacilityType));
        //            if (facilities == null) return FacilitySource;

        //            TotalFacilities = facilities.ToList().Count;
        //            TotalFacilityTypes = facilityTypeValues.Length;

        //            foreach (var facilityTypeValue in facilityTypeValues)
        //            {
        //                var type = facilityTypeValue.Replace('_', ' ');
        //                double count = facilities.Where(f => f.FacilityType == (int)Enum.Parse(typeof(FacilityType), facilityTypeValue)).Count();
        //                if (count > 0)
        //                {
        //                    FacilitySource.Add(new GraphData { xValue = type, yValue = count, text = Math.Round(count / TotalFacilities * 100).ToString() + "%" });
        //                }
        //            }
        //            return FacilitySource;
        //        }

        //        private async Task<List<GraphData>> GetRecipientsByStatusAsync()
        //        {
        //            List<GraphData> RecipientSource = new();
        //            var recipients = await _svcRecipient.GetRecipientsAsync(Hide);
        //            var statuses = await _svcStatus.GetAllAsync();
        //            if (recipients == null) return RecipientSource;
        //            TotalRecipients = recipients.Count();
        //            TotalStatuses = statuses.Count();

        //            foreach (var status in statuses)
        //            {
        //                double count = recipients.Where(r => r.StatusId == status.StatusId).Count();
        //                if (count > 0)
        //                {
        //                    RecipientSource.Add(new GraphData { xValue = status.Name, yValue = count, text = Math.Round(count / TotalRecipients * 100).ToString() + "%" });
        //                }
        //            }
        //            return RecipientSource;
        //        }

        //        public class GraphData
        //        {
        //            public string xValue { get; set; }
        //            public double yValue { get; set; }
        //            public string text { get; set; }
        //        }

        //        public class VisitDataModel
        //        {
        //            public string Text { get; set; }
        //            public string Icon { get; set; }
        //            public List<VisitDataModel> Child { get; set; }
        //        }


        //        public class AppointmentData
        //        {
        //            public int Id { get; set; }
        //            public string Subject { get; set; }
        //            public string Location { get; set; }
        //            public DateTime StartTime { get; set; }
        //            public DateTime EndTime { get; set; }
        //            public string Description { get; set; }
        //            public bool IsAllDay { get; set; }
        //            public string RecurrenceRule { get; set; }
        //            public string RecurrenceException { get; set; }
        //            public Nullable<int> RecurrenceID { get; set; }
        //        }
    }


}





