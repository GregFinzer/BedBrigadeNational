using BedBrigade.Common;
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Security.Claims;
using static BedBrigade.Common.Common;
using Newtonsoft.Json;
using Serilog;
using Syncfusion.Blazor.DropDowns;
using System.Diagnostics;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.Linq;
using Syncfusion.Blazor.Navigations;
using Syncfusion.Blazor.RichTextEditor;
using static BedBrigade.Client.Components.MediaHelper;
using System.Collections.Generic;

namespace BedBrigade.Client.Components
{
    public partial class EvolGrid : ComponentBase
    {
        // data services

        [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }
        [Inject] private IVolunteerDataService? _svcVolunteer { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }
        [Inject] private IScheduleDataService? _svcSchedule { get; set; }
        [Inject] private IVolunteerEventsDataService? _svcVolunteerEvents { get; set; }

        // object lists

        protected List<Volunteer>? Volunteers { get; set; }
        protected List<Volunteer>? EventVolunteers { get; set; }
        protected List<Volunteer>? lstVolunteerSelector { get; set; }
        protected List<Volunteer>? lstLocationVolunteers { get; set; } = new List<Volunteer>();
        protected List<Location>? Locations { get; set; }
        protected List<Schedule>? Schedules { get; set; }
        protected List<VolunteerEvent>? VolunteerEvents { get; set; }
        protected List<Syncfusion.Blazor.Navigations.ItemModel> Toolbaritems = new List<Syncfusion.Blazor.Navigations.ItemModel>();
        protected List<VehicleTypeEnumItem>? lstVehicleTypes { get; set; }
      

        // identity variables

        private string userRole = String.Empty;
        private string userName = String.Empty;
        private string userLocationName = String.Empty;
        private int userLocationId = 0;      
        public bool isLocationAdmin = false;    

        // variables

        private ClaimsPrincipal? Identity { get; set; }
        protected string? RecordText { get; set; } = "Loading Volunteers ...";

        // Grid regerences

        protected SfGrid<Volunteer>? Grid { get; set; }

        // Constants
        private const string DisplayNone = "none";
        private const string CaptionClose = "Close";
        private const string CaptionWarning = "warning";
        private const string CaptionAdd = "Add";
        private const string CaptionDelete = "Delete";
        private const string RegisterColumn = "RegistrationId";

        // Action & Dialog variables

        public bool ShowEditDialog { get; set; } = false;
        private string ErrorMessage = String.Empty;
        private string DeleteStatusMessage = String.Empty;
        private bool DeletePermit = false;
        private string DisplayAddButton = DisplayNone;
        private string DisplayDeleteButton = DisplayNone;
        private string DisplayDataPanel = DisplayNone;
        private string DisplaySearchPanel = DisplayNone;
        private MarkupString DeleteMessage;
           
        private MarkupString DialogMessage;
        private MarkupString AvailabilityMessage; // Volunteer Availability
        private string DialogTitle = string.Empty;
        private string CloseButtonCaption = CaptionClose;

        public  Volunteer? selectedGridObject { get; set; }
        public Volunteer? newVolunteer { get; set; }
        public int foundVolunteerId = 0;
        public bool displayId = false;
        public string displayVolunteerData = DisplayNone;
               
        // test variables

        public string strJson = string.Empty;
        public string strHtml = string.Empty;
        private string testString = string.Empty;   
     
        protected override async Task OnInitializedAsync()
        {
            await LoadUserData();
            await LoadConfigurations();
            await LoadLocations();
            await LoadVolunteerData();
            //await LoadScheduleData();
            Schedules = await EvolHelper.GetSchedules(_svcSchedule, isLocationAdmin, userLocationId); ;
            //await LoadVolunteerEvents();
            VolunteerEvents = await EvolHelper.GetVolunteerEvents(_svcVolunteerEvents, isLocationAdmin, userLocationId);        
            lstVehicleTypes = GetVehicleTypeItems();
            DisableToolBar();
            PrepareGridData();

        } // Async Init

