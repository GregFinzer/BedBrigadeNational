using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using BedBrigade.Client.Services;
using BedBrigade.Data.Models;
using static BedBrigade.Common.Common;
using Syncfusion.Blazor.FileManager;
using System.Diagnostics;
using Newtonsoft.Json;

namespace BedBrigade.Client.Components
{
    public partial class FileManager: ComponentBase
    {
       // Data Services
        [Inject] private IConfigurationService? _svcConfiguration { get; set; }
        [Inject] private ILocationService? _svcLocation { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }
        private ClaimsPrincipal? Identity { get; set; }

        public SfFileManager<FileManagerDirectoryContent>? fileManager;

        private const string PathDivider = "/";
        private const string DoubleBackSlash = "\\";
        private const string SubfolderKey = "MainMediaSubFolder";
        private const string SiteRoot = "wwwroot/";
        private string? MainAdminFolder { get; set; } = String.Empty;
        private string @ErrorMessage = String.Empty;
        private string MediaRoot = "wwwroot/media";      
      
        private Dictionary<string, string?> dctConfiguration { get; set; } = new Dictionary<string, string?>();
        private string AllowedExtensions  = String.Empty;
        private double MaxFileSize = 0;
        private int userLocationId = 0;
        public string userRoute = String.Empty;
        private string userRole = String.Empty;
        private string userName = String.Empty;
        public bool isLocationAdmin = false;
        private List<Location>? lstLocations;
        public bool isRead = true;
        public string[] toolbarItems = { "NewFolder" };
        public string[] menuItems = {"Cut", "Copy", "Paste", "Delete", "Rename" };

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

        public void success(SuccessEventArgs<FileManagerDirectoryContent> args)
        {

            Debug.WriteLine("success: " + JsonConvert.SerializeObject(args));

            bool isFile = args.Result.CWD.IsFile;
            bool isPagesFolder = false;
            bool isMediaRoot = false;

           if (fileManager.Path == PathDivider || args.Result.CWD.Name.ToString().ToLower() == dctConfiguration["MediaFolder"].ToString().ToLower() ) // For National Admin only
           {
                    isMediaRoot = true;
           }

            if (args.Result.CWD.Name.ToString().ToLower().Contains("/pages"))
            {
                isPagesFolder = true;
            }

            SetMenu(isFile, isPagesFolder, isMediaRoot);

        } // success

        public void objectSelected(FileSelectEventArgs<FileManagerDirectoryContent> args)
        {
            Debug.WriteLine("object selected: " + JsonConvert.SerializeObject(args));
           
            bool isPagesFolder = false;
            bool isFile = false;
            string selectedFolder = String.Empty;
            try
            {
                if (args.FileDetails.Name != null) // sametimes null or error
                {
                    if (args.FileDetails.Name.ToString().ToLower() == "pages")
                    {
                        isPagesFolder = true;
                    }
                    isFile = args.FileDetails.IsFile;
                }
            }
            catch(Exception ex) { }                  

            SetMenu(isFile, isPagesFolder);           
           

        } // selection folder or file

        public void OnMenuOpen(MenuOpenEventArgs<FileManagerDirectoryContent> args)
        {         
            bool isPagesFolder = false;
            bool isFile = false;

            if (args.FileDetails != null)
            {
                isFile = args.FileDetails[0].IsFile;
                if (args.FileDetails[0].Name.ToString() == "pages")
                {
                    isPagesFolder = true;
                }                
            }

            if (!isFile) { 
                SetMenu(isFile, isPagesFolder);
            }

        } // Context Menu Open

        public void SetMenu(bool isFile = false, bool isPagesFolder = false, bool isMediaRoot=false)  
        {
            // for folders only
            // For National Admin - no manage location folders
            // For all Admin - no manage /pages Folder
            // additional validation fileManager.Path
            // enable all menu items by default          
            // enable all menu items by default
            fileManager.EnableToolbarItems(toolbarItems);
            fileManager.EnableToolbarItems(menuItems);
                                  
            bool bDisableEditing = false;

            if (!isLocationAdmin) // National Admin 
            {
                if (isMediaRoot)
                {
                    bDisableEditing = true;   
                }
            }
           
            if (fileManager.Path != null)
            {
                if (fileManager.Path.ToString().ToLower().Contains("/pages"))
                { // for all admin
                    bDisableEditing = true;
                }

                if (!isLocationAdmin && (fileManager.Path == PathDivider || isLocationFolder()))
                {
                    bDisableEditing = true;
                }
            }

            if (isFile)
            {
                bDisableEditing = false;
            }                                       

            if (bDisableEditing)
            {
                fileManager.DisableToolbarItems(toolbarItems);
                fileManager.DisableToolbarItems(menuItems);
                fileManager.DisableMenuItems(menuItems);
            }

        } // Set Menu


        public void onsend(BeforeSendEventArgs args)
        {

            if (isRead && args.Action == "read")
            {
                // send only not for national admin           

                if (isLocationAdmin) // Not Admin User
                {
                    args.HttpClientInstance.DefaultRequestHeaders.Add("rootfolder", userRoute); // UserPath cannot be empty
                }
                isRead = false;
            }
        }// onsend

        public void beforeImageLoad(BeforeImageLoadEventArgs<FileManagerDirectoryContent> args)
        {
            if (isLocationAdmin)
            {
                args.ImageUrl = args.ImageUrl + "&SubFolder=" + userRoute;
            }
        } // before Image Load

        public void beforeDownload(BeforeDownloadEventArgs<FileManagerDirectoryContent> args)
        {
            if (isLocationAdmin)
            {  // add local path 
                if (args.DownloadData.Path == PathDivider)
                {
                    args.DownloadData.Path = PathDivider + userRoute + PathDivider;
                    foreach (var myFile in args.DownloadData.DownloadFileDetails)
                    {
                        myFile.Path = DoubleBackSlash + userRoute + DoubleBackSlash;
                        myFile.FilterPath = DoubleBackSlash + userRoute + DoubleBackSlash;
                    }
                }
                else
                {
                    args.DownloadData.Path = PathDivider + userRoute + args.DownloadData.Path;
                    foreach (var myFile in args.DownloadData.DownloadFileDetails)
                    {
                        myFile.Path = args.DownloadData.Path;
                        myFile.FilterPath = args.DownloadData.Path;
                    }
                }

                //var jsonString = JsonSerializer.Serialize(args);
                //Debug.WriteLine(jsonString);
                //args.Cancel = true;                       
            } // Location Admin

        } // before Image Load

        public bool isLocationFolder()
        {
            bool bLocation = false;
            string folderPath = String.Empty;
            string selectedLocation = PathDivider;

            if (fileManager.Path != null)
            {
                folderPath = fileManager.Path.ToString().Trim();
            }
            else
            {
                return (false);
            }

            // check location name in Path /xxxxxxx/ - when selected in left panel list
            int slashCount = folderPath.Count(t => t == '/');
            if (slashCount == 2)
            { // take text between /xxxxx/
                var arLocation = folderPath.Split(PathDivider);
                if (arLocation[1].ToLower() != "national") // special validation, because route "/" is national
                {
                    selectedLocation += arLocation[1];
                }

                if (lstLocations != null)
                {
                    int index = lstLocations.FindIndex(item => item.Route.ToLower() == selectedLocation.ToLower());
                    //Debug.WriteLine(selectedLocation + " => " + index);
                    if (index > -1)
                    { bLocation = true; }
                }
            }

            return (bLocation);

        } // Is Location Folder

       

    } // File Manager Class
} // Namespace





