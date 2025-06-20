using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Navigations;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Serilog;

namespace BedBrigade.Client.Components;

public partial class SignUpGrid : ComponentBase
{
    // data services

    [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }
    [Inject] private IVolunteerDataService? _svcVolunteer { get; set; }
    [Inject] private IUserDataService? _svcUser { get; set; }
    [Inject] private ILocationDataService? _svcLocation { get; set; }
    [Inject] private IAuthService? _svcAuth { get; set; }
    [Inject] private IScheduleDataService? _svcSchedule { get; set; }
    [Inject] private ISignUpDataService? _svcSignUp { get; set; }
    [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
    [Inject] private ILanguageContainerService _lc { get; set; }
    [Inject] private ToastService _toastService { get; set; }

    // object lists

    protected List<Volunteer>? Volunteers { get; set; }
    protected List<Volunteer>? EventVolunteers { get; set; }
    protected List<Volunteer>? lstVolunteerSelector { get; set; }
    protected List<Volunteer>? lstLocationVolunteers { get; set; } = new List<Volunteer>();
    protected List<Location>? Locations { get; set; }
    protected List<Schedule>? Schedules { get; set; }
    protected List<SignUp>? SignUps { get; set; }

    protected List<Syncfusion.Blazor.Navigations.ItemModel> Toolbaritems =
        new List<Syncfusion.Blazor.Navigations.ItemModel>();

    protected List<VehicleTypeEnumItem>? lstVehicleTypes { get; set; }


    // identity variables

    private string userName = String.Empty;
    private string userRole = String.Empty;
    private string userLocationName = String.Empty;
    private int userLocationId = 0;

    // variables

    protected string? RecordText { get; set; } = "Loading Schedules ...";
    public bool DataStatus = true;

    // Grid regerences

    protected SfGrid<Volunteer>? Grid { get; set; }


    // Constants
    private const string DisplayNone = "none";
    private const string CaptionClose = "Close";
    private const string CaptionWarning = "warning";
    private const string CaptionAdd = "Add";
    private const string CaptionDelete = "Delete";
    private const string RegisterColumn = "SignUpId";

    // Action & Dialog variables

    public bool ShowEditDialog { get; set; } = false;
    private string ErrorMessage = String.Empty;
    private string DisplayAddButton = DisplayNone;
    private string DisplayDeleteButton = DisplayNone;
    private string DisplayDataPanel = DisplayNone;
    private string DisplaySearchPanel = DisplayNone;
    private string GridDisplay = String.Empty;

    private MarkupString DialogMessage;
    private MarkupString AvailabilityMessage; // Volunteer Availability
    private string DialogTitle = string.Empty;
    private string CloseButtonCaption = CaptionClose;

    public Volunteer? selectedGridObject { get; set; }
    public Volunteer? newVolunteer { get; set; }
    
    public bool displayId = false;
    public string displayVolunteerData = DisplayNone;

    // test variables

    public string strHtml = string.Empty;
    private string testString = string.Empty;
    private List<string> lstEmptyTables = new List<string>();

    protected override async Task OnInitializedAsync()
    {
        try
        {

            _lc.InitLocalizedComponent(this);
            lstEmptyTables = await SignUpHelper.GetSignUpDataStatusAsync(_svcSchedule, _svcVolunteer);
            if (lstEmptyTables.Count > 0)
            {
                DataStatus = false;
                GridDisplay = DisplayNone;
                _ = Task.CompletedTask;
                return;
            }

            if (DataStatus)
            {
                await LoadGridData();
            }

        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing SignUpGrid component");
            _toastService.Error("Error", "An error occurred while loading the Sign Up Grid data.");
        }

    } // Async Init


    /// <summary>
    /// On loading of the Grid get the user grid persisted data
    /// </summary>
    /// <returns></returns>
    protected async Task OnLoad()
    {
        string userName = _svcUser.GetUserName();
        UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Evol };
        var result = await _svcUserPersist.GetGridPersistence(persist);
        if (result.Success && result.Data != null)
        {
            await Grid.SetPersistDataAsync(result.Data);
        }
    }

    /// <summary>
    /// On destroying of the grid save its current state
    /// </summary>
    /// <returns></returns>
    protected async Task OnDestroyed()
    {
        await SaveGridPersistence();
    }

