using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using System.Text;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;


namespace BedBrigade.Data.Services
{
    public class EmailBuilderService : IEmailBuilderService
    {
        private readonly ILocationDataService _locationDataService;
        private readonly IContentDataService _contentDataService;
        private readonly IEmailQueueDataService _emailQueueDataService;
        private readonly IBedRequestDataService _bedRequestDataService;
        private readonly IUserDataService _userDataService;
        private readonly IDonationDataService _donationDataService;
        private readonly IVolunteerDataService _volunteerDataService;
        private readonly IScheduleDataService _scheduleDataService;
        private readonly IMailMergeLogic _mailMergeLogic;

        public EmailBuilderService(ILocationDataService locationDataService,
            IContentDataService contentDataService,
            IEmailQueueDataService emailQueueDataService,
            IBedRequestDataService bedRequestDataService,
            IUserDataService userDataService,
            IDonationDataService donationDataService,
            IVolunteerDataService volunteerDataService,
            IScheduleDataService scheduleDataService,
            IMailMergeLogic mailMergeLogic)
        {
            _locationDataService = locationDataService;
            _contentDataService = contentDataService;
            _emailQueueDataService = emailQueueDataService;
            _bedRequestDataService = bedRequestDataService;
            _userDataService = userDataService;
            _donationDataService = donationDataService;
            _volunteerDataService = volunteerDataService;
            _scheduleDataService = scheduleDataService;
            _mailMergeLogic = mailMergeLogic;
        }

        public async Task<ServiceResponse<bool>> EmailTaxForms(List<Donation> donations)
        {
            var userResult = await _userDataService.GetCurrentLoggedInUser();

            if (!userResult.Success || userResult.Data == null)
            {
                return new ServiceResponse<bool>(userResult.Message, false);
            }


            var groupedByEmail = donations.GroupBy(d => d.Email);

            foreach (var group in groupedByEmail)
            {
                var email = group.Key;
                var donationsForEmail = group.ToList();

                var location = await _locationDataService.GetByIdAsync(donationsForEmail.First().LocationId);
                if (!location.Success || location.Data == null)
                {
                    return new ServiceResponse<bool>("Location not found " + donationsForEmail.First().LocationId, false);
                }

                var templateResult = await _contentDataService.GetByLocationAndContentType(location.Data.LocationId, ContentType.EmailTaxForm);

                if (!templateResult.Success || templateResult.Data == null)
                {
                    return new ServiceResponse<bool>("EmailTaxForm not found", false);
                }

                EmailQueue emailQueue = new()
                {
                    ToAddress = email,
                    Subject = BuildTaxFormSubject(location.Data, donationsForEmail.First().DonationDate),
                    Body = BuildTaxFormBody(templateResult.Data.ContentHtml, location.Data, userResult.Data, donationsForEmail)
                };
                var emailResult = await _emailQueueDataService.QueueEmail(emailQueue);

                if (!emailResult.Success)
                {
                    return new ServiceResponse<bool>(emailResult.Message, false);
                }

                foreach (var donation in donationsForEmail)
                {
                    donation.TaxFormSent = true;
                    var updateResult = await _donationDataService.UpdateAsync(donation);

                    if (!updateResult.Success)
                    {
                        return new ServiceResponse<bool>(updateResult.Message, false);
                    }
                }
            }

            return new ServiceResponse<bool>("Successfully sent out tax forms", true);
        }

        private string BuildTaxFormSubject(Location location, DateTime? donationDate)
        {
            if (donationDate.HasValue)
            {
                return $"{location.Name} Tax Form for {donationDate.Value.Year}";
            }
            return $"{location.Name} Tax Form";
        }