        private async Task LoadConfigurations()
        {
            displayId = await EvolHelper.GetIdColumnsConfigurations(_svcConfiguration);                        
            if(!displayId)
            {
                ErrorMessage = "Cannot Load Configuration Data";
            }
        } // Load Configuration
            

        private void DisableToolBar()
        {
            foreach (ItemModel tbItem in Toolbaritems)
            {
                if (tbItem.Id.Contains("add") || tbItem.Id.Contains("del"))
                {
                    tbItem.Disabled = true;
                }
            }
        }

        private async Task LoadUserData()
        {
            var authState = await _authState!.GetAuthenticationStateAsync();
            Identity = authState.User;
            userLocationId = int.Parse(Identity.Claims.FirstOrDefault(c => c.Type == "LocationId").Value);
            userName = Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Constants.DefaultUserNameAndEmail;
            //Log.Information($"{userName} went to the Manage Event Volunteers Page");
            
            Toolbaritems.Add(new Syncfusion.Blazor.Navigations.ItemModel() { Text = CaptionAdd, Id = "add", TooltipText = "Add Volunteer to selected Event", PrefixIcon = "e-add" });
            Toolbaritems.Add(new Syncfusion.Blazor.Navigations.ItemModel() { Text = CaptionDelete, Id = "delete", TooltipText = "Delete Volunteer from selected Event", PrefixIcon = "e-delete" });
            //Toolbaritems.Add(new Syncfusion.Blazor.Navigations.ItemModel() { Text = "Print", Id = "print", TooltipText = "Print Grid Data" });
            Toolbaritems.Add(new Syncfusion.Blazor.Navigations.ItemModel() { Text = "PDF Export", Id = "pdf", TooltipText = "Export Grid Data to PDF" });
            Toolbaritems.Add(new Syncfusion.Blazor.Navigations.ItemModel() { Text = "Excel Export", Id = "excel", TooltipText = "Export Grid Data to Excel" });

            if (Identity.IsInRole(RoleNames.NationalAdmin)) // not perfect! for initial testing
            {
                userRole = RoleNames.NationalAdmin;
                isLocationAdmin = false;
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
                if (Identity.IsInRole(RoleNames.LocationScheduler))
                {
                    userRole = RoleNames.LocationScheduler;
                    isLocationAdmin = true;
                }
            } // Get User Data
        } // Load User Data

            private async Task LoadLocations()
            {
                var dataLocations = await _svcLocation!.GetAllAsync();
                if (dataLocations.Success) // 
                {
                    Locations = dataLocations.Data;
                    if (Locations != null && Locations.Count > 0)
                    { // select User Location Name 
                        userLocationName = Locations.Find(e => e.LocationId == userLocationId).Name;
                    } // Locations found            
                }
            } // Load Locations

            private async Task LoadVolunteerData()
            {
                try // get Volunteer List ===========================================================================================
                {
                    Volunteers = await  EvolHelper.GetVolunteers(_svcVolunteer);
                    if (Volunteers.Count > 0)
                    { 
                        if (isLocationAdmin)
                           {
                              Volunteers = Volunteers.FindAll(e => e.LocationId == userLocationId); // Location Filter
                           }
                       lstVolunteerSelector = Volunteers;
                       
                    }
                    else
                    {
                        ErrorMessage = "No Volunteers Data Found";
                    }                   
                }
                catch (Exception ex)
                {
                    ErrorMessage = "Volunteer Data: No DB Files. " + ex.Message;
                }
        } // Load Grid Data
              

        private async Task LoadScheduleData()
        {
            try // get Schedule List ===========================================================================================
            {
                var dataSchedule = await _svcSchedule.GetAllAsync(); // get Schedules
                Schedules = new List<Schedule>();
                if (dataSchedule.Success)                {
                    if (dataSchedule!.Data.Count > 0)
                    {
                        Schedules = dataSchedule!.Data; // retrieve existing media records to temp list
                        Schedules = Schedules.FindAll(e => e.EventStatus == EventStatus.Scheduled && e.EventDateScheduled >= DateTime.Today); // only scheduled events to the future
                        // Location Filter
                        if (isLocationAdmin)
                        {
                            Schedules = Schedules.FindAll(e => e.LocationId == userLocationId);
                        }
                    }
                    else
                    {
                        ErrorMessage = "No Schedule Data Found";
                    } // no rows in Media
                } // the first success
            }
            catch (Exception ex)
            {
                ErrorMessage = "Schedule Data: No DB Files. " + ex.Message;
            }
        } // Load Schedule Data           

