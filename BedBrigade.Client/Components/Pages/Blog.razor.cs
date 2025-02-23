using BedBrigade.Client.Services;
using BedBrigade.Data.Services;
using BedBrigade.Common.Logic;
using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Enums;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using KellermanSoftware.NetEmailValidation;
using System.Data;
using BedBrigade.Data;
using System.Data.Entity.Infrastructure;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.IO.Compression;
using System.Data.SqlClient;
using BedBrigade.Common.Models;
using BedBrigade.Client.Components.Pages.Administration.Manage;
using System.Collections.Generic;
using Microsoft.JSInterop.Infrastructure;
using Syncfusion.Blazor.Kanban.Internal;
using BedBrigade.Common.Constants;

namespace BedBrigade.Client.Components.Pages
{
    public partial class Blog: ComponentBase
    {
       
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }

        [Inject] private IContentDataService? _svcContent { get; set; }
      
        [Inject] private NavigationManager? _navigationManager { get; set; }
        [Inject] private ILocationState? _locationState { get; set; }
        [Inject] private ITranslationDataService? _translateLogic { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private IWebHostEnvironment _environment { get; set; }

        [Inject] private IConfiguration _configuration { get; set; }


        [Parameter]
        public string? LocationRoute { get; set; }

        [Parameter]
        public string? ContentType { get; set; } 

        // constant data
        public string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public string? RotatorTitle { get; set; }

        private string Key => $"{LocationId}-{ContentType}";

        public int LocationId { get; set; }

        public string? ImagePath { get; set; }

        public string? LocationName { get; set; }
        protected List<Location>? lstLocations { get; set; }

        protected Location myLocation { get; set; }
        protected List<Content>? lstContents { get; set; }

        private string ChildKey = Guid.NewGuid().ToString();
        private List<BlogData> _cards = new();


        private bool IsShowBlogs = true;
        private bool IsPaging = true;       
        private int NumberOfColumns = 4;
        private int NumberOfRows = 4;
        private int MaxTextSize = 150;

        public bool IsShowBanner = false;
        public bool IsBlogData = false;
        private bool IsTestMode = false; 
        private bool IsCardSettings = false;

        private string? BlogModuleOptions { get; set; }
        private string? BlogModuleImagesExt { get; set; }

        // Temporary variables - to test display only
        private MarkupString? OptionsDisplay;
        private int LocationCount = 0;
        private int ContentCount = 0;
        private string LocationStatus = "Not Found";
        private string ContentStatus = "Not Found";
        public MarkupString NoDataMessage;
        public MarkupString ErrorMessage;


        private static readonly Dictionary<string, string> ValidContentTypes = new()
        {
            { "news", "News" },
            { "new", "News" },      // Corrects "new" → "News"
            { "stories", "Stories" },
            { "story", "Stories" }  // Corrects "story" → "Stories"
        };

        private Dictionary<string, string> dctBlogModuleConfig = new Dictionary<string, string>();


        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrWhiteSpace(ContentType))
            {
                string lowerInput = ContentType.ToLower();
                ContentType = ValidContentTypes.TryGetValue(lowerInput, out string correctedValue)
                    ? correctedValue
                    : ToCapitalized(ContentType);
            }

            // run test data creation

            await LoadConfiguration();

