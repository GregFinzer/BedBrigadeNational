using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using BedBrigade.Data.Models;
using static BedBrigade.Common.Logic.Common;
using Syncfusion.Blazor.FileManager;
using System.Diagnostics;
using Microsoft.JSInterop;
using BedBrigade.Data.Services;
using Serilog;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;

namespace BedBrigade.Client.Components
{
    public partial class FileManager: ComponentBase
    {
        // Data Services
        [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }

        [Parameter]
        public bool ShowHeader { get; set; } = true;

        [Parameter]
        public string FolderPath { get; set; } = String.Empty;

        private ClaimsPrincipal? Identity { get; set; }

        public SfFileManager<FileManagerDirectoryContent>? fileManager;

        private const string PathDivider = "/";
        private const string SubfolderKey = ConfigNames.MainMediaSubFolder;
        private const string SiteRoot = "wwwroot/";
        private string? MainAdminFolder { get; set; } = String.Empty;
      
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

        // preview file 
        private string? previewFileName { get; set; }
        private string? previewFileUrl { get; set; }
        private bool previewVisibility { get; set; } = false;

        //protected List<Location>? lstLocations;

        protected override async Task OnInitializedAsync()
        {
            var authState = await _authState!.GetAuthenticationStateAsync();
            Identity = authState.User;
            userName = Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Defaults.DefaultUserNameAndEmail;
            Log.Information($"{userName} went to the Manage Media Page");
            userLocationId = int.Parse(Identity.Claims.FirstOrDefault(c => c.Type == "LocationId").Value);
            userRoute = Identity.Claims.FirstOrDefault(c => c.Type == "UserRoute").Value;
            userRoute = userRoute.Replace(PathDivider, "");

            if (Identity.IsInRole(RoleNames.NationalAdmin)) // not perfect! for initial testing
            {
                userRole = RoleNames.NationalAdmin;                
            }
            else // Location User
            {
                if (Identity.IsInRole(RoleNames.LocationAdmin))
                {                   
                    userRole = RoleNames.LocationAdmin;
                    isLocationAdmin = true;
                }

                if (Identity.IsInRole(RoleNames.LocationAuthor))
                {
                   userRole = RoleNames.LocationAuthor;
                   isLocationAdmin = true;
                }
            }                 
                       
            var dataConfiguration = await _svcConfiguration.GetAllAsync(ConfigSection.Media); // Configuration ============================
            if (dataConfiguration.Success && dataConfiguration != null)
            {
                dctConfiguration = dataConfiguration.Data.ToDictionary(keySelector: x => x.ConfigurationKey, elementSelector: x => x.ConfigurationValue);
                AllowedExtensions = dctConfiguration[ConfigNames.AllowedFileExtensions].ToString() + "," + dctConfiguration[ConfigNames.AllowedVideoExtensions].ToString();
                MaxFileSize = Convert.ToDouble(dctConfiguration[ConfigNames.MaxVideoSize]);
                MainAdminFolder = dctConfiguration[SubfolderKey];

                if (userLocationId == (int)LocationNumber.National)
                {
                    userRoute = MainAdminFolder;
                }
            }

        } // Init



        public void success(SuccessEventArgs<FileManagerDirectoryContent> args)
        {
            bool isPagesFolder = false;
            bool isMediaRoot = false;

            if (args.Result != null && args.Result.CWD != null)
            {
                bool isFile = args.Result.CWD.IsFile; 

                if (fileManager.Path == PathDivider || args.Result.CWD.Name.ToString().ToLower() == dctConfiguration[ConfigNames.MediaFolder].ToString().ToLower()) // For National Admin only
                {
                    isMediaRoot = true;
                }

                if (args.Result.CWD.Name.ToString().ToLower().Contains("/pages"))
                {
                    isPagesFolder = true;
                }

                SetMenu(isFile, isPagesFolder, isMediaRoot);
            }


        } // success

        public void objectSelected(FileSelectEventArgs<FileManagerDirectoryContent> args)
        {
           //  Debug.WriteLine("object selected: " + JsonConvert.SerializeObject(args));
           
            bool isPagesFolder = false;
            bool isFile = false;
            if (args.FileDetails != null && args.FileDetails.Name != null) 
            {
                if (args.FileDetails.Name.ToString().ToLower() == "pages")
                {
                    isPagesFolder = true;
                }
                isFile = args.FileDetails.IsFile;
            }

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
            const string rootFolder = "rootfolder";
            //This is when we are overriding the FolderPath with a dialog
            if (!String.IsNullOrEmpty(FolderPath))
            {
                //Clear previous rootfolder header
                if (args.HttpClientInstance.DefaultRequestHeaders.Contains(rootFolder))
                {
                    args.HttpClientInstance.DefaultRequestHeaders.Remove(rootFolder);
                }

                args.HttpClientInstance.DefaultRequestHeaders.Add(rootFolder, FolderPath);
            }
            else if (isRead && args.Action == "read")
            {
                // send only not for national admin           
                if (isLocationAdmin) // Not Admin User
                {
                    args.HttpClientInstance.DefaultRequestHeaders.Add(rootFolder, userRoute); // UserPath cannot be empty
                }
                isRead = false;
            }
        }// onsend

        public void beforeImageLoad(BeforeImageLoadEventArgs<FileManagerDirectoryContent> args)
        {
            if (!String.IsNullOrEmpty(FolderPath))
            {
                args.ImageUrl = args.ImageUrl + "&SubFolder=" + FolderPath;
            }
            else if (isLocationAdmin)
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
                        myFile.Path = Path.DirectorySeparatorChar + userRoute + Path.DirectorySeparatorChar;
                        myFile.FilterPath = Path.DirectorySeparatorChar + userRoute + Path.DirectorySeparatorChar;
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

        public async Task fileOpen(FileOpenEventArgs<FileManagerDirectoryContent> args)
        {
            //Debug.WriteLine("File Open: " + JsonConvert.SerializeObject(args));

            string[] myFiles = { ".webp", ".pdf", ".mp4" };

            try
            {
                if (args.FileDetails != null && args.FileDetails.IsFile == true && myFiles.Contains(args.FileDetails.Type))
                {
                    previewFileName = args.FileDetails.Name;
                    previewFileUrl = NavigationManager.BaseUri.ToString() + dctConfiguration[ConfigNames.MediaFolder];
                    if (isLocationAdmin)
                    {
                        previewFileUrl = previewFileUrl + PathDivider + userRoute;
                    }

                    previewFileUrl = previewFileUrl + args.FileDetails.FilterPath + args.FileDetails.Name;
                   // Debug.WriteLine(previewFileUrl);

                    switch (args.FileDetails.Type)
                    {
                        case ".webp":
                            this.previewVisibility = true;
                            args.Cancel = true;
                            break;
                        case ".pdf":
                        case ".mp4":
                            await jsRuntime.InvokeVoidAsync("window.open", previewFileUrl, "_blank");
                            args.Cancel = true;
                            break;
                        default:
                            break;
                    }

                }

            }
            catch (Exception ex) { 
                Debug.WriteLine(ex.Message);
            }

        } // file Open


    } // File Manager Class
} // Namespace





