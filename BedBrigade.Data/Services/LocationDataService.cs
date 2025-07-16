using AKSoftware.Localization.MultiLanguages;
using Microsoft.EntityFrameworkCore;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using KellermanSoftware.AddressParser;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Data.Seeding;
using Serilog;

namespace BedBrigade.Data.Services;

public class LocationDataService : Repository<Location>, ILocationDataService
{
    private readonly IAuthService _authService;
    private readonly IConfigurationDataService _configurationDataService;
    private readonly IContentDataService _contentDataService;
    private readonly ILanguageContainerService _lc;
    private readonly IDonationCampaignDataService _donationCampaignDataService;

    public LocationDataService(IDbContextFactory<DataContext> contextFactory,
        ICachingService cachingService,
        IAuthService authService,
        IConfigurationDataService configurationDataService,
        IContentDataService contentDataService,
        ILanguageContainerService languageContainerService,
        IDonationCampaignDataService donationCampaignDataService) : base(contextFactory, cachingService, authService)
    {
        _configurationDataService = configurationDataService;
        _contentDataService = contentDataService;
        _authService = authService;
        _lc = languageContainerService;
        _donationCampaignDataService = donationCampaignDataService;
    }

    public override async Task<ServiceResponse<Location>> CreateAsync(Location entity)
    {
        try
        {
            var groveCityLocation = await GetByIdAsync(Defaults.GroveCityLocationId);

            if (!groveCityLocation.Success || groveCityLocation.Data == null)
            {
                return new ServiceResponse<Location>("Grove City location not found", false);
            }

            var existingLocation = await GetLocationByRouteAsync(entity.Route);

            if (existingLocation.Success)
            {
                return new ServiceResponse<Location>($"Location with route {entity.Route} already exists");
            }

            var result = await base.CreateAsync(entity);

            if (!result.Success || result.Data == null)
                return result;

            var location = result.Data;
            await CreateGeneralDonationCampaign(location);
            FileUtil.CreateLocationMediaDirectory(location);
            await CreateContent(location, ContentType.Header, "LocationHeader.html", "Header");
            await CreateContent(location, ContentType.Footer, "LocationFooter.html", "Footer");
            await CreateContent(location, ContentType.Body, "LocationHome.html", "Home"); 
            FileUtil.CopyMediaFromLocation(groveCityLocation.Data, location, "Home");
            await CreateContent(location, ContentType.Body, "LocationDonations.html", "Donations");
            FileUtil.CopyMediaFromLocation(groveCityLocation.Data, location, "Donations");
            await CreateContent(location, ContentType.Body, "LocationAboutUs.html", "AboutUs");
            FileUtil.CopyMediaFromLocation(groveCityLocation.Data, location, "AboutUs");
            await CreateContent(location, ContentType.Body, "LocationAssemblyInstructions.html",  "Assembly-Instructions");
            FileUtil.CopyMediaFromLocation(groveCityLocation.Data, location, "Assembly-Instructions");
            await CreateContent(location, ContentType.DeliveryCheckList, "DeliveryCheckList.txt", "DeliveryCheckList");
            await CreateContent(location, ContentType.EmailTaxForm, "EmailTaxForm.txt", "EmailTaxForm");
            await CreateContent(location, ContentType.BedRequestConfirmationForm, "BedRequestConfirmationForm.txt", "BedRequestConfirmationForm");
            await CreateContent(location, ContentType.SignUpEmailConfirmationForm, "SignUpEmailConfirmationForm.txt", "SignUpEmailConfirmationForm");
            await CreateContent(location, ContentType.SignUpSmsConfirmationForm, "SignUpSmsConfirmationForm.txt", "SignUpSmsConfirmationForm");
            await CreateContent(location, ContentType.NewsletterForm, "NewsletterForm.txt", "NewsletterForm");
            await CreateConfig(location);
            return result;
        }
        catch (Exception ex)
        {
            return new ServiceResponse<Location>("Error creating location: " + ex.Message);
        }
    }

