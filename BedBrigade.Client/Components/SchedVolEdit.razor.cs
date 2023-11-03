using BedBrigade.Common;
using BedBrigade.Data.Data;
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.Grids;
using static BedBrigade.Common.Common;
using Newtonsoft.Json;
using Syncfusion.Blazor.Navigations;
using System.Diagnostics.Eventing.Reader;
using BedBrigade.Client.Pages.Administration.Manage;
using Syncfusion.Blazor.Sparkline.Internal;

namespace BedBrigade.Client.Components
{
   public partial class SchedVolEdit
   {
        [Parameter]
        public string? Title { get; set; }    
        [Parameter]
        public string? ResultType { get; set; } 
        [Parameter]
        public EventCallback<int> ParentMethod { get; set; }
        [Parameter]
        public Schedule? SelectedEvent { get; set; }
        [Parameter]
        public bool displayId { get; set; }
        [Parameter]
        public string? DisplayParentHeader { get; set; }
        [Parameter]
       
        // data services

        [Inject] public IVolunteerDataService? _svcVolunteer { get; set; }
        [Inject] public ILocationDataService? _svcLocation { get; set; }
        [Inject] private IVolunteerEventsDataService? _svcVolunteerEvents { get; set; }
        [Inject] private IScheduleDataService? _svcSchedule { get; set; }
        protected List<Volunteer>? Volunteers { get; set; }
        protected List<Volunteer>? EventVolunteers { get; set; }
        protected List<Volunteer>? lstVolunteerSelector { get; set; }       
        public Schedule? CurrentSchedule { get; set; } = new Schedule();
        protected List<VolunteerEvent>? RegisteredVolunteers { get; set; }
        protected List<ItemModel> Toolbaritems = new List<ItemModel>();
        protected List<VehicleTypeEnumItem>? lstVehicleTypes { get; set; }


        public Schedule? UpdatedEvent { get; set; }
        public Volunteer? newVolunteer { get; set; }
        public int foundVolunteerId = 0;
        public Volunteer selectedGridObject = new Volunteer();

        // Constants
        private const string DisplayNone = "none";
        private const string CaptionClose = "Close";
        private const string CaptionWarning = "warning";
        private const string CaptionAdd = "Add";
        private const string CaptionDelete = "Delete";
        private const string RegisterColumn = "RegistrationId";

        // Modal dialog variables

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
        private string EventLocationName = String.Empty;
        public string displayVolunteerData = DisplayNone;

        // test variables

        public string strJson = string.Empty;
        public string strHtml = string.Empty;
        private string testString = string.Empty;       
        public string strName = String.Empty;


        SfGrid<Volunteer> Grid;
       
        public int GridRowHeight = 20;

        async Task CallParentMethod()
        {
            try
            {
                await ParentMethod.InvokeAsync(SelectedEvent.ScheduleId);
            }
            catch (Exception ex)
            {

            }
        }


