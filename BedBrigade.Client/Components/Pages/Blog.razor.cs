using BedBrigade.Client.Services;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Data;
using System.Diagnostics;
using static BedBrigade.Common.Logic.BlogHelper;


namespace BedBrigade.Client.Components.Pages
{
    public partial class Blog : ComponentBase
    {
        // BedBrigade Services

        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }
        [Inject] private IContentDataService? _svcContent { get; set; }
        [Inject] private NavigationManager? _navigationManager { get; set; }
        [Inject] private ILocationState? _locationState { get; set; }
        [Inject] private ITranslationDataService? _translateLogic { get; set; }
        [Inject] private ILanguageContainerService? _lc { get; set; }
        [Inject] private IWebHostEnvironment? _environment { get; set; }
        [Inject] private IConfiguration? _configuration { get; set; }
        [Inject] private IJSRuntime? JS { get; set; }

        // Page Parameters (by URL)

        [Parameter]
        public string? LocationRoute { get; set; }

        // Parameters for Banner Rotator

        public string? RotatorTitle { get; set; }

        public int LocationId { get; set; }
        public string? ImagePath { get; set; }

        // passing parameters for Card View

        private string ChildKey = Guid.NewGuid().ToString();
        private List<BlogData> _cards = new();
        protected Location? myLocation { get; set; }
        private bool IsCardPaging = true;
        private int NumberOfColumns = 4;
        private int NumberOfRows = 4;
        private int MaxTextSize = 150;
        private MarkupString ErrorMessage;
        private MarkupString NoDataMessage;

        public string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        public string? LocationName { get; set; }
        protected List<Location>? lstLocations { get; set; }
        protected List<Content>? lstContents { get; set; }


        private BlogConfiguration? blogConfig { get; set; }


        private bool IsShowBlogs = true;
        public bool IsShowBanner = false;
        public bool IsBlogData = false;

        private bool IsCardSettings = false;


        // Test Only Variables
        protected List<Content>? lstBlogData { get; set; }
        private String DataStatusMessage = string.Empty;
        private String DataAddMessage = string.Empty;
        private String FolderMessage = string.Empty;
        private String FileMessage = string.Empty;
        private int ImageFolderCount = 0;
        private int BlogContentCount = 0;
        private int UnzippedImagesCount = 0;

        private string? connectionString { get; set; }
        private string? webRootPath { get; set; }

        private string TestBarClass = "row bg-danger";

        private string ResetAction = "load";

        private bool SpinnerVisible { get; set; } = false;

        private string? BlogModuleOptions { get; set; }
        private string? BlogModuleImagesExt { get; set; }

        private string? BlogType { get; set; }

        protected override async Task OnInitializedAsync()
        {
            _lc.InitLocalizedComponent(this);
            SetBlogType();

            await LoadConfiguration();


            await CheckParameters();

            // Validate and prepare Banner Folders
            if (LocationRoute != null)
            {
                if (LocationRoute.ToLower() != "national")
                {
                    ValidateAndPrepareBannerFolders(LocationRoute, BlogType, _environment.WebRootPath);
                }
            }

            await base.OnInitializedAsync();

        }

        private void SetBlogType()
        {
            string baseUri = _navigationManager.BaseUri;
            baseUri = baseUri.TrimEnd('/');
            BlogType = StringUtil.GetLastWord(baseUri, "/");
        }
        // Init       

        //private void SetValidContentType()
        //{
        //    if (!string.IsNullOrWhiteSpace(ContentType))
        //    {
        //        string lowerInput = ContentType.ToLower();
        //        ContentType = BlogHelper.ValidContentTypes.TryGetValue(lowerInput, out string? correctedValue)
        //        ? correctedValue
        //            : StringUtil.ProperCase(ContentType);
        //    }
        //} // SetValidContentType

        private async Task LoadConfiguration()
        {
            BlogModuleOptions = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "BlogModuleOptions");
            if (BlogModuleOptions != null & BlogModuleOptions.Length > 0)
            {

                blogConfig = new BlogConfiguration(BlogModuleOptions);
                IsCardSettings = blogConfig.CardSettings;
                IsShowBanner = blogConfig.ShowBanner;

                if (IsCardSettings) // Load more Settings, if missing - default values
                {
                    IsCardPaging = blogConfig.CardPaging;
                    NumberOfColumns = blogConfig.CardColumns;
                    NumberOfRows = blogConfig.CardRows;
                    MaxTextSize = blogConfig.CardTextSize;
                }

            }