    private async Task SaveGridPersistence()
    {
        string state = await Grid.GetPersistData();
        string userName = _svcUser.GetUserName();
        UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.Evol, Data = state };
        var result = await _svcUserPersist.SaveGridPersistence(persist);
        if (!result.Success)
        {
            Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.Evol} : {result.Message}");
        }
    }

    private async Task LoadGridData()
    {
        await LoadConfiguration();
        await LoadUserData();
        await LoadLocations();
        await LoadVolunteerData();
        Schedules = await SignUpHelper.GetSchedules(_svcSchedule, IsLocationAdmin, userLocationId);
        SignUps = await SignUpHelper.GetSignUps(_svcSignUp, IsLocationAdmin, userLocationId);
        lstVehicleTypes = EnumHelper.GetVehicleTypeItems();
        DisableToolBar();
        PrepareGridData();
    }

    private async Task LoadConfiguration()
    {
        displayId = await _svcConfiguration.GetConfigValueAsBoolAsync(ConfigSection.System,
            ConfigNames.DisplayIdFields);
        RecordText = await _svcConfiguration.GetConfigValueAsync(ConfigSection.System, ConfigNames.EmptyGridText);
    }


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
        userLocationId =  _svcUser.GetUserLocationId();
        userName = _svcUser.GetUserName();
        userRole = _svcUser.GetUserRole();
        Log.Information($"{userName} went to the Manage Sign Up Page");

        Toolbaritems.Add(new Syncfusion.Blazor.Navigations.ItemModel()
        {
            Text = CaptionAdd, Id = "add", TooltipText = "Add Volunteer to selected Event", PrefixIcon = "e-add"
        });
        Toolbaritems.Add(new Syncfusion.Blazor.Navigations.ItemModel()
        {
            Text = CaptionDelete, Id = "delete", TooltipText = "Delete Volunteer from selected Event",
            PrefixIcon = "e-delete"
        });
        Toolbaritems.Add(new Syncfusion.Blazor.Navigations.ItemModel()
            { Text = "PDF Export", Id = "pdf", TooltipText = "Export Grid Data to PDF" });
        Toolbaritems.Add(new Syncfusion.Blazor.Navigations.ItemModel()
            { Text = "Excel Export", Id = "excel", TooltipText = "Export Grid Data to Excel" });

    } // Load User Data


    private bool IsLocationAdmin
    {
        get
        {
            return _svcAuth.UserHasRole(RoleNames.CanManageSchedule);
        }
    }

    private async Task LoadLocations()
    {
        var dataLocations = await _svcLocation!.GetAllAsync();
        if (dataLocations.Success && dataLocations.Data != null) // 
        {
            Locations = dataLocations.Data;
            userLocationName = Locations.FirstOrDefault(e => e.LocationId == userLocationId)?.Name;
        }
        else
        {
            Log.Error($"SignUpGrid, Error loading locations: {dataLocations.Message}");
            _toastService.Error("Error", dataLocations.Message);
            ErrorMessage = "Unable to load Locations. " + dataLocations.Message;
        }
    } 

    private async Task LoadVolunteerData()
    {
        try // get Volunteer List ===========================================================================================
        {
            Volunteers = await SignUpHelper.GetVolunteers(_svcVolunteer);
            if (Volunteers.Count > 0)
            {
                if (IsLocationAdmin)
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
            Log.Error(ex, "Error loading Volunteers data");
            ErrorMessage = "Volunteer Data: No DB Files. " + ex.Message;
        }
    } // Load Volunteer Data            

    private void PrepareGridData()
    {
        EventVolunteers = SignUpHelper.GetGridDataSource(SignUps, Schedules, Volunteers, Locations);
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

        NoSelectedGridRow(args.Item.Text.ToString(), ref strMessageText);

        this.ShowEditDialog = true;

    } // ToolbarClick

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
        switch (args.Item.Text.ToString())
        {
            case CaptionAdd:
                PrepareVolunteerAddDialog();
                break;

            case CaptionDelete:
                newVolunteer = SignUpHelper.PrepareVolunteerDeleteDialog(selectedGridObject, ref strMessageText,
                    ref DialogTitle, ref DisplayDeleteButton, ref CloseButtonCaption);
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
                DialogMessage =
                    BootstrapHelper.GetBootstrapMessage(CaptionWarning, "No selected Volunteer!", "", false);
            }
        }

        CloseButtonCaption = CaptionClose;

    } // No selected grid row

    private void PrepareVolunteerAddDialog()
    {
        DialogTitle = "Select Volunteer & Add to Event";
        // create a list to select Volunteer: Volunteers of current Location and not linked to selected event
        // Location Volunteers

        lstLocationVolunteers =
            SignUpHelper.GetLocationVolunteersSelector(selectedGridObject, lstVolunteerSelector, SignUps);
        //strJson = JsonConvert.SerializeObject(lstLocationVolunteers, Formatting.Indented);
        //strHtml = "<pre>" + strJson + "</pre>";



        if (lstLocationVolunteers.Count > 0)
        {
            DisplaySearchPanel = "";
            DisplayAddButton = DisplayNone; //  OK button
            CloseButtonCaption = "Cancel";
            AvailabilityMessage = BootstrapHelper.GetBootstrapMessage("info",
                lstLocationVolunteers.Count.ToString() + " Volunteer(s) available", "", false, "compact");
        }
        else
        {
            DisplayDataPanel = DisplayNone;
            DialogMessage = BootstrapHelper.GetBootstrapMessage(CaptionWarning,
                "No available Volunteers for this Location & Event", "", false);
            //this.ShowEditDialog = true;
            //return;
        }
    } // add dialog    


    private async Task onConfirmAdd()
    {
        // Action Finished
        AvailabilityMessage = (MarkupString)"&nbsp;";
        var strMessageText = string.Empty;
        var actionStatus = "error";
        if (selectedGridObject != null && newVolunteer.VolunteerId > 0)
        {
            var newRegistration = new SignUp();
            newRegistration.VolunteerId = newVolunteer.VolunteerId;
            newRegistration.ScheduleId = selectedGridObject.EventId;
            newRegistration.LocationId = selectedGridObject.EventLocationId;
            var addResult = await _svcSignUp.CreateAsync(newRegistration);
            if (addResult.Success)
            {
                // add Volunteer to Schedule table

                var bUpdateSchedule = await SignUpHelper.UpdateSchedule(_svcSchedule, Schedules, CaptionAdd,
                    newRegistration.ScheduleId, newVolunteer.VehicleType);
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
    {
        // Action Finished
        //ErrorMessage = "Delete Confirmed";
        var strMessageText = string.Empty;
        var actionStatus = "error";
        if (selectedGridObject != null)
        {
            int RegistrationId = selectedGridObject.RegistrationId;
            if (RegistrationId > 0)
            {
                var deleteResult = await _svcSignUp.DeleteAsync(RegistrationId);
                if (deleteResult.Success)
                {
                    // add Volunteer to Schedule table
                    var bUpdateSchedule = await SignUpHelper.UpdateSchedule(_svcSchedule, Schedules, "Del",
                        selectedGridObject.EventId, selectedGridObject.VehicleType);
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
        DialogMessage = BootstrapHelper.GetBootstrapMessage(actionStatus, strMessageText, "");
    } // del confirmation

    public async Task RefreshGrid(string strAction)
    {
        // update Sign-Ups first        
        SignUps = await SignUpHelper.GetSignUps(_svcSignUp, IsLocationAdmin, userLocationId);
        PrepareGridData();
        Grid.CallStateHasChanged();
        Grid.Refresh();
    } // Refresh Gruid

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
            //TODO:  Not sure why the exception is eaten
            // ErrorMessage = ex.ToString();
        }
    } // Volunteer Selected

    public async Task GetSelectedRecords(RowSelectEventArgs<Volunteer> args)
    {
        DisableToolBar();
        selectedGridObject = args.Data;
        // Enable Add - in all situations
        var AddItem = Toolbaritems.FirstOrDefault(tb => tb.Text == CaptionAdd);
        AddItem.Disabled = false;
        //Enable Delete - if row contains Volunteer Data
        if (selectedGridObject.RegistrationId > 0)
        {
            var DelItem = Toolbaritems.FirstOrDefault(tb => tb.Text == "Delete");
            DelItem.Disabled = false;
        }
    } // get selected record

    private async Task OnFilterChange(ChangeEventArgs<string, GridFilterOption> args)
    {
        // External Grid Filtering by Event Date
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


    public class GridFilterOption
    {
        public string ID { get; set; }
        public string Text { get; set; }
    }

    public string DefaultFilter = "all";

    List<GridFilterOption> GridDefaultFilter = new List<GridFilterOption>
    {
        new GridFilterOption() { ID = "reg", Text = "Events with Registered Volunteers" },
        new GridFilterOption() { ID = "notreg", Text = "Events without Registered Volunteers" },
        new GridFilterOption() { ID = "all", Text = "All Events" },
    };

} 