        private string BuildTaxFormBody(string template, Location location, User user, List<Donation> donations)
        {
            StringBuilder sb = new(template, template.Length * 2);
            sb = sb.Replace("%%Donation.FullName%%", donations.First().FullName);
            sb = sb.Replace("%%Donation.TotalAmount%%", donations.Sum(d => d.Amount).ToString("C"));

            if (donations.First().DonationDate.HasValue)
                sb = sb.Replace("%%Donation.Year%%", donations.First().DonationDate.Value.Year.ToString());
            else
                sb = sb.Replace("%%Donation.Year%%", "Unknown");

            sb = _mailMergeLogic.ReplaceUserFields(user, sb);
            sb = _mailMergeLogic.ReplaceLocationFields(location, sb);
            sb = ReplaceItemizedDonations(donations, sb);
            return sb.ToString();
        }

        private StringBuilder ReplaceItemizedDonations(List<Donation> donations, StringBuilder sb)
        {
            StringBuilder itemizedDonations = new();
            foreach (var donation in donations)
            {
                itemizedDonations.AppendLine($"{donation.DonationDate?.ToString("yyyy-MM-dd")} {donation.Amount.ToString("C")}");
            }
            sb = sb.Replace("%%ItemizedDonations%%", itemizedDonations.ToString());
            return sb;
        }

        public async Task<ServiceResponse<bool>> SendSignUpConfirmationEmail(SignUp signUp)
        {
            ServiceResponse<Content> templateResult = await _contentDataService.GetByLocationAndContentType(signUp.LocationId, ContentType.SignUpEmailConfirmationForm);

            if (!templateResult.Success || templateResult.Data == null)
            {
                return new ServiceResponse<bool>("BedRequestConfirmationForm not found", false);
            }

            ServiceResponse<Volunteer> volunteerResult = await _volunteerDataService.GetByIdAsync(signUp.VolunteerId);

            if (!volunteerResult.Success || volunteerResult.Data == null)
            {
                return new ServiceResponse<bool>($"Volunteer not found {signUp.VolunteerId}", false);
            }

            ServiceResponse<Schedule> scheduleResult = await _scheduleDataService.GetByIdAsync(signUp.ScheduleId);

            if (!scheduleResult.Success || scheduleResult.Data == null)
            {
                return new ServiceResponse<bool>($"Schedule not found {signUp.ScheduleId}", false);
            }

            ServiceResponse<string> bodyResult = await BuildSignUpConfirmationBody(signUp, volunteerResult.Data, scheduleResult.Data, templateResult.Data.ContentHtml);

            if (!bodyResult.Success)
            {
                return new ServiceResponse<bool>(bodyResult.Message, false);
            }

            ServiceResponse<bool> volunteerEmailResult = await EmailVolunteer(volunteerResult.Data, bodyResult.Data);

            if (!volunteerEmailResult.Success)
            {
                return new ServiceResponse<bool>(volunteerEmailResult.Message, false);
            }

            ServiceResponse<bool> organizerEmailResult = await EmailOrganizer(volunteerResult.Data, scheduleResult.Data, bodyResult.Data);

            if (!organizerEmailResult.Success)
            {
                return new ServiceResponse<bool>(organizerEmailResult.Message, false);
            }

            return new ServiceResponse<bool>("Successfully queued Sign Up Confirmation Email", true);
        }

        private async Task<ServiceResponse<bool>> EmailVolunteer(Volunteer volunteer, string body)
        {
            EmailQueue emailQueue = new()
            {
                ToAddress = volunteer.Email,
                Subject = BuildSignUpConfirmationSubject(volunteer),
                Body = body,
                Priority = Defaults.BulkHighPriority
            };
            ServiceResponse<string> emailResult = await _emailQueueDataService.QueueEmail(emailQueue);

            if (!emailResult.Success)
            {
                return new ServiceResponse<bool>(emailResult.Message, false);
            }

            return new ServiceResponse<bool>("Success", true);
        }