        protected override async Task OnInitializedAsync()
        {
            ErrorMessage = String.Empty;
            EventVolunteers = new List<Volunteer>();
            try
            {              
               
                await GetEventVolunteers();
                await GetLocationVolunteers();
                lstVehicleTypes = GetVehicleTypeItems();
                GetGridDataSource();

                if (SelectedEvent != null && SelectedEvent.EventStatus == EventStatus.Scheduled)
                {
                    SetToolBar();
                }

                 EventLocationName = (await _svcLocation.GetByIdAsync(SelectedEvent.LocationId)).Data.Name;

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
         
        } // Init
           
             
        public async Task GetEventVolunteers()
        {
            // get Volunteers of Selected Event
            var dataEvents = await _svcVolunteerEvents.GetAllAsync();
            if (dataEvents.Success) // 
            {
               RegisteredVolunteers = dataEvents.Data.ToList();
                if (RegisteredVolunteers != null && RegisteredVolunteers.Count > 0)
                { // current Event Volunteers
                    if (SelectedEvent != null)
                    {
                        RegisteredVolunteers = RegisteredVolunteers.FindAll(e => e.ScheduleId == SelectedEvent.ScheduleId);
                    }

                }

                //strJson = JsonConvert.SerializeObject(VolunteerEvents, Formatting.Indented);
                //strHtml = "<pre>" + strJson + "</pre>";

            }
        } // Get Event Volunteers

        public async Task GetLocationVolunteers()
        {
            var dataVolunteer = await _svcVolunteer.GetAllAsync();           
            if (dataVolunteer.Success && dataVolunteer != null)
            {
                if (dataVolunteer.Data.Count > 0)
                {
                    Volunteers = dataVolunteer.Data.ToList(); // All Volunteers
                    { // select Location Volunteers
                        if (SelectedEvent != null)
                        {
                            Volunteers = Volunteers.FindAll(e => e.LocationId == SelectedEvent.LocationId);
                        }
                        // Location Volunteers, not link to current Event
                       lstVolunteerSelector = (
                       (from v in Volunteers // location volunteers                                                                                                 
                        where !(from lv in RegisteredVolunteers
                                select lv.VolunteerId).Contains(v.VolunteerId)
                        select new Volunteer
                        {
                            VolunteerId = v.VolunteerId,
                            FirstName = v.FirstName,
                            LastName = v.LastName,
                            Email = v.Email,
                            Phone = v.Phone,
                            IHaveVolunteeredBefore = v.IHaveVolunteeredBefore,
                            VehicleType = v.VehicleType
                        }
                        ).OrderBy(loc => loc.SearchName).ToList()
                       );                    

                    }
                }
            }
        } // Get Location Volunteers

        public void GetGridDataSource()
        {
            if ((RegisteredVolunteers != null && RegisteredVolunteers.Count > 0) && (Volunteers != null && Volunteers.Count > 0))
            {
                EventVolunteers = (from ve in RegisteredVolunteers
                                   join v in Volunteers on ve.VolunteerId equals v.VolunteerId
                                   select new Volunteer
                                   {
                                       RegistrationId = ve.RegistrationId,
                                       VolunteerId = ve.VolunteerId,
                                       // Volunteer Fields
                                       IHaveVolunteeredBefore = v.IHaveVolunteeredBefore,
                                       FirstName = v.FirstName,
                                       LastName = v.LastName,
                                       Phone = v.Phone,
                                       Email = v.Email,
                                       OrganizationOrGroup = v.OrganizationOrGroup,
                                       Message = v.Message,
                                       VehicleType = v.VehicleType,
                                       CreateDate = ve.CreateDate
                                   }
                              ).ToList();
            }
        }// Grid Data Source

        private void SetToolBar()
        {
            Toolbaritems.Add(new ItemModel() { Text = "Add Volunteer", Id = "add", TooltipText = "Add Volunteer to Event", PrefixIcon = "e-add" });
            Toolbaritems.Add(new ItemModel() { Text = "Delete Volunteer", Id = "del", TooltipText = "Delete Volunteer from Event", PrefixIcon = "e-delete" });

            foreach (ItemModel tbItem in Toolbaritems)
            {
                if (tbItem.Id.Contains("add") || tbItem.Id.Contains("del"))
                {
                    if (lstVolunteerSelector.Count == 0)
                    {
                        tbItem.Disabled = true; // no Volunteers
                    }               
                                        
                }

                if (tbItem.Id.Contains("del"))
                {
                   tbItem.Disabled = true; // disables until volunteer selected
                }

            }
        } // Set Tool Bar

        private async Task ToolbarClickHandler(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            bool bSelectionStatus = false;
            displayVolunteerData = DisplayNone;
            var strMessageText = String.Empty;

            if (selectedGridObject != null)
            {
                if (args.Item.Id == "add") // Add Volunteer
                {
                    bSelectionStatus = true;
                }
                else // Delete Volunteer
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
               // NoSelectedGridRow(args.Item.Text.ToString(), ref strMessageText);

            }

            this.ShowEditDialog = true;
        } // ToolBar

        private async Task ToolbarActions(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            newVolunteer = new Volunteer();
            DisplayAddButton = DisplayNone;
            DisplayDeleteButton = DisplayNone;
            DisplayDataPanel = DisplayNone;
            DisplaySearchPanel = DisplayNone;
            // displayVolunteerData = DisplayNone;
            AvailabilityMessage = (MarkupString)"&nbsp;";
            DialogMessage = (MarkupString)"&nbsp;";
            var strMessageText = String.Empty;
            ErrorMessage = string.Empty;

            // if row selection & all id found
            DisplayDataPanel = "";
            switch (args.Item.Id.ToString())
            {
                case "add":
                    PrepareVolunteerAddDialog();
                    break;

                case "del":
                    newVolunteer = EvolHelper.PrepareVolunteerDeleteDialog(selectedGridObject, ref strMessageText, ref DialogTitle, ref DisplayDeleteButton, ref CloseButtonCaption);
                    break;
            }

            if (strMessageText.Length > 0)
            {
                DialogMessage = BootstrapHelper.GetBootstrapMessage("help", strMessageText, "", false);
            }
            this.ShowEditDialog = true;

        } // Tool Bar Clicks

        private void PrepareVolunteerAddDialog()
        {
            DialogTitle = "Select Volunteer & Add to Event";
            // create a list to select Volunteer: Volunteers of current Location and not linked to selected event
            // Location Volunteers

            //lstVolunteers = EvolHelper.GetLocationVolunteersSelector(selectedGridObject, lstVolunteerSelector, VolunteerEvents);
            //strJson = JsonConvert.SerializeObject(lstLocationVolunteers, Formatting.Indented);
            //strHtml = "<pre>" + strJson + "</pre>";

            if (lstVolunteerSelector.Count > 0)
            {
                DisplaySearchPanel = "";
                DisplayAddButton = DisplayNone; //  OK button
                CloseButtonCaption = "Cancel";
                AvailabilityMessage = BootstrapHelper.GetBootstrapMessage("info", lstVolunteerSelector.Count.ToString() + " Volunteer(s) available", "", false, "compact");
            }
            else
            {
                DisplayDataPanel = DisplayNone;
                DialogMessage = BootstrapHelper.GetBootstrapMessage(CaptionWarning, "No available Volunteers for this Location & Event", "", false);
                //this.ShowEditDialog = true;
                //return;
            }
        } // add dialog    


        public void GetSelectedVolunteer(RowSelectEventArgs<Volunteer> args)
        {
           // DisableToolBar();
            selectedGridObject = args.Data; // selected Volunteer       
            //Enable Delete - if row contains Volunteer Data
            if (selectedGridObject.RegistrationId > 0)
            {
                var DelItem = Toolbaritems.FirstOrDefault(tb => tb.Id == "del");
                DelItem.Disabled = false;
            }
        } // get selected record

        public void OnVolunteerSelect(Syncfusion.Blazor.DropDowns.ChangeEventArgs<string, Volunteer> args)
        {  // Pop up selector
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
                        newVolunteer.VolunteerId = args.ItemData.VolunteerId;
                        if (newVolunteer.VolunteerId > 0)
                        {
                            DisplayAddButton = ""; //  OK button
                            var strMessageText = "New selected Volunteer will be added to selected Event. Are you sure?";
                            DialogMessage = BootstrapHelper.GetBootstrapMessage("help", strMessageText, "", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // ErrorMessage = ex.ToString();
            }
        } // Volunteer Selected

        private async Task onConfirmAdd()
        {  // Action Finished
            AvailabilityMessage = (MarkupString)"&nbsp;";
            var strMessageText = string.Empty;
            var actionStatus = "error";
            if (selectedGridObject != null && newVolunteer.VolunteerId > 0)
            {
                var newRegistration = new VolunteerEvent();
                newRegistration.VolunteerId = newVolunteer.VolunteerId;
                newRegistration.ScheduleId = SelectedEvent.ScheduleId;
                newRegistration.LocationId = SelectedEvent.LocationId;
                var addResult = await _svcVolunteerEvents.CreateAsync(newRegistration);
                if (addResult.Success)
                {
                    // add Volunteer to Schedule table

                     var bUpdateSchedule = await UpdateCurrentSchedule(SelectedEvent, "Add", newVolunteer.VehicleType);
                    if (bUpdateSchedule)
                    {
                        actionStatus = "success";
                        strMessageText = "Add Successful!";
                        await CallParentMethod();
                    }
                    else
                    {
                        actionStatus = "warning";
                        strMessageText = "Selected volunteer was added to Event, but Schedules cannot be updated!";
                    }
                    CloseButtonCaption = CaptionClose;
                    await RefreshGrid();
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
                    {  //Delete Volunteer from  Schedule table                      
                        var bUpdateSchedule = await UpdateCurrentSchedule(SelectedEvent, "Del", selectedGridObject.VehicleType);
                        if (bUpdateSchedule)
                        {
                            actionStatus = "success";
                            strMessageText = "Deletion Successful!";
                            await CallParentMethod();
                        }
                        else
                        {
                            actionStatus = "warning";
                            strMessageText = "Deletion Successful, but Schedules cannot be updated!";

                        }
                        CloseButtonCaption = CaptionClose;
                        await RefreshGrid();
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
            DialogMessage = BootstrapHelper.GetBootstrapMessage(actionStatus, strMessageText, "");
        } // del confirmation

        public async Task RefreshGrid()
        {
            await GetEventVolunteers();
            await GetLocationVolunteers();
            GetGridDataSource();
            //Grid.CallStateHasChanged();
            await Grid.Refresh();
        }// Refresh Gruid


        private async Task<bool> UpdateCurrentSchedule(Schedule mySchedule, string strAction, VehicleType CarType)
        {
            var bScheduleUpdated = false;
            var bUpdate = true;
            if (mySchedule != null) // update 
            {
                switch (strAction)
                {
                    case "Add":                                               
                            mySchedule.VolunteersRegistered++;
                            if (CarType != VehicleType.NoCar)
                            {
                                mySchedule.VehiclesDeliveryRegistered++;
                            }                        
                        break;
                    case "Del":
                            if (mySchedule.VolunteersRegistered > 0)
                            {
                                mySchedule.VolunteersRegistered--;
                            }                        
                            if (CarType != VehicleType.NoCar && mySchedule.VehiclesDeliveryRegistered > 0)
                            {
                                mySchedule.VehiclesDeliveryRegistered--;
                            }                        
                        break;
                    default:
                        bUpdate = false;
                        // Do Nothing
                        break;
                } // switch action
            }
            // update Schedule Table Record                 
            if (bUpdate && mySchedule != null && _svcSchedule != null)
            {
                var dataUpdate = await _svcSchedule.UpdateAsync(mySchedule);

                if (dataUpdate.Success)
                {
                    bScheduleUpdated = true;
                }
            }
            return (bScheduleUpdated);
        } // update Schedule

    
    } // Class
} // namespace

 
