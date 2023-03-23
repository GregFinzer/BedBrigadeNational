using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using BedBrigade.Client.Services;
using BedBrigade.Data.Models;
using static BedBrigade.Common.Common;

namespace BedBrigade.Client.Components
{
    public partial class FileManager: ComponentBase
    {
       // Data Services
        [Inject] private IConfigurationService? _svcConfiguration { get; set; }
        [Inject] private ILocationService? _svcLocation { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }
        private ClaimsPrincipal? Identity { get; set; }
                
        private const string PathDivider = "/";       
        private const string SubfolderKey = "MainMediaSubFolder";
        private const string SiteRoot = "wwwroot/";
        private string? MainAdminFolder { get; set; } = String.Empty;
        private string @ErrorMessage = String.Empty;
        private string MediaRoot = "wwwroot/media";      
      
        private Dictionary<string, string?> dctConfiguration { get; set; } = new Dictionary<string, string?>();
        private string AllowedExtensions  = String.Empty;
        private double MaxFileSize = 0;
        private int userLocationId = 0;
        private string userRoute = String.Empty;
        private string userRole = String.Empty;
        private string userName = String.Empty;
        public bool isLocationAdmin = false;
        private List<Location>? lstLocations;

        //protected List<Location>? lstLocations;

        protected override async Task OnInitializedAsync()
        {
           
            var authState = await _authState!.GetAuthenticationStateAsync();
            Identity = authState.User;
            userName = Identity.Identity.Name;
            userLocationId = int.Parse(Identity.Claims.FirstOrDefault(c => c.Type == "LocationId").Value);
            userRoute = Identity.Claims.FirstOrDefault(c => c.Type == "UserRoute").Value;
            userRoute = userRoute.Replace(PathDivider, "");

            if (Identity.IsInRole("National Admin")) // not perfect! for initial testing
            {
                userRole = "National Admin";                
            }
            else // Location User
            {
                if (Identity.IsInRole("Location Admin"))
                {                   
                    userRole = "Location Admin";
                    isLocationAdmin = true;
                }

                if (Identity.IsInRole("Location Author"))
                {
                   userRole = "Location Author";
                   isLocationAdmin = true;
                }
            }                 
                       
            var dataConfiguration = await _svcConfiguration.GetAllAsync(ConfigSection.Media); // Configuration ============================
            if (dataConfiguration.Success && dataConfiguration != null)
            {
                dctConfiguration = dataConfiguration.Data.ToDictionary(keySelector: x => x.ConfigurationKey, elementSelector: x => x.ConfigurationValue);
                AllowedExtensions = dctConfiguration["AllowedFileExtensions"].ToString() + "," + dctConfiguration["AllowedVideoExtensions"].ToString();
                MaxFileSize = Convert.ToDouble(dctConfiguration["MaxVideoSize"]);
                MediaRoot = SiteRoot + dctConfiguration["MediaFolder"];
                MainAdminFolder = dctConfiguration[SubfolderKey];

                if (userLocationId == 1)
                {
                    userRoute = MainAdminFolder;
                }

            }

            await CheckLocationFolders();

        } // Init

        private async Task CheckLocationFolders()
        { // Loop in location list & create location folder, if not exist

            var dataLocations = await _svcLocation!.GetAllAsync();

            if (dataLocations.Success) // 
            {
                lstLocations = dataLocations.Data;

                foreach (var location in lstLocations)
                {
                    if (location.Route != "/")
                    {
                        // Check Folder
                        var TargetFolder = MediaRoot + location.Route;
                        if (!System.IO.Directory.Exists(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), TargetFolder)))
                        {
                            Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), TargetFolder));
                        }
                    }
                }
            }

        } // Check Location Folders




    } // File Manager Class
} // Namespace