        private void PrepareGridData()
        {
            EventVolunteers = EvolHelper.GetGridDataSource(VolunteerEvents, Schedules, Volunteers, Locations);
            return;        
        } // Create Grid Data Source

        private async Task ToolbarClickHandler(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            bool bSelectionStatus = false;
            displayVolunteerData = DisplayNone;
            var strMessageText = String.Empty;

            if (args.Item.Text.ToString().Contains("PDF"))
            {
                await PdfExport();
                return;
            }
            if (args.Item.Text.ToString().Contains("Excel"))
            {
                await ExcelExport();
                return;
            }

            if (selectedGridObject != null)
            {
                if (args.Item.Text.ToString() == CaptionAdd)
                {
                    if (selectedGridObject.EventId > 0) // Existing Schedule/Event ID
                    {
                        bSelectionStatus = true;
                    }
                }
                else // Delete
                {
                    if (selectedGridObject.RegistrationId > 0) // existing Link ID
                    {
                        bSelectionStatus = true;
                        displayVolunteerData = "";
                    }
                }
            } // Grid Row selected

            if (bSelectionStatus)
            {
                await ToolbarActions(args);
                return;
            }
            else
            {
                NoSelectedGridRow(args.Item.Text.ToString(), ref strMessageText);
                
            }

            this.ShowEditDialog = true;

        } // ToolbarClick

        private async Task ToolbarActions(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            newVolunteer = new Volunteer();          
            DisplayAddButton = DisplayNone;
            DisplayDeleteButton = DisplayNone;
            DisplayDataPanel = DisplayNone;
            DisplaySearchPanel= DisplayNone;
           // displayVolunteerData = DisplayNone;
            AvailabilityMessage = (MarkupString)"&nbsp;";
            DialogMessage = (MarkupString)"&nbsp;";           
            var strMessageText = String.Empty;
            ErrorMessage = string.Empty;                 

            // if row selection & all id found
            DisplayDataPanel = "";
                switch (args.Item.Text.ToString())
                {
                    case CaptionAdd:
                        PrepareVolunteerAddDialog();
                        break;                       

                    case CaptionDelete:                                        
                        newVolunteer = EvolHelper.PrepareVolunteerDeleteDialog(selectedGridObject, ref strMessageText, ref DialogTitle, ref DisplayDeleteButton, ref CloseButtonCaption);
                        break;
                }
            
                if (strMessageText.Length > 0)
                {
                    DialogMessage = BootstrapHelper.GetBootstrapMessage("help", strMessageText, "", false);
                }           
                this.ShowEditDialog = true;             

        } // Tool Bar Clicks

        private void NoSelectedGridRow(string strAction, ref string strMessageText)
        {
            // no selected object                
            DialogTitle = strAction + " Volunteer Requested";
            if (strAction == CaptionAdd)
            {
                DialogMessage = BootstrapHelper.GetBootstrapMessage(CaptionWarning, "No selected Event!", "", false);
            }
            else // delete action
            {
                if (selectedGridObject.EventId > 0 && selectedGridObject.VolunteerId == 0)
                {
                    strMessageText = "No Volunteer to delete.<br />Go to Manage Schedules to delete events";
                    DialogMessage = BootstrapHelper.GetBootstrapMessage(CaptionWarning, strMessageText, "", false);
                }
                else
                {
                    DialogMessage = BootstrapHelper.GetBootstrapMessage(CaptionWarning, "No selected Volunteer!", "", false);
                }
            }
            CloseButtonCaption = CaptionClose;

        } // No selected grid row

