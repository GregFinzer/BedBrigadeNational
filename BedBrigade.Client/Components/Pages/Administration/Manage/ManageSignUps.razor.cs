using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Navigations;

namespace BedBrigade.Client.Components.Pages.Administration.Manage;

public partial class ManageSignUps : ComponentBase
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
    [Inject] private ToastService _toastService { get; set; }
    [Inject] private ITimezoneDataService _svcTimezone { get; set; }
    [Inject] private ISendSmsLogic _sendSmsLogic { get; set; }
    [Inject] private IEmailBuilderService _svcEmailBuilder { get; set; }

    protected List<SignUpDisplayItem>? SignUpDisplayItems { get; set; }
    protected List<Volunteer>? lstVolunteerSelector { get; set; }
    protected List<Volunteer>? lstLocationVolunteers { get; set; } = new List<Volunteer>();
    protected List<Location>? Locations { get; set; }
    //protected List<Common.Models.Schedule>? Schedules { get; set; }

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

    // Grid references

    protected SfGrid<SignUpDisplayItem>? Grid { get; set; }


    // Constants
    private const string DisplayNone = "none";
    private const string CaptionClose = "Close";
    private const string CaptionWarning = "warning";
    private const string CaptionAdd = "Add";
    private const string CaptionDelete = "Delete";
    private const string RegisterColumn = "SignUpId";
    private const string Reset = "Reset";
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

    public SignUpDisplayItem? selectedGridObject { get; set; }
    public SignUpDisplayItem? newSignUp { get; set; }

    public string displayVolunteerData = DisplayNone;

    // test variables

    public string strHtml = string.Empty;

    public string ManageSignUpsMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadGridData();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing SignUpGrid component");
            _toastService.Error("Error", "An error occurred while loading the Sign Up Grid data.");
        }

    } 


    /// <summary>
    /// On loading of the Grid get the user grid persisted data
    /// </summary>
    /// <returns></returns>
    protected async Task OnLoad()
    {
        string userName = _svcUser.GetUserName();
        UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.SignUp };
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
        UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.SignUp, Data = state };
        var result = await _svcUserPersist.SaveGridPersistence(persist);
        if (!result.Success)
        {
            Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.SignUp} : {result.Message}");
        }
    }

    private async Task LoadGridData()
    {
        await LoadConfiguration();
        await LoadUserData();
        await LoadLocations();
        lstVehicleTypes = EnumHelper.GetVehicleTypeItems();
        DisableToolBar();
        await LoadSignUpData(CurrentFilter);
    }

    private async Task LoadConfiguration()
    {
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
        userLocationId = _svcUser.GetUserLocationId();
        userName = _svcUser.GetUserName();
        userRole = _svcUser.GetUserRole();
        Log.Information($"{userName} went to the Manage Sign Up Page");

        Toolbaritems.Add(new Syncfusion.Blazor.Navigations.ItemModel()
        {
            Text = CaptionAdd,
            Id = "add",
            TooltipText = "Add Volunteer to selected Event",
            PrefixIcon = "e-add"
        });
        Toolbaritems.Add(new Syncfusion.Blazor.Navigations.ItemModel()
        {
            Text = CaptionDelete,
            Id = "delete",
            TooltipText = "Delete Volunteer from selected Event",
            PrefixIcon = "e-delete"
        });
        Toolbaritems.Add(new Syncfusion.Blazor.Navigations.ItemModel()
        { Text = "PDF Export", Id = "pdf", TooltipText = "Export Grid Data to PDF" });
        Toolbaritems.Add(new Syncfusion.Blazor.Navigations.ItemModel()
        { Text = "Excel Export", Id = "excel", TooltipText = "Export Grid Data to Excel" });
        Toolbaritems.Add(new Syncfusion.Blazor.Navigations.ItemModel()
        { Text = "CSV Export", Id = "csv", TooltipText = "Export Grid Data to CSV" });
        Toolbaritems.Add(new Syncfusion.Blazor.Navigations.ItemModel()
        { Text = Reset, Id = Reset, TooltipText = Reset });

    } 


    private bool IsLocationAdmin
    {
        get
        {
            return _svcAuth.UserHasRole(RoleNames.CanManageSchedule);
        }
    }

    private async Task LoadLocations()
    {
        //This GetAllAsync should always have less than 1000 records
        var dataLocations = await _svcLocation!.GetAllAsync();
        if (dataLocations.Success && dataLocations.Data != null) // 
        {
            Locations = dataLocations.Data;
            userLocationName = Locations.FirstOrDefault(e => e.LocationId == userLocationId)?.Name;
            ManageSignUpsMessage = $"Manage Sign-Ups for {userLocationName}";
        }
        else
        {
            Log.Error($"SignUpGrid, Error loading locations: {dataLocations.Message}");
            _toastService.Error("Error", dataLocations.Message);
            ErrorMessage = "Unable to load Locations. " + dataLocations.Message;
        }
    }

    private async Task LoadSignUpData(string filter)
    {
        var response = await _svcSignUp.GetSignUpsForSignUpGrid(_svcAuth.LocationId, filter);

        if (response.Success && response.Data != null)
        {
            SignUpDisplayItems = response.Data;
            RecordText = $"{SignUpDisplayItems.Count} Sign-Up Records Loaded";
            GridDisplay = String.Empty;
        }
        else
        {
            Log.Error($"SignUpGrid, Error loading SignUp data: {response.Message}");
            _toastService.Error("Error", response.Message);
            ErrorMessage = "Unable to load Sign-Up Data. " + response.Message;
            SignUpDisplayItems = new List<SignUpDisplayItem>();
            GridDisplay = DisplayNone;
        }
    } 

    private async Task ToolbarClickHandler(Syncfusion.Blazor.Navigations.ClickEventArgs args)
    {
        bool bSelectionStatus = false;
        displayVolunteerData = DisplayNone;
        var strMessageText = String.Empty;

        if (args.Item.Text.ToString() == Reset)
        {
            await Grid.ResetPersistDataAsync();
            await SaveGridPersistence();
            return;
        }

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

        if (args.Item.Text.ToString().Contains("CSV"))
        {
            await CsvExportAsync();
            return;
        }

        if (selectedGridObject != null)
        {
            if (args.Item.Text.ToString() == CaptionAdd)
            {
                if (selectedGridObject.ScheduleId > 0) // Existing Schedule/Event ID
                {
                    bSelectionStatus = true;
                }
            }
            else // Delete
            {
                if (selectedGridObject.SignUpId > 0) // existing Link ID
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

    } 

    private async Task ToolbarActions(Syncfusion.Blazor.Navigations.ClickEventArgs args)
    {
        newSignUp = new SignUpDisplayItem();
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
                await PrepareVolunteerAddDialog();
                break;

            case CaptionDelete:
                newSignUp = SignUpHelper.PrepareVolunteerDeleteDialog(selectedGridObject, ref strMessageText,
                    ref DialogTitle, ref DisplayDeleteButton, ref CloseButtonCaption);
                break;
        }

        if (strMessageText.Length > 0)
        {
            DialogMessage = BootstrapHelper.GetBootstrapMessage("help", strMessageText, "", false);
        }

        this.ShowEditDialog = true;

    } 

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
            if (selectedGridObject.ScheduleId > 0 && selectedGridObject.VolunteerId == 0)
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

    } 

    private async Task PrepareVolunteerAddDialog()
    {
        DialogTitle = "Select Volunteer & Add to Event";
        // create a list to select Volunteer: Volunteers of current Location and not linked to selected event
        // Location Volunteers

        var response = await _svcSignUp.GetVolunteersNotSignedUpForAnEvent(_svcAuth.LocationId, selectedGridObject.ScheduleId);

        if (response.Success && response.Data != null)
        {
            lstLocationVolunteers = response.Data;
        }

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
        }
    }  


    private async Task onConfirmAdd()
    {
        // Action Finished
        AvailabilityMessage = (MarkupString)"&nbsp;";
        var strMessageText = "Error";
        var actionStatus = "Unable to Add Volunteer.";
        if (selectedGridObject != null && newSignUp.VolunteerId > 0)
        {
            var newSignUp = new SignUp();
            newSignUp.VolunteerId = this.newSignUp.VolunteerId;
            newSignUp.ScheduleId = selectedGridObject.ScheduleId;
            newSignUp.LocationId = selectedGridObject.ScheduleLocationId;
            newSignUp.NumberOfVolunteers = this.newSignUp.SignUpNumberOfVolunteers;
            newSignUp.VehicleType = this.newSignUp.VehicleType ?? VehicleType.None;
            var addResult = await _svcSignUp.CreateAsync(newSignUp);
            if (addResult.Success && addResult.Data != null)
            {
                string customMessage = "This is to confirm that your sign-up was created.";
                var emailResponse = await _svcEmailBuilder.SendSignUpConfirmationEmail(addResult.Data, customMessage);

                if (!emailResponse.Success)
                {
                    _toastService.Error("Could not queue email", emailResponse.Message);
                    Log.Logger.Error($"Error SendSignUpConfirmationEmail: {emailResponse.Message}");
                    return;
                }

                var smsResponse = await _sendSmsLogic.CreateSignUpReminder(addResult.Data);

                if (!smsResponse.Success)
                {
                    _toastService.Error("SMS Error", smsResponse.Message);
                    Log.Logger.Error($"Error CreateSignUpReminder: {smsResponse.Message}");
                    return;
                }

                await RefreshGrid();
                HideStuff();
                _toastService.Success("Volunteer Added", "Volunteer added to event");
                return;
            }
        }

        HideStuff();
        DialogMessage = BootstrapHelper.GetBootstrapMessage(actionStatus, strMessageText, "");
    }        

    private async Task onConfirmDelete()
    {
        // Action Finished
        var strMessageText = "Unable to remove the volunteer signup.";
        var actionStatus = "Error";
        if (selectedGridObject != null)
        {
            int signUpId = selectedGridObject.SignUpId;
            if (signUpId > 0)
            {
                var existingRecord = await _svcSignUp.GetByIdAsync(signUpId);
                var deleteResult = await _svcSignUp.DeleteAsync(signUpId);
                if (existingRecord.Success && deleteResult.Success)
                {
                    string customMessage = "This is to confirm that your sign-up was removed.";
                    var emailResponse = await _svcEmailBuilder.SendSignUpConfirmationEmail(existingRecord.Data, customMessage);

                    if (!emailResponse.Success)
                    {
                        _toastService.Error("Could not queue email", emailResponse.Message);
                        Log.Logger.Error($"Error SendSignUpRemovedEmail: {emailResponse.Message}");
                        return;
                    }
                    HideStuff();
                    await RefreshGrid();
                    _toastService.Success("Volunteer Removed", "The volunteer has been removed from the event.");
                    return;
                }
            }
        }

        HideStuff();
        DialogMessage = BootstrapHelper.GetBootstrapMessage(actionStatus, strMessageText, "");
    } 

    private void HideStuff()
    {
        ShowEditDialog = false;
        CloseButtonCaption = CaptionClose;
        DisplayDeleteButton = DisplayNone;
        DisplayDataPanel = DisplayNone;
        DisplaySearchPanel = DisplayNone;
        DisplayAddButton = DisplayNone;
        DisplayDataPanel = DisplayNone;
        DisplaySearchPanel = DisplayNone;
    }
    public async Task RefreshGrid()
    {
        await LoadSignUpData(CurrentFilter);
        Grid.CallStateHasChanged();
        Grid.Refresh();
    }

    public void OnVolunteerSelect(Syncfusion.Blazor.DropDowns.ChangeEventArgs<string, Volunteer> args)
    {
        DialogMessage = (MarkupString)"&nbps";
        DisplayAddButton = DisplayNone;
        displayVolunteerData = DisplayNone;
        newSignUp = new SignUpDisplayItem(); // empty data panel

        if (args.ItemData != null)
        {
            displayVolunteerData = "";
            newSignUp.VolunteerId = args.ItemData.VolunteerId;
            newSignUp.VolunteerEmail = args.ItemData.Email;
            newSignUp.VolunteerPhone = args.ItemData.Phone;
            newSignUp.VolunteerFirstName = args.ItemData.FirstName;
            newSignUp.VolunteerLastName = args.ItemData.LastName;
            newSignUp.VehicleType = args.ItemData.VehicleType;
            DisplayAddButton = ""; //  OK button
            var strMessageText = "New selected Volunteer will be added to selected Event. Are you sure?";
            DialogMessage = BootstrapHelper.GetBootstrapMessage("help", strMessageText, "", false);
        }
    }

    public void GetSelectedRecords(RowSelectEventArgs<SignUpDisplayItem> args)
    {
        DisableToolBar();
        selectedGridObject = args.Data;
        // Enable Add - in all situations
        var addItem = Toolbaritems.FirstOrDefault(tb => tb.Text == CaptionAdd);

        if (addItem != null)
        {
            addItem.Disabled = false;
        }

        //Enable Delete - if row contains Volunteer Data
        if (selectedGridObject.SignUpId > 0)
        {
            var delItem = Toolbaritems.FirstOrDefault(tb => tb.Text == "Delete");

            if (delItem != null)
            {
                delItem.Disabled = false;
            }
        }
    } 

    private async Task OnFilterChange(ChangeEventArgs<string, GridFilterOption> args)
    {
        await LoadSignUpData(args.Value);
        DisableToolBar();
    } 

    protected async Task PdfExport()
    {
        if (Grid != null)
        {
            PdfExportProperties exportProperties = new PdfExportProperties
            {
                FileName = FileUtil.BuildFileNameWithDate("SignUps", ".pdf"),
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.ExportToPdfAsync(exportProperties);
        }
    }
    protected async Task ExcelExport()
    {
        if (Grid != null)
        {
            ExcelExportProperties exportProperties = new ExcelExportProperties
            {
                FileName = FileUtil.BuildFileNameWithDate("SignUps", ".xlsx"),
            };

            await Grid.ExportToExcelAsync(exportProperties);
        }
    }
    protected async Task CsvExportAsync()
    {
        if (Grid != null)
        {
            ExcelExportProperties exportProperties = new ExcelExportProperties
            {
                FileName = FileUtil.BuildFileNameWithDate("SignUps", ".csv"),
            };

            await Grid.ExportToCsvAsync(exportProperties);
        }
    }


    public class GridFilterOption
    {
        public string ID { get; set; }
        public string Text { get; set; }
    }

    public string CurrentFilter = "allfuture";

    List<GridFilterOption> GridFilterOptions = new List<GridFilterOption>
    {
        new GridFilterOption() { ID = "reg", Text = "Future Events with Registered Volunteers" },
        new GridFilterOption() { ID = "notreg", Text = "Future Events without Registered Volunteers" },
        new GridFilterOption() { ID = "allfuture", Text = "All Future Events" },
        new GridFilterOption() { ID = "allpast", Text = "All Past Events" },
    };


}

