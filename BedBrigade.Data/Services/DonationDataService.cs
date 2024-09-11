using System.Text;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Data.Seeding;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BedBrigade.Data.Services;

public class DonationDataService : Repository<Donation>, IDonationDataService
{
    private readonly ICommonService _commonService;
    private readonly IUserDataService _userDataService;
    private readonly ILocationDataService _locationDataService;
    private readonly IContentDataService _contentDataService;

    public DonationDataService(IDbContextFactory<DataContext> contextFactory, 
        ICachingService cachingService,
        AuthenticationStateProvider authProvider, 
        ICommonService commonService,
        IUserDataService userDataService,
        ILocationDataService locationDataService,
        IContentDataService contentDataService) : base(contextFactory, cachingService, authProvider)
    {
        _commonService = commonService;
        _userDataService = userDataService;
        _locationDataService = locationDataService;
        _contentDataService = contentDataService;
    }

    public async Task<ServiceResponse<List<Donation>>> GetAllForLocationAsync(int locationId)
    {
        return await _commonService.GetAllForLocationAsync(this, locationId);
    }

    public async Task<ServiceResponse<bool>> EmailTaxForms(List<Donation> donations)
    {
        var userResult = await _userDataService.GetCurrentLoggedInUser();

        if (!userResult.Success || userResult.Data == null)
        {
            return new ServiceResponse<bool>(userResult.Message , false);
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
                Subject = BuildSubject(location.Data, donationsForEmail.First().DonationDate),
                Body = BuildBody(templateResult.Data.ContentHtml, location.Data, userResult.Data, donationsForEmail)
            };
            var emailResult = await EmailQueueLogic.QueueEmail(emailQueue);

            if (!emailResult.Success)
            {
                return new ServiceResponse<bool>(emailResult.Message, false);
            }

            foreach (var donation in donationsForEmail)
            {
                donation.TaxFormSent = true;
                var updateResult = await UpdateAsync(donation);

                if (!updateResult.Success)
                {
                    return new ServiceResponse<bool>(updateResult.Message, false);
                }
            }
        }

        return new ServiceResponse<bool>("Successfully sent out tax forms",true);
    }

    private string BuildBody(string template, Location location, User user, List<Donation> donations)
    {
        StringBuilder sb = new(template, template.Length*2);
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

    private static StringBuilder ReplaceItemizedDonations(List<Donation> donations, StringBuilder sb)
    {
        StringBuilder itemizedDonations = new();
        foreach (var donation in donations)
        {
            itemizedDonations.AppendLine($"{donation.DonationDate?.ToString("yyyy-MM-dd")} {donation.Amount.ToString("C")}");
        }
        sb = sb.Replace("%%ItemizedDonations%%", itemizedDonations.ToString());
        return sb;
    }

    private static StringBuilder ReplaceUserFields(User user, StringBuilder sb)
    {
        sb = sb.Replace("%%User.FirstName%%", user.FirstName);
        sb = sb.Replace("%%User.LastName%%", user.LastName);
        sb = sb.Replace("%%User.Role%%", user.Role);
        sb = sb.Replace("%%User.Email%%", user.Email);
        sb = sb.Replace("%%User.Phone%%", user.Phone.FormatPhoneNumber());
        return sb;
    }

    private static StringBuilder ReplaceLocationFields(Location location, StringBuilder sb)
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

    private string BuildSubject(Location location, DateTime? donationDate)
    {
        if (donationDate.HasValue)
        {
            return $"{location.Name} Tax Form for {donationDate.Value.Year}";
        }
        return $"{location.Name} Tax Form";
    }

    

}



