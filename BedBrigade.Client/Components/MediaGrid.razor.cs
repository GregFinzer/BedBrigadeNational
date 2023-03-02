using BedBrigade.Client.Services;
using BedBrigade.Data.Models;
using BedBrigade.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Security.Claims;
using Action = Syncfusion.Blazor.Grids.Action;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.DropDowns;
using BedBrigade.Data.Data.Seeding;
using System.Data.Entity;
using Syncfusion.Blazor.Notifications.Internal;
using System.Collections.Generic;
using BedBrigade.Client.Pages.Administration.Manage;

namespace BedBrigade.Client.Components
{
    public partial class MediaGrid : ComponentBase
    {
        [Inject] private IConfigurationService? _svcConfiguration { get; set; }
        [Inject] private ILocationService? _svcLocation { get; set; }
        [Inject] private IUserService? _svcUser { get; set; }
        [Inject] private IMediaService? _svcMedia { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }

        [Parameter] public string? Id { get; set; }

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        private ClaimsPrincipal? Identity { get; set; }
        protected List<Media>? MediaFiles { get; set; }
        protected SfGrid<Media>? Grid { get; set; }

        protected List<Location>? lstLocations;
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        protected string? _state { get; set; }
        protected string? editHeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }
        protected string? addNeedDisplay { get; private set; }
        protected string? editNeedDisplay { get; private set; }
        protected SfToast? ToastObj { get; set; }
        protected string? ToastTitle { get; set; }
        protected string? ToastContent { get; set; }
        protected int ToastTimeout { get; set; } = 5000; // should be 3000

        protected string? RecordText { get; set; } = "Loading Media Files ...";
        protected string? Hide { get; private set; } = "true";
        public bool NoPaging { get; private set; }