    private async Task CreateConfig(Location location)
    {
        List<Configuration> configurations = SeedConfigLogic.LocationSpecificConfigurations(location.LocationId);

        foreach (var config in configurations)
        {
            config.LocationId = location.LocationId;
            var result = await _configurationDataService.CreateAsync(config);
            if (!result.Success)
            {
                Log.Error("Error creating configuration for location {LocationName}: {Message}", location.Name, result.Message);
            }
        }
    }

    private async Task CreateGeneralDonationCampaign(Location location)
    {
        DonationCampaign donationCampaign = new()
        {
            CampaignName = Defaults.DefaultDonationCampaignName,
            LocationId = location.LocationId,
            StartDate = DateTime.UtcNow
        };

        await _donationCampaignDataService.CreateAsync(donationCampaign);
    }

    public async Task CreateContent(Location location, ContentType contentType, string templateName, string name)
    {
        var content = new Content();
        content.LocationId = location.LocationId;
        content.ContentType = contentType;
        content.Name = name;
        content.ContentHtml = WebHelper.GetSeedingFile(templateName);
        content.ContentHtml = content.ContentHtml.Replace("%%LocationRoute%%", location.Route.TrimStart('/'));
        content.ContentHtml = content.ContentHtml.Replace("%%LocationName%%", location.Name);
        content.ContentHtml = content.ContentHtml.Replace("Bed Brigade Bed Brigade", "Bed Brigade");
        content.Title = StringUtil.InsertSpaces(content.Name);
        await _contentDataService.CreateAsync(content);
    }


    public override async Task<ServiceResponse<Location>> GetByIdAsync(object? id)
    {
        //There will always be less than 100 locations, so we can cache all of them
        var allLocations = await GetAllAsync();

        var location = allLocations.Data.FirstOrDefault(l => l.LocationId == (int)id);

        if (location != null)
        {
            return new ServiceResponse<Location>($"{location.Name} found", true, location);
        }

        return new ServiceResponse<Location>("Not Found");
    }

    public async Task<ServiceResponse<Location>> GetLocationByRouteAsync(string routeName)
    {
        //There will always be less than 100 locations, so we can cache all of them
        var allLocations = await GetAllAsync();

        var location = allLocations.Data.FirstOrDefault(l => l.Route.ToLower() == routeName.ToLower()
        || l.Route.ToLower() == $"/{routeName}".ToLower());

        if (location != null && !location.IsActive && !_authService.IsNationalAdmin)
        {
            return new ServiceResponse<Location>("Not Found");
        }

        if (location != null)
        {
            return new ServiceResponse<Location>($"{routeName} found", true, location);
        }

        return new ServiceResponse<Location>("Not Found");
    }

    public List<Location> GetAvailableLocations(List<Location> locations)
    {
        bool isAuthenticated = _authService.IsLoggedIn;

       // Debug.WriteLine("User authenticated -  " + isAuthenticated.ToString());

        // Step 1: Filter locations by postal code
        var filteredLocations = locations.Where(l => !string.IsNullOrEmpty(l.BuildPostalCode)).ToList();

        // Step 2: If user is not authenticated, select only active locations
        if (!isAuthenticated)
        {
            filteredLocations = filteredLocations.Where(l => l.IsActive).ToList();
        }
       
        return filteredLocations;
    } // get available location

