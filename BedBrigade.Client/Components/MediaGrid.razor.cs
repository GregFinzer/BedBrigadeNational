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
using System.Linq.Expressions;
using System.Drawing.Imaging;

namespace BedBrigade.Client.Components
{
    public partial class MediaGrid : ComponentBase
    {
        // Data Services
        [Inject] private IConfigurationService? _svcConfiguration { get; set; }
        [Inject] private ILocationService? _svcLocation { get; set; }
        [Inject] private IUserService? _svcUser { get; set; }
        [Inject] private IMediaService? _svcMedia { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }

        // Syncfusion Grid variables
        [Parameter] public string? Id { get; set; }                          

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
               
        protected SfGrid<Media>? Grid { get; set; }
              
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

        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "200px" };

        // Media Manager variables

        private const string PathDivider = "/";
        private const string PathDot = ".";
        private const string SubfolderKey = "MainMediaSubFolder";
        private const string SiteRoot = "wwwroot/";

        private ClaimsPrincipal? Identity { get; set; }
        protected List<Media>? MediaFiles { get; set; }

        protected List<Location>? lstLocations;

        private string @ErrorMessage = String.Empty;
               
        private string MediaRoot = "wwwroot/media";
        private MediaHelper.MediaUser MediaUser = new MediaHelper.MediaUser();                                       

        private Dictionary<string, string?> dctConfiguration { get; set; } = new Dictionary<string, string?>();

        #region Initialization ===========================================================================================
        protected override async Task OnInitializedAsync()
        {
            await LoadUserData();
            SetGridToolBar();

             // Load data from database
             await LoadConfigurations();
             await LoadLocations();
             await LoadMediaData();
           

        } // Async Init

        private async Task LoadUserData()
        {
            // User Identity
            var authState = await _authState!.GetAuthenticationStateAsync();
            Identity = authState.User;

            if(Identity != null){
                MediaHelper.ReviewUserData(ref MediaUser, Identity, PathDot);           
            }

        } // Get User Data

        private async Task LoadConfigurations()
        {
            var dataConfiguration = await _svcConfiguration.GetAllAsync(); // Configuration ============================
            if (dataConfiguration.Success && dataConfiguration != null)
            {
              dctConfiguration = dataConfiguration.Data.ToDictionary(keySelector: x => x.ConfigurationKey, elementSelector: x => x.ConfigurationValue);
              MediaRoot = SiteRoot + dctConfiguration["MediaFolder"];                       
              MediaUser.DropFileFolder = PathDivider + dctConfiguration[SubfolderKey];
            }
            else
            {
                ErrorMessage = "Cannot Load Configuration Data";
            }
        } // Load Configuration

        private async Task LoadLocations()
        {
            var dataLocations = await _svcLocation!.GetAllAsync(); 

            if (dataLocations.Success) // 
            {
                lstLocations = dataLocations.Data;
                if (lstLocations != null && lstLocations.Count > 0)
                {
                    MediaHelper.GetUserLocation(ref MediaUser, ref lstLocations, dctConfiguration, PathDivider, SubfolderKey);                    
                } // Locations found             

            } 
        } // Load Locations

        private async Task LoadMediaData()
        {
            try // get Media Files ===========================================================================================
            {
                var dataMedia = await _svcMedia.GetAllAsync(); // get Media
                var dbMedia = new List<Media>();             

                    if (dataMedia.Success)
                    {
                        if (dataMedia!.Data.Count > 0)
                        {
                            dbMedia = dataMedia!.Data; // retrieve existing media records to temp list
                        }
                        else
                        {
                            dbMedia = await AddInitMedia();                           
                        } // no rows in Media

                        if (dbMedia != null && dbMedia.Count > 0)
                        {                           
                            MediaFiles = MediaHelper.SetLocationFilter(dbMedia, MediaUser);
                        }
                        else // no records in media table
                        {
                            ErrorMessage = "No DB Files found";
                        }
                    } // the first success
                }
                catch (Exception ex)
            {
                ErrorMessage = "No DB Files. " + ex.Message;
            }                 
               
        } // Load Media Data

       
        private async Task<List<Media>> AddInitMedia()
        {
            var newMediaList = new List<Media>();
            var initMedia = new Media
            {
                LocationId = 1,
                FileName = "logo",
                MediaType = "png",
                FilePath = dctConfiguration["MediaFolder"] + PathDivider + dctConfiguration[SubfolderKey],
                FileSize = 9827,
                AltText = "Bed Brigade National Logo",
                FileStatus = "seed"
            };

            var newMedia = await _svcMedia!.CreateAsync(initMedia);
            if (newMedia.Success)
            {
                var dataMedia = await _svcMedia.GetAllAsync(); // get Media
                if (dataMedia.Success && dataMedia.Data.Count > 0)
                {
                    newMediaList = dataMedia.Data;
                }
            }

            return newMediaList;

        } // Add Init Media

        