        private string @ErrorMessage = String.Empty;

        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "200px" };

        private string MediaRoot = "wwwroot/media";
        private string MediaFolder = "media";
        private List<string> FolderList = new List<string>();
        private int VideoSizeLimit = 0;
        private int FileSizeLimit = 0;
        private string VideoFileTypes = "???";
        private string FileTypes = "???";
        private string MainMediaSubFolder = String.Empty;
        private string DropFileFolder = String.Empty;
        private string CurrentUserRole = String.Empty;
        private string CurrentUserName = String.Empty;
        private int UserLocationId = 0;
        private string UserLocationFolder = String.Empty;

        private Dictionary<string, string?> dctConfiguration { get; set; } = new Dictionary<string, string?>();
        
        // System Events
        protected override async Task OnInitializedAsync()
        {
            // User Identity
            var authState = await _authState.GetAuthenticationStateAsync();                      
            Identity = authState.User;
            CurrentUserName=Identity.Identity.Name;
            bool IsAdmin = false;
            if (Identity.IsInRole("National Admin")) // not perfect! for initial testing
            {
                IsAdmin = true;
                CurrentUserRole = "National Admin";

            }
            else // Location User
            {
                if(Identity.IsInRole("Location Admin"))
                {
                    IsAdmin = true;
                    CurrentUserRole = "Location Admin";
                }

                // Get User Location
                if (CurrentUserName!=null){
                    // get user name to search as part of Identity Name
                    var arUser = CurrentUserName.Split("@");
                    var SearchUserName = arUser[0].Replace(".", ""); // remove dot

                    var myUser = await _svcUser.GetUserAsync(SearchUserName);
                    if(myUser!=null && myUser.Success)
                    {
                        UserLocationId = myUser.Data.LocationId;
                    }
                }

            } // User Identity


             if (IsAdmin) // Add/Edit
              {
                  ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                  ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
              }
              else
              {
                  ToolBar = new List<string> { "Search", "Reset" };
                  ContextMenu = new List<string> { FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
              }

            var dataConfiguration = await _svcConfiguration.GetAllAsync(); // Configuration ============================
            if (dataConfiguration.Success && dataConfiguration != null)
            {
                var lstConfiguration = dataConfiguration.Data;
                // get configuration dictionary
                dctConfiguration = lstConfiguration.ToDictionary(keySelector: x => x.ConfigurationKey, elementSelector: x => x.ConfigurationValue);
                MediaFolder = dctConfiguration["MediaFolder"].ToString();
                MediaRoot = "wwwroot/" + MediaFolder;
                VideoSizeLimit = Convert.ToInt32(dctConfiguration["MaxVideoSize"]);
                VideoFileTypes = dctConfiguration["AllowedVideoExtensions"].ToString();
                FileSizeLimit = Convert.ToInt32(dctConfiguration["MaxFileSize"]);
                FileTypes = dctConfiguration["AllowedFileExtensions"].ToString();
                MainMediaSubFolder = dctConfiguration["MainMediaSubFolder"].ToString();

                lstConfiguration.Clear();
                lstConfiguration = null;

                DropFileFolder = "/"+MainMediaSubFolder;
               
            }
            else
            {
                ErrorMessage = "Cannot Load Configuration Data";
            }
            // ===============================================================================================================

            var dataLocations = await _svcLocation.GetAllAsync(); // get Locations ===========================================

            if (dataLocations.Success) // 
            {
                lstLocations = dataLocations.Data;
                if (lstLocations != null && lstLocations.Count > 0)
                {
                    // select Location Route for User Location ID
                    if (UserLocationId > 0) {
                        //  var myLocation = lstLocations.Select()
                        var myLocation = lstLocations.SingleOrDefault(a => a.LocationId == UserLocationId);
                        if (myLocation != null)
                        {
                            UserLocationFolder = myLocation.Route;
                            if (UserLocationFolder == "/")
                            {
                                UserLocationFolder =  MainMediaSubFolder; // Very rare situation, because "/" is national and available only for National Admin
                            }
                            else
                            {
                                UserLocationFolder = UserLocationFolder.Replace("/", "");                              
                            }
                        }

                        DropFileFolder = "/"+ UserLocationFolder;
                    }
                    else
                    { // Admin User
                        UserLocationFolder = MainMediaSubFolder;
                    }

                    if (UserLocationId > 0)
                    {
                        // single location
                        FolderList.Add("/" + UserLocationFolder);
                    }
                    else
                    { // the lait of locations/folders
                        FolderList.AddRange(lstLocations.Select(i => i.Route));
                        FolderList = FolderList.Select(i =>
                               {
                                   if (i == "/") i = ("/" + MainMediaSubFolder);
                                   return i;
                               }).ToList();
                        FolderList.Sort(); 
                    }
                } // Locations found             

            } // Locations ===================================================================================================


            try // get Media Files ===========================================================================================
            {
                var dataMedia = await _svcMedia.GetAllAsync(); // get Media
                if (dataMedia.Success)
                {   
                    var dbMedia = dataMedia.Data;
                    
                    if (dbMedia != null && dbMedia.Count > 0)
                    {
                        //FileDbCount = dbMedia.Count;
                        MediaFiles = SetLocationFilter(dbMedia); 
                       
                    }
                    else // no records in media table
                    {
                        var initMedia = new Media
                        {
                            LocationId = 1,
                            FileName = "logo",
                            MediaType = "png",
                            FilePath = MediaFolder + "/" + MainMediaSubFolder,
                            FileSize = 9827,
                            AltText = "Bed Brigade National Logo",
                            FileStatus = "seed"
                           // CreateUser=CurrentUserName,
                           // UpdateUser=CurrentUserName
                        };

                        await _svcMedia.CreateAsync(initMedia);
                        // reload Media Files
                        dataMedia = await _svcMedia.GetAllAsync(); // refresh File List
                        if (dataMedia.Success)
                        {
                            dbMedia = dataMedia.Data;

                            if (dbMedia != null && dbMedia.Count > 0)
                            {
                                //FileDbCount = dbMedia.Count;
                                MediaFiles = SetLocationFilter(dbMedia);
                            }
                            else // no records in media table
                            {
                                ErrorMessage = "No DB Files found";
                            }
                        }
                        else
                        {
                            ErrorMessage = "No DB Files. " + dataMedia.Message;
                        }
                    }
                }
                else
                {
                    ErrorMessage = "No DB Files. " + dataMedia.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "DB Media Error. " + ex.Message;
            } // End Load Media Files ==============================================================================        

            
        } // Async Init


        private List<Media> SetLocationFilter(List<Media> dbFileList)
        {
           // throw new NotImplementedException();
            // if Not National Admin User linked to location, Grid should show only location files
            
            if (UserLocationId > 1 || (CurrentUserRole == "Location Admin" && UserLocationId==1) )
            {
                List<Media> LocationFiles = dbFileList.FindAll(a => a.LocationId == UserLocationId);
                return (LocationFiles);
            }
            else
            {
                return (dbFileList);
            }
            
        } // Set Location Filter
         
        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                //  if (Identity.IsInRole("National Admin")  || Identity.IsInRole("Location Admin"))
                // {
                    Grid.EditSettings.AllowEditOnDblClick = true;
                    Grid.EditSettings.AllowDeleting = true;
                    Grid.EditSettings.AllowAdding = true;
                    Grid.EditSettings.AllowEditing = true;
                    StateHasChanged();
               // }
            } // firstRender
            return base.OnAfterRenderAsync(firstRender);
        }
        /// <summary>
        /// On loading of the Grid get the user grid persited data
        /// </summary>
        /// <returns></returns>
        protected async Task OnLoad()
        {
          //  var result = await _svcUser.GetPersistAsync(Common.Common.PersistGrid.User);
          //  if (result.Success)
          //  {
           //     await Grid.SetPersistData(_state);
           // }
        }

        protected async Task OnDestroyed()
        {
           // _state = await Grid.GetPersistData();
           // await _svcUser.SavePersistAsync(new Persist { GridId = (int)Common.Common.PersistGrid.Location, UserState = _state });
        }         


        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if (args.Item.Text == "Reset")
            {
               // await Grid.ResetPersistData();
               // _state = await Grid.GetPersistData();
              //  await _svcUser.SavePersistAsync(new Persist { GridId = (int)Common.Common.PersistGrid.Location, UserState = _state });
                return;
            }

            if (args.Item.Text == "Pdf Export")
            {
                await PdfExport();
            }
            if (args.Item.Text == "Excel Export")
            {
                await ExcelExport();
                return;
            }
            if (args.Item.Text == "Csv Export")
            {
                await CsvExportAsync();
                return;
            }

        }

        public async Task OnActionBegin(ActionEventArgs<Media> args)
        {
           
            var requestType = args.RequestType;
            switch (requestType)
            {
                case Action.Searching:
                    RecordText = "Searching ... Record Not Found.";
                    break;

                case Action.Delete:
                    await Delete(args);
                    break;

                case Action.Add:
                    Add();
                    break;

                case Action.Save:
                    await Save(args);
                    break;
                case Action.BeginEdit:
                    BeginEdit();
                    break;
            }

        }

        private async Task Delete(ActionEventArgs<Media> args)
        {
            List<Media> records = await Grid.GetSelectedRecordsAsync();
            foreach (var rec in records)
            {
                var deleteResult = await _svcMedia.DeleteAsync(rec.MediaId);
                ToastTitle = "Delete Media File";  // + Physical Media File
                if (deleteResult.Success)
                {
                    ToastContent = "Delete Successful!";
                }
                else
                {
                    ToastContent = $"Unable to Delete. Media File is in use.";
                    args.Cancel = true;
                }
                ToastTimeout = 6000;
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });

            }
        }

        private void Add()
        {
            editHeaderTitle = "Upload new Media File";
            ButtonTitle = "Close";
        }

        private async Task Save(ActionEventArgs<Media> args)
        {            
            if (args.Data.MediaId > 0)
            {
                try
                {
                    bool updateDb = false;
                    bool renameLock = false;
                    string renameResult = String.Empty;
                    string? PreviousFileName = args.PreviousData.FileName;
                    string? NewFileName = args.Data.FileName;
                    Media MediaFile = args.Data;
                    //
                    ToastTitle = "Update Media File ID = " + MediaFile.MediaId;
                    ToastContent = String.Empty;
                    // step 1 - Try to rename physical file
                    if (PreviousFileName != null && NewFileName != null)
                    {
                        if (PreviousFileName.Trim() != NewFileName.Trim())
                        { // rename file
                            renameResult = RenameFile(MediaFile, PreviousFileName);
                            if (renameResult == "Success")
                            {
                                ToastContent += "Physical file renamed.<br />";
                                updateDb = true;
                            }
                            else
                            {
                                ToastContent += "File cannot be renamed.<br />";
                                ToastContent += renameResult;
                                renameLock = true;
                                MediaFile.FileName = PreviousFileName; // restore file name
                            }
                        }
                    }

                    // step 2 - update database record if AltText changed
                    if (args.PreviousData.AltText != null && args.Data.AltText != null)
                    {
                        if (args.PreviousData.AltText.Trim() != args.Data.AltText.Trim())
                        {
                            updateDb = true;
                        }
                    }

                    if (updateDb)
                    {
                        try
                        {                           
                                //Update Media Record if File Name or AltText were changed
                                var updateResult = await _svcMedia.UpdateAsync(MediaFile);

                                if (updateResult.Success)
                                {
                                    ToastContent += "File Updated in DB successfully!";

                                }
                                else
                                {
                                    ToastContent += "Unable to update File: " + updateResult.Message;
                                }                           
                        }
                        catch (Exception ex)
                        {
                            ToastTitle = "Update Media File Error!";
                            ToastContent += ex.Message;
                        }
                    }
                    else
                    {                        
                        ToastContent += "<br />No changes!";
                    }
                }
                catch (Exception ex) 
                {
                    ToastTitle = "Update Media File Error!";
                    ToastContent = ex.Message; 
                }
            } // Media ID > 0


            await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });

        } // save

        private void BeginEdit()
        {
            editHeaderTitle = "Update Media File";
            ButtonTitle = "Update Media File";
        }

        protected async Task Save(Media MediaFile)
        {
            await Grid.EndEditAsync();
        }

        protected async Task Cancel()
        {
            await Grid.CloseEditAsync();
        }

        protected async Task DataBound()
        {
            if (MediaFiles == null || MediaFiles.Count == 0) RecordText = "No Registered Files records found";
            if (Grid.TotalItemCount <= Grid.PageSettings.PageSize)  //compare total grid data count with pagesize value 
            {
                NoPaging = true;
            }
            else
            {
                NoPaging = false;
            }

        }

        protected async Task PdfExport()
        {
            PdfExportProperties ExportProperties = new PdfExportProperties
            {
                FileName = "MediaFiles_" + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.ExportToPdfAsync(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "MediaFiles_" + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExportToExcelAsync();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "MediaFiles_" + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.ExportToCsvAsync(ExportProperties);
        }

        private async void OnUploadFileChange(UploadChangeEventArgs args)
        { // FileTargetFolder - selected by User
            foreach (var file in args.Files)
            {
                // Check Folder
                var TargetFolder = MediaRoot + DropFileFolder;
                if (!System.IO.Directory.Exists(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), TargetFolder)))
                {
                    Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), TargetFolder));
                }

                var FileTarget = TargetFolder + "/" + file.FileInfo.Name;
                              
                var UploadPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), FileTarget);
                FileStream filestream = new FileStream(UploadPath, FileMode.Create, FileAccess.Write);
                file.Stream.WriteTo(filestream);
                filestream.Close();
                file.Stream.Close();
                // add file to database
                await SaveFileToDatabase(file, DropFileFolder);
            }          

        } // file upload

        private async void OnUploadVideoChange(UploadChangeEventArgs args)
        { // VideoTargetFolder - selected by User
            foreach (var file in args.Files)
            {
                // Check Folder
                var TargetFolder = MediaRoot + DropFileFolder;
                if (!System.IO.Directory.Exists(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), TargetFolder)))
                {
                    Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), TargetFolder));
                }

                var FileTarget = TargetFolder + "/" + file.FileInfo.Name;
                var UploadPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), FileTarget);
                FileStream filestream = new FileStream(UploadPath, FileMode.Create, FileAccess.Write);
                file.Stream.WriteTo(filestream);
                filestream.Close();
                file.Stream.Close();
                // add file to database
                await SaveFileToDatabase(file, DropFileFolder);
            }        


        } // file upload


        private async Task SaveFileToDatabase(UploadFiles upFile, string TargetFolder)
        {
            string FilePath = MediaFolder + TargetFolder;
            Media myMedia = new Media
            {
                LocationId = GetLocationId(TargetFolder),
                FileName = Path.GetFileNameWithoutExtension(upFile.FileInfo.Name),
                MediaType = upFile.FileInfo.Type,
                FilePath = FilePath,
                FileSize = (int)upFile.FileInfo.Size,
                AltText = Path.GetFileNameWithoutExtension(upFile.FileInfo.Name),
                FileStatus = "upload"
                
                // CreateDate = DateTime.Now,
                // UpdateDate = DateTime.Now,
                // CreateUser = SeedConstants.SeedUserName,
                // UpdateUser = SeedConstants.SeedUserName,
                // MachineName = Environment.MachineName
            };
            if (_svcMedia != null)
            {
                try
                {
                    var dbResponse = await _svcMedia.CreateAsync(myMedia);
                    if (dbResponse.Success) {              // not poerfect! Should be updated         
                        // add new items to File List
                       // MediaFiles.Add(dbResponse.Data);
                        //StateHasChanged();
                        //Grid.Refresh();
                       RefreshPage();
                    } 
                }
                catch (Exception ex) { }

            }

        } // Save Uploaded File to Database

        private async void RefreshPage()
        {
           // await Grid.EndEditAsync();
           // StateHasChanged();
           // await Grid.Refresh();

            await Task.Delay(1000); // 1 sec delay
            NavigationManager.NavigateTo(NavigationManager.Uri, true);
        }


        private string FormatFileSize(long intFileSize)
        {
            string strFileSize = String.Empty;
            try
            {
                // Return formatted file size (source size in bytes)
                double dblSize = 0.0;

                if (intFileSize > 1024)
                {
                    dblSize = Convert.ToDouble(intFileSize) / 1024;
                    intFileSize = Convert.ToInt32(Math.Ceiling(dblSize));
                    strFileSize = Convert.ToString(intFileSize) + " KB";
                    if (intFileSize > 1024)
                    {
                        dblSize = Convert.ToDouble(intFileSize) / 1024;
                        strFileSize = Microsoft.VisualBasic.Strings.FormatNumber(dblSize, 1) + " MB";
                    }
                }
                else // bytes
                {
                    strFileSize = Convert.ToString(intFileSize) + " B";
                }
            }
            catch (Exception ex) { strFileSize = "???"; }
            return (strFileSize);
        }//FormatFileSize

        private string RenameFile(Media myFile, string OldFileName)
        {
            var result = "Success";
            var FileLocation = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot/" + myFile.FilePath);
            string FullNameOldFile = FileLocation + "/" + OldFileName + "." + myFile.MediaType;
            string FullNameNewFile = FileLocation + "/" + myFile.FileName + "." + myFile.MediaType;
            try
            {                
                System.IO.File.Move(FullNameOldFile, FullNameNewFile);
                return (result);
            }
            catch(Exception ex) {
                return (ex.Message+": "+ FullNameOldFile);
            }
           
        } // Rename File


        private bool IsFileExists(Media dbFile)
        {
            bool response = false;
            var FileFolder = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot/" + dbFile.FilePath);
            var FullFileName = FileFolder + "/" + dbFile.FileName + "." + dbFile.MediaType;
            if (File.Exists(FullFileName))
            {
                response = true;
            }
            return (response);
        } // File Exists?

        private int GetLocationId(string Route)
        {
            var LocationId = 1; // MainMediaSubFolder

            if (Route == "/" + MainMediaSubFolder)
            {
                Route = "/";
            }

            if (lstLocations != null)
            {
                if (lstLocations.Count > 0)
                {
                    var myLocation = lstLocations.Find(x => x.Route == Route);
                    if (myLocation != null) // location found 
                    {
                      LocationId = myLocation.LocationId;
                    }
                }
            }
           
            return (LocationId);

        }// Get Location Id


    } // class
} // namespace