    public async Task<ServiceResponse<List<LocationDistance>>> GetBedBrigadeNearMe(string postalCode)
    {
        AddressParser parser = LibraryFactory.CreateAddressParser();

        if (!parser.IsValidZipCode(postalCode))
        {
            return new ServiceResponse<List<LocationDistance>>(_lc.Keys["InvalidPostalCode"] + " " + postalCode);
        }

        var zipCodeInfo = parser.GetInfoForZipCode(postalCode);
        
        if (zipCodeInfo.Latitude == null || zipCodeInfo.Longitude == null)
        {
            return new ServiceResponse<List<LocationDistance>>(_lc.Keys["PostalCodeIsMilitary"] + " " + postalCode);
        }

        int maxMiles;
        var milesResult = await _configurationDataService.GetByIdAsync(ConfigNames.BedBrigadeNearMeMaxMiles);
        if (milesResult.Success && milesResult.Data != null && milesResult.Data.ConfigurationValue != null)
        {
            maxMiles = int.Parse(milesResult.Data.ConfigurationValue);
        }
        else
        {
            return new ServiceResponse<List<LocationDistance>>(_lc.Keys["BedBrigadeNearMeMaxMiles"]);
        }

        var locationsResult = await GetAllAsync();
        bool anyPostalCodes = locationsResult.Data.Any(l => !string.IsNullOrEmpty(l.BuildPostalCode));

        if (!anyPostalCodes)
        {
            return new ServiceResponse<List<LocationDistance>>(_lc.Keys["NoLocationsHavePostalCodes"]);
        }


        var availableLocations = GetAvailableLocations(locationsResult.Data.ToList());

        var result = BuildLocationDistanceResult(postalCode, availableLocations, parser, maxMiles);

        if (result.Count == 0)
        {
            string message = _lc.Keys["NoLocationsFoundWithin",new {maxMiles = maxMiles.ToString(), postalCode = postalCode }];
            return new ServiceResponse<List<LocationDistance>>(message, true, result);
        }

        string foundMessage = _lc.Keys["FoundLocationsWithin", new { count = result.Count.ToString(), maxMiles = maxMiles.ToString(), postalCode = postalCode }];
        return new ServiceResponse<List<LocationDistance>>(foundMessage, true, result);
    }

    private List<LocationDistance> BuildLocationDistanceResult(string postalCode, List<Location> availableLocations, AddressParser parser, int maxMiles)
    {
        var result = new List<LocationDistance>();
        foreach (var loc in availableLocations) // locationsResult.Data
        {
            var distance = parser.GetDistanceInMilesBetweenTwoZipCodes(postalCode, loc.BuildPostalCode);
            if (distance < maxMiles)
            {
                LocationDistance locationDistance = new LocationDistance();
                locationDistance.LocationId = loc.LocationId;
                locationDistance.Name = loc.IsActive ? loc.Name : $"*{loc.Name}";
                locationDistance.Route = loc.Route;
                locationDistance.Distance = distance;
                FillMilesAway(locationDistance);
                result.Add(locationDistance);
            }                           
        }
        result = result.OrderBy(r => r.Distance).ToList();
        return result;
    }

    private void FillMilesAway(LocationDistance locationDistance)
    {
        if (locationDistance.Distance == 0)
        {
            locationDistance.MilesAwayString =  _lc.Keys["IsInYourZipCode"];
        }

        string translation = _lc.Keys["MilesAway"];

        if (translation != null && translation.ToLower() == "km")
        {
            locationDistance.MilesAwayString = Math.Round(locationDistance.Distance * 1.60934, 1).ToString("0.0") + " km";
        }
        else
        {
            locationDistance.MilesAwayString = Math.Round(locationDistance.Distance, 1).ToString("0.0") + " " + _lc.Keys["MilesAway"];
        }
    }

    public async Task<ServiceResponse<List<Location>>> GetLocationsByMetroAreaId(int metroAreaId)
    {
        //There will always be less than 100 locations, so we can cache all of them
        var allLocations = await GetAllAsync();

        if (!allLocations.Success || allLocations.Data == null)
        {
            return new ServiceResponse<List<Location>>(
                "Could not get locations by metro area id: " + allLocations.Message);
        }

        var metroLocations = allLocations.Data.Where(l => l.MetroAreaId == metroAreaId).ToList();
        return new ServiceResponse<List<Location>>(
            $"Found {metroLocations.Count} locations for Metro Area ID {metroAreaId}", true, metroLocations);
    }


}