        #endregion Initialization ================================================================================================
           
        
        protected void SetGridToolBar()
        {
           
            if (MediaUser.IsAdmin) // Add/Edit
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
                ContextMenu = new List<string> { FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }

        } // Set Grid Toolbar


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
     
        protected async Task OnLoad()
        {
           // var result = await _svcUser.GetPersistAsync(Common.Common.PersistGrid.User);
          //  if (result.Success)
           // {
           //     await Grid.SetPersistDataAsync(_state);
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
            editHeaderTitle = "Upload new Media File to selected Location Folder ";
            ButtonTitle = "Close";
        }

        private async Task Save(ActionEventArgs<Media> args)
        {   // save file after editing - not Add!         
            if (args.Data.MediaId > 0)
            {
                int updateDb = 0;
                Media MediaFile = args.Data;
                ToastTitle = "Update Media File ID = " + MediaFile.MediaId;
                // step 1 - Try to rename physical file
                var renameResult = MediaHelper.RenameFile(ref MediaFile, args.PreviousData.FileName, PathDivider, PathDot);
                if (renameResult == "success")
                {
                    updateDb++; // database update required
                }
                else
                {
                    if (renameResult == "restore")
                    {
                        ToastContent += "File cannot be renamed.<br />";
                    }
                }
                // step 2 - check AltText Renaming                               
                if (args.PreviousData.AltText != null && args.Data.AltText != null)
                {
                    if (args.PreviousData.AltText.Trim() != args.Data.AltText.Trim())
                    {
                        updateDb++;
                    }
                }

                if(updateDb > 0) // database update required
                {                  
                    try
                    {
                        var updateResult = await _svcMedia.UpdateAsync(MediaFile);

                        if (updateResult.Success)
                        {
                            ToastContent += "File Updated in DB successfully!<br />";
                        }
                        else
                        {
                            ToastContent += "Unable to update File: " + updateResult.Message+"<br/>";
                        }

                    }
                    catch(Exception ex)
                    {
                        ToastContent += "Unable to update File: " + ex.Message+"<br />";
                    }
                }

                if (ToastContent.Trim().Length > 0)
                {
                    await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
                }

            } // Media ID > 0            

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

        #region Grid Export ======================================================================

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
        #endregion Grid Export =========================================================

        #region Upload File =====================================================================

        private async void OnUploadFileChange(UploadChangeEventArgs args)
        { // FileTargetFolder - selected by User
            string locationRoute = MediaUser.DropFileFolder.Split("[")[1].Replace("]", "").Trim();
            foreach (var file in args.Files)
            {
                // Check Folder
                var TargetFolder = MediaRoot + locationRoute;
                if (!System.IO.Directory.Exists(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), TargetFolder)))
                {
                    Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), TargetFolder));
                }

                var FileTarget = TargetFolder + PathDivider + file.FileInfo.Name;
                              
                var UploadPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), FileTarget);
                FileStream filestream = new FileStream(UploadPath, FileMode.Create, FileAccess.Write);
                file.Stream.WriteTo(filestream);
                filestream.Close();
                file.Stream.Close();
                // add file to database
                await SaveFileToDatabase(file, locationRoute);
            }          

        } // file upload
             

        private async Task SaveFileToDatabase(UploadFiles upFile, string TargetFolder)
        {
            try
            {
                string FilePath = dctConfiguration["MediaFolder"] + TargetFolder;
                Media myMedia = new Media
                {
                    LocationId = GetLocationId(TargetFolder),
                    FileName = Path.GetFileNameWithoutExtension(upFile.FileInfo.Name),
                    MediaType = upFile.FileInfo.Type,
                    FilePath = FilePath,
                    FileSize = (int)upFile.FileInfo.Size,
                    AltText = Path.GetFileNameWithoutExtension(upFile.FileInfo.Name),
                    FileStatus = "upload"

                };
                if (_svcMedia != null)
                {
                    try
                    {
                        var dbResponse = await _svcMedia.CreateAsync(myMedia);
                        if (dbResponse.Success)
                        {              // not perfect! Should be updated         

                            RefreshPage();
                        }
                    }
                    catch (Exception ex) { }

                }
            }
            catch(Exception ex)
            {
                ErrorMessage = ex.Message;
            }

        } // Save Uploaded File to Database

        #endregion Upload File ======================================================================

        private async void RefreshPage()
        {
           // await Grid.EndEditAsync();
           // StateHasChanged();
           // await Grid.Refresh();

            await Task.Delay(1000); // 1 sec delay
            NavigationManager.NavigateTo(NavigationManager.Uri, true);
        }

      
        private int GetLocationId(string Route)
        {
            var LocationId = 1; // MainMediaSubFolder

            if (Route == PathDivider + dctConfiguration[SubfolderKey])
            {
                Route = PathDivider;
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

        #region ======= Utility Functions ========================================================

  




        #endregion ===== Utility Functions =======================================================
    } // class
} // namespace