        private async Task<ServiceResponse<bool>> EmailOrganizer(Volunteer volunteer, Schedule schedule, string body)
        {
            if (!String.IsNullOrEmpty(schedule.OrganizerEmail))
            {
                EmailQueue emailQueue = new()
                {
                    ToAddress = schedule.OrganizerEmail,
                    Subject = BuildSignUpConfirmationSubject(volunteer),
                    Body = body,
                    Priority = Defaults.BulkHighPriority
                };
                ServiceResponse<string> emailResult = await _emailQueueDataService.QueueEmail(emailQueue);

                if (!emailResult.Success)
                {
                    return new ServiceResponse<bool>(emailResult.Message, false);
                }
            }

            return new ServiceResponse<bool>("Success", true);
        }

        private string BuildSignUpConfirmationSubject(Volunteer entity)
        {
            return $"Volunteer Sign Up {entity.FirstName} {entity.LastName}";
        }

        private async Task<ServiceResponse<string>> BuildSignUpConfirmationBody(SignUp signUp, Volunteer volunteer, Schedule schedule, string template)
        {
            var locationResult = await _locationDataService.GetByIdAsync(signUp.LocationId);

            if (!locationResult.Success || locationResult.Data == null)
            {
                return new ServiceResponse<string>($"Location not found {signUp.LocationId}", false);
            }

            StringBuilder sb = new StringBuilder(template, template.Length * 2);
            sb = _mailMergeLogic.ReplaceVolunteerFields(volunteer, schedule, sb);
            sb = _mailMergeLogic.ReplaceLocationFields(locationResult.Data, sb);    
            sb = _mailMergeLogic.ReplaceScheduleFields(schedule, sb);
            return new ServiceResponse<string>("Built Body", true, sb.ToString());
        }

        public async Task<ServiceResponse<bool>> SendBedRequestConfirmationEmail(BedRequest entity)
        {
            var templateResult = await _contentDataService.GetByLocationAndContentType(entity.LocationId, ContentType.BedRequestConfirmationForm);

            if (!templateResult.Success || templateResult.Data == null)
            {
                return new ServiceResponse<bool>("BedRequestConfirmationForm not found", false);
            }

            var bodyResult = await BuildBedRequestConfirmationBody(entity, templateResult.Data.ContentHtml);

            if (!bodyResult.Success)
            {
                return new ServiceResponse<bool>(bodyResult.Message, false);
            }

            EmailQueue emailQueue = new()
            {
                ToAddress = entity.Email,
                Subject = BuildBedRequestConfirmationSubject(entity),
                Body = bodyResult.Data,
                Priority = Defaults.BulkHighPriority
            };
            var emailResult = await _emailQueueDataService.QueueEmail(emailQueue);

            if (!emailResult.Success)
            {
                return new ServiceResponse<bool>(emailResult.Message, false);
            }

            return new ServiceResponse<bool>("Successfully queued Bed Request Confirmation Email", true);
        }

        private string BuildBedRequestConfirmationSubject(BedRequest entity)
        {
            return $"Bed Request Confirmation for {entity.FirstName} {entity.LastName}";
        }

        private async Task<ServiceResponse<string>> BuildBedRequestConfirmationBody(BedRequest entity, string template)
        {
            var locationResult = await _locationDataService.GetByIdAsync(entity.LocationId);

            if (!locationResult.Success || locationResult.Data == null)
            {
                return new ServiceResponse<string>("Location not found", false);
            }

            var countResult = await _bedRequestDataService.SumBedsForNotReceived(entity.LocationId);

            if (!countResult.Success || countResult.Data == null)
            {
                return new ServiceResponse<string>(countResult.Message, false);
            }

            StringBuilder sb = new StringBuilder(template, template.Length*2);
            sb = _mailMergeLogic.ReplaceBedRequestFields(entity, sb);
            sb = _mailMergeLogic.ReplaceLocationFields(locationResult.Data, sb);
            sb = sb.Replace("%%BedRequest.NumberOfBedsWaitingSum%%", (countResult.Data - 1).ToString());
            return new ServiceResponse<string>("Built Body", true, sb.ToString());
        }








    }


}
