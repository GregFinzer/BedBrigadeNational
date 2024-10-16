using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedBrigade.Common.Enums;
using BedBrigade.Data.Migrations;
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

        public EmailBuilderService(ILocationDataService locationDataService,
            IContentDataService contentDataService,
            IEmailQueueDataService emailQueueDataService,
            IBedRequestDataService bedRequestDataService,
            IUserDataService userDataService,
            IDonationDataService donationDataService)
        {
            _locationDataService = locationDataService;
            _contentDataService = contentDataService;
            _emailQueueDataService = emailQueueDataService;
            _bedRequestDataService = bedRequestDataService;
            _userDataService = userDataService;
            _donationDataService = donationDataService;
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
                sb = sb.Replace("%%Year%%", donations.First().DonationDate.Value.Year.ToString());
            else
                sb = sb.Replace("%%Year%%", "Unknown");

            sb = ReplaceUserFields(user, sb);
            sb = ReplaceLocationFields(location, sb);
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

        public async Task<ServiceResponse<bool>> SendBedRequestConfirmationEmail(BedRequest entity)
        {
            var templateResult = await _contentDataService.GetByLocationAndContentType(entity.LocationId, ContentType.BedRequestConfirmationForm);

            if (!templateResult.Success || templateResult.Data == null)
            {
                return new ServiceResponse<bool>("EmailTaxForm not found", false);
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
                Priority = Defaults.EmailHighPriority
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
            sb = ReplaceBedRequestFields(entity, sb);
            sb = ReplaceLocationFields(locationResult.Data, sb);
            sb = sb.Replace("%%BedRequest.NumberOfBedsWaitingSum%%", (countResult.Data - 1).ToString());
            return new ServiceResponse<string>("Built Body", true, sb.ToString());
        }

        private StringBuilder ReplaceBedRequestFields(BedRequest entity, StringBuilder sb)
        {
            sb = sb.Replace("%%BedRequest.FirstName%%", entity.FirstName);
            sb = sb.Replace("%%BedRequest.NumberOfBeds%%", entity.NumberOfBeds.ToString());
            sb = sb.Replace("%%BedRequest.AgesGender%%", entity.AgesGender);
            return sb;
        }

        private StringBuilder ReplaceUserFields(User user, StringBuilder sb)
        {
            sb = sb.Replace("%%User.FirstName%%", user.FirstName);
            sb = sb.Replace("%%User.LastName%%", user.LastName);
            sb = sb.Replace("%%User.Role%%", user.Role);
            sb = sb.Replace("%%User.Email%%", user.Email);
            sb = sb.Replace("%%User.Phone%%", user.Phone.FormatPhoneNumber());
            return sb;
        }

        private StringBuilder ReplaceLocationFields(Location location, StringBuilder sb)
        {
            sb = sb.Replace("%%Location.Name%%", location.Name);
            sb = sb.Replace("%%Location.Address1%%", location.Address1);
            sb = sb.Replace("%%Location.Address2%%", location.Address2);
            sb = sb.Replace("%%Location.City%%", location.City);
            sb = sb.Replace("%%Location.State%%", location.State);
            sb = sb.Replace("%%Location.PostalCode%%", location.PostalCode);
            sb = sb.Replace("%%Location.Route%%", location.Route);
            return sb;
        }
    }


}