        private void PrepareVolunteerAddDialog()
        {
            DialogTitle = "Select Volunteer & Add to Event";
            // create a list to select Volunteer: Volunteers of current Location and not linked to selected event
            // Location Volunteers

            lstLocationVolunteers = EvolHelper.GetLocationVolunteersSelector(selectedGridObject, lstVolunteerSelector, VolunteerEvents);
            //strJson = JsonConvert.SerializeObject(lstLocationVolunteers, Formatting.Indented);
            //strHtml = "<pre>" + strJson + "</pre>";



            if (lstLocationVolunteers.Count > 0)
            {
                DisplaySearchPanel = "";
                DisplayAddButton = DisplayNone; //  OK button
                CloseButtonCaption = "Cancel";
                AvailabilityMessage = BootstrapHelper.GetBootstrapMessage("info", lstLocationVolunteers.Count.ToString() + " Volunteer(s) available", "", false, "compact");
            }
            else
            {
                DisplayDataPanel = DisplayNone;
                DialogMessage = BootstrapHelper.GetBootstrapMessage(CaptionWarning, "No available Volunteers for this Location & Event", "", false);
                //this.ShowEditDialog = true;
                //return;
            }
        } // add dialog    
            

        private async Task onConfirmAdd()
        {  // Action Finished
            AvailabilityMessage = (MarkupString)"&nbsp;";
            var strMessageText = string.Empty;
            var actionStatus = "error";
            if (selectedGridObject != null && newVolunteer.VolunteerId>0)
            {
                var newRegistration = new VolunteerEvent();
                newRegistration.VolunteerId = newVolunteer.VolunteerId;
                newRegistration.ScheduleId = selectedGridObject.EventId;
                newRegistration.LocationId = selectedGridObject.EventLocationId;
                var addResult = await _svcVolunteerEvents.CreateAsync(newRegistration);
                if (addResult.Success)
                {
                    // add Volunteer to Schedule table
                    
                    var bUpdateSchedule = await EvolHelper.UpdateSchedule(_svcSchedule, Schedules, CaptionAdd, newRegistration.ScheduleId, newVolunteer.VehicleType);
                    if (bUpdateSchedule)
                    {
                        actionStatus = "success";
                        strMessageText = "Add Successful!";
                    }
                    else
                    {
                        actionStatus = "warning";
                        strMessageText = "Selected volunteer was added to Event, but Schedules cannot be updated!";
                    }
                    CloseButtonCaption = CaptionClose;                  
                    await RefreshGrid(CaptionAdd);
                }
                else
                {
                    strMessageText = "Unable to Add Volunteer.";
                }
            }
            else
            {
                strMessageText = "Unable to Add Volunteer.";
            }
            DisplayAddButton = DisplayNone;
            DisplayDataPanel = DisplayNone;
            DisplaySearchPanel = DisplayNone;
            DialogMessage = BootstrapHelper.GetBootstrapMessage(actionStatus, strMessageText, "");
        } // add confiormation         

        private async Task onConfirmDelete()
        {  // Action Finished
            //ErrorMessage = "Delete Confirmed";
            var strMessageText = string.Empty;
            var actionStatus = "error";
            if (selectedGridObject != null)
            {
                int RegistrationId = selectedGridObject.RegistrationId;
                if (RegistrationId > 0)
                {
                    var deleteResult = await _svcVolunteerEvents.DeleteAsync(RegistrationId);                    
                    if (deleteResult.Success)
                    {  // add Volunteer to Schedule table
                        var bUpdateSchedule = await EvolHelper.UpdateSchedule(_svcSchedule, Schedules, "Del", selectedGridObject.EventId, selectedGridObject.VehicleType);
                        if (bUpdateSchedule)
                        {
                            actionStatus = "success";
                            strMessageText = "Deletion Successful!";
                        }
                        else
                        {
                            actionStatus = "warning";
                            strMessageText = "Deletion Successful, but Schedules cannot be updated!";

                        }
                        CloseButtonCaption = CaptionClose;                      
                        await RefreshGrid(CaptionDelete);
                    }
                    else
                    {                       
                        strMessageText = "Unable to Delete Volunteer.";                       
                    }
                }
            }
            else
            {
                strMessageText = "Unable to Delete Volunteer.";
            }
            DisplayDeleteButton = DisplayNone;
            DisplayDataPanel = DisplayNone;
            DisplaySearchPanel = DisplayNone;
            DialogMessage= BootstrapHelper.GetBootstrapMessage(actionStatus, strMessageText, "");       
         } // del confirmation