            BlogModuleImagesExt = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "BlogModuleImages");
            if (BlogModuleImagesExt != null & BlogModuleImagesExt.Length > 0)
            {
                AllowedExtensions = BlogModuleImagesExt.Split(',');
            }

        }//LoadConfiguration

        protected override async Task OnParametersSetAsync()
        {
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

            if (!String.IsNullOrEmpty(BlogType))
            {
                bBlogTypeStatus = true;
                ImagePath = $"{BlogType}";
                // check path & images existing
                var LocationBlogFolder = $"{LocationRoute}/{BlogType}";
                var BlogFolderPath = FileUtil.GetMediaDirectory(LocationBlogFolder);

            }
            else
            {
                ErrorMessage = BootstrapHelper.GetBootstrapMessage("warning", $"Unknown Requested Blog Type.<br />Please contact system administrator.");
                BlogType = "";
                return;
            }

            if (bLocationStatus && bBlogTypeStatus) // Show Banner only for correct location & type, if allowed
            {
                RotatorTitle = $"{LocationName} {BlogType}";
                IsShowBanner = blogConfig.ShowBanner;
            }
            else
            {
                IsShowBanner = false;
            }

            string newMessage = _lc.Keys["BlogNoData", new { LocationName = LocationName, ContentType = BlogType }];
            NoDataMessage = (MarkupString)newMessage;

            StateHasChanged();

        } // Check Parameters

        private async Task LoadSourceData()
        {
            await LoadLocation(); //all Locations or Location by Route
            await LoadContent(); // all Contents for Blogs                                                      

        } // Load Source Data


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
            }
            else
            {
                LocationId = 0; // unknown Location
                IsShowBanner = false; // no banner
                ErrorMessage = BootstrapHelper.GetBootstrapMessage("warning", $"Cannot load location data. Please contact system administrator.");
            }

        } // Load Location

        //TODO:  Performance Violation of filtering in memory
        private async Task LoadContent()
        {

            var contentResult = await _svcContent.GetAllAsync();
            if (contentResult != null && contentResult.Success)
            {
                lstContents = contentResult.Data.ToList();
                if (lstContents != null && lstContents.Count > 0)
                {

                    // Filter to Current Location & Type
                    lstContents = lstContents.Where(c => c.LocationId == LocationId && c.ContentType.ToString() == BlogType).ToList();
                    if (lstContents != null && lstContents.Count > 0) // Data Found for current Location/Type
                    {
                        _cards = BlogHelper.GetBlogItemsDataList(lstContents, LocationRoute, LocationName, BlogType, AllowedExtensions);
                        IsBlogData = true; // otherwise cannot show Blog Cards                        
                    }
                    else
                    {
                        IsBlogData = false;

                    }
                }
            }

        } // Load Content


        // TEST DATA AREA - START ==============================================================================

        //private void HandleTestMode()
        //{
        //    connectionString = _configuration.GetConnectionString("DefaultConnection");
        //    webRootPath = _environment.WebRootPath;

        //    // Validate and create test data if needed
        //    var validationResult = BlogTest.ValidateAndCreateTestData(connectionString, webRootPath, ResetAction);

        //    if (validationResult != null)
        //    {
        //        BlogContentCount = validationResult.BlogCount;
        //        ImageFolderCount = validationResult.FolderCount;
        //        UnzippedImagesCount = validationResult.ImageCount;
        //        TestBarClass = validationResult.TestBarStyleClass;

        //    }

        //} // Handle Test Mode


        //private async Task ResetTestData()
        //{
        //    bool isConfirmed = await JS.InvokeAsync<bool>("confirmReset", "Are you sure you want to reset the test data?");

        //    if (isConfirmed)
        //    {
        //        ResetAction = "reset";
        //        Debug.WriteLine($"Request Reset Test Data; current URL: {_navigationManager.Uri.ToString()}");
        //        // Proceed with resetting the test data
        //        HandleTestMode();  // Call the method that resets the data                
        //        await ReloadPage();
        //    }

        //}// Reset Test Data

        //private async Task ClearTestData()
        //{
        //    bool isConfirmed = await JS.InvokeAsync<bool>("confirmReset", "Are you sure you want to delete the test data?");

        //    if (isConfirmed)
        //    {
        //        ResetAction = "clear";
        //        Debug.WriteLine($"Request Clear Test Data; current URL: {_navigationManager.Uri.ToString()}");
        //        // Proceed with resetting the test data
        //        HandleTestMode();  // Call the method that resets the data
        //        await ReloadPage();
        //    }
        //} // Clear Test Data

        //private async Task ReloadPage()
        //{
        //    //_navigationManager.NavigateTo(_navigationManager.Uri, forceLoad: true);
        //    //await JS.InvokeVoidAsync("reloadPage");
        //    await JS.InvokeVoidAsync("eval", "window.location.href = window.location.href");
        //}// Reload Page                                 

        //// TEST DATA AREA - END  ==============================================================================


    } // class Blog
} // namespace