            await CheckParameters();                           
        

        }// Init

        private async Task LoadConfiguration()
        {
            BlogModuleOptions = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "BlogModuleOptions");
            if (BlogModuleOptions != null & BlogModuleOptions.Length > 0)
            {
                // IsTestMode = BlogHelper.GetSettingValueAsBool(BlogModuleOptions, "TestMode");
                // IsCardSettings = BlogHelper.GetSettingValueAsBool(BlogModuleOptions, "CardSettings");
                dctBlogModuleConfig = ParseConfigString(BlogModuleOptions);
                OptionsDisplay = (MarkupString)BlogModuleOptions.Replace("|", "<br/>");
                // Get Configuration Dictionary 

            }

            BlogModuleImagesExt = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "BlogModuleImages");
            if (BlogModuleImagesExt != null & BlogModuleImagesExt.Length > 0)
            {
                AllowedExtensions = BlogModuleImagesExt.Split(',');
            }         

        }


        protected override async void OnParametersSet()
        {
            if (!string.IsNullOrWhiteSpace(ContentType))
            {
                string lowerInput = ContentType.ToLower();
                ContentType = ValidContentTypes.TryGetValue(lowerInput, out string correctedValue)
                    ? correctedValue
                    : ToCapitalized(ContentType);
            }

            await CheckParameters();
                       
            ChildKey = Guid.NewGuid().ToString();                                                               
                       
            StateHasChanged();

        } // Parameters SET

        


        private async Task CheckParameters()
        {
            var bLocationStatus = false;
            var bBlogTypeStatus = false;

            await LoadSourceData();

            if (LocationRoute != null && LocationRoute.Length > 0)
            {
                if (LocationId > 0)
                {
                    bLocationStatus = true;
                }
            }

            if (ContentType != null && ContentType.Length > 0 && IsValidContentType(ContentType))
            {
                bBlogTypeStatus = true;
                ImagePath = $"pages/{ContentType}";
                // check path & images existing
                var LocationBlogFolder = $"{LocationRoute}/pages/{ContentType}";
                var BlogFolderPath = FileUtil.GetMediaDirectory(LocationBlogFolder);
                // if(!BlogHelper.ValidateImageDirectory(BlogFolderPath, ContentType))               
                // {                  
                //    IsShowBanner = false;
                //    ErrorMessage = BootstrapHelper.GetBootstrapMessage("warning", $"Requested Blog Type Folder '{LocationBlogFolder}' folder not found.<br />Please contact system administrator.");
                // }                
            }
            else
            {
                ErrorMessage = BootstrapHelper.GetBootstrapMessage("warning", $"Unknown Requested Blog Type.<br />Please contact system administrator.");
                ContentType = "";
                return;
            }

            if(bLocationStatus && bBlogTypeStatus)
            {
                RotatorTitle = $"{LocationName} {ContentType}";
                IsShowBanner = dctBlogModuleConfig.ContainsKey("ShowBanner") ? dctBlogModuleConfig["ShowBanner"] == "true" : false;

            }
        
        } // Check Parameters


        private async Task LoadSourceData()
        {
            await LoadLocation(); //all Locations or Location by Route
            await LoadContent(); // all Contents

            NoDataMessage = (MarkupString)$"There are no&nbsp;<b>{ContentType}</b> available for <b>{LocationName}</b> at this time.<br/>Please check back later for updates!";

            // ONE time test data creation
            if (IsTestMode)
            {
                // BlogHelper.ProcessBlogImages(lstContents, lstLocations, _environment.WebRootPath);
                                  String? connectionString = _configuration.GetConnectionString("DefaultConnection");
                    //Try to add test data
                    //int intInsertedData = BlogHelper.CreateTestData(connectionString, _environment.WebRootPath);
                
            }

            if (lstContents != null && lstContents.Count > 0)
            {
                // Filter to Current Location & Type
                lstContents = lstContents.Where(c => c.LocationId == LocationId && c.ContentType.ToString() == ContentType).ToList();
                if (lstContents != null && lstContents.Count > 0)
                {
                    // _cards = BlogHelper.GetBlogItemsDataList(lstContents, LocationRoute, LocationName, ContentType, AllowedExtensions);
                    ContentStatus = "Found";
                    IsBlogData = true; // otherwise cannot show Blog Cards
                }
            }         
               
            
        }




        private async Task LoadLocation()
        {
                      // Get Location by Route
            
                var currentLocationResult = await _svcLocation.GetLocationByRouteAsync(LocationRoute);
                if (currentLocationResult != null && currentLocationResult.Success)
                { // single Location Only
                    myLocation = currentLocationResult.Data;
                    LocationId = myLocation.LocationId;
                    LocationName = myLocation.Name;
                    _locationState.Location = LocationRoute;
                    LocationStatus = "Found";
            }
                else {
                    LocationId = 0; // unknown Location
                    IsShowBanner = false; // no banner
                    ErrorMessage = BootstrapHelper.GetBootstrapMessage("warning", $"Cannot load location data. Please contact system administrator.");
                }
            
            
        } // Load Location

        private async Task LoadContent()
        {
            var contentResult = await _svcContent.GetAllAsync();
            if (contentResult != null && contentResult.Success)
            {
                lstContents = contentResult.Data.ToList();
                if (lstContents != null && lstContents.Count > 0)
                {
                    ContentCount = lstContents.Count;
                }
            }

            //if (IsTestMode)
            //{  // get all locations
            lstLocations = new List<Location>();

            var result = await _svcLocation.GetAllAsync();
            if (result.Success)
            {
                lstLocations = result.Data.ToList();
                if(lstLocations != null && lstLocations.Count > 0)
                {                   
                    LocationCount = lstLocations.Count;
                }
            }

            //}


        } // Load Content


        private bool IsValidContentType(string? contentTypeName)
        {
            if (!string.IsNullOrWhiteSpace(ContentType))
            {
                ContentType = StringUtil.ProperCase(ContentType);
            }

            return Enum.TryParse(contentTypeName, out ContentType _);
        }

        private string ToCapitalized(string input)
        {
            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        static Dictionary<string, string> ParseConfigString(string config)
        {
            return config.Split('|')
                         .Select(part => part.Split(':'))
                         .ToDictionary(pair => pair[0], pair => pair[1]);
        }

    } // class Blog
} // namespace