        public async Task RefreshGrid(string strAction)
        {   // update Volunteer Events first        
            VolunteerEvents = await EvolHelper.GetVolunteerEvents(_svcVolunteerEvents, isLocationAdmin, userLocationId);
            //strJson = JsonConvert.SerializeObject(VolunteerEvents, Formatting.Indented);
            //strHtml = "<pre>" + strJson + "</pre>";
            PrepareGridData();
            Grid.CallStateHasChanged();
            Grid.Refresh();
        }// Refresh Gruid

        public void OnVolunteerSelect(Syncfusion.Blazor.DropDowns.ChangeEventArgs<string, Volunteer> args)
        {
            DialogMessage = (MarkupString)"&nbps";
            DisplayAddButton = DisplayNone;
            displayVolunteerData = DisplayNone;
            newVolunteer = new Volunteer(); // empty data panel
            try
            {
                if (args.ItemData != null)
                {
                    displayVolunteerData = "";
                    newVolunteer = (Volunteer)args.ItemData;
                    if (newVolunteer != null)
                    {
                        newVolunteer.VolunteerId=args.ItemData.VolunteerId;
                        if (newVolunteer.VolunteerId > 0)
                        {
                            DisplayAddButton = ""; //  OK button
                            var strMessageText = "New selected Volunteer will be added to selected Event. Are you sure?";
                            DialogMessage = BootstrapHelper.GetBootstrapMessage("help", strMessageText, "",false);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
              // ErrorMessage = ex.ToString();
            }
        } // Volunteer Selected

        public async Task GetSelectedRecords(RowSelectEventArgs<Volunteer> args)
        {
            DisableToolBar();
            selectedGridObject = args.Data;
            // Enable Add - in all situations
            var AddItem = Toolbaritems.FirstOrDefault(tb=>tb.Text == CaptionAdd);
            AddItem.Disabled = false;
            //Enable Delete - if row contains Volunteer Data
            if (selectedGridObject.RegistrationId > 0)
            {
                var DelItem = Toolbaritems.FirstOrDefault(tb => tb.Text == "Delete");
                DelItem.Disabled = false;
            }          
        } // get selected record

        private async Task OnFilterChange(ChangeEventArgs<string, GridFilterOption> args)
        {   // External Grid Filtering by Event Date
            //Debug.WriteLine("The Grid Filter DropDownList Value", args.Value);
            switch (args.Value)
            {
                case "reg":
                    await Grid.FilterByColumnAsync(RegisterColumn, "greaterthan", 0);
                    break;
                case "notreg":
                    await Grid.FilterByColumnAsync(RegisterColumn, "equal", 0);
                    break;
                default: // all
                    await Grid.ClearFilteringAsync(RegisterColumn);
                    break;
            }
            DisableToolBar();
        } // Filter Changed

        protected async Task PdfExport()
        {
            PdfExportProperties ExportProperties = new PdfExportProperties
            {
                FileName = "EventVolunteers_" + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.ExportToPdfAsync(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "EventVolunteers" + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExportToExcelAsync(ExportProperties);
        }

        private string cssClass { get; set; } = "e-outline";
        protected Dictionary<string, object> DescriptionHtmlAttribute { get; set; } = new Dictionary<string, object>()
        {
            { "rows", "5" },
        };

        public class GridFilterOption
        {
            public string ID { get; set; }
            public string Text { get; set; }
        }

        public string DefaultFilter = "all";

        List<GridFilterOption> GridDefaultFilter = new List<GridFilterOption> {
                new GridFilterOption() { ID= "reg", Text= "Events with Registered Volunteers" },
                new GridFilterOption() { ID= "notreg", Text= "Events without Registered Volunteers" },
                new GridFilterOption() { ID= "all", Text= "All Events" }, };

    } // EvolGrid Class
} // namespace
