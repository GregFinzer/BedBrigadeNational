using System.Security.Claims;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using BedBrigade.Data.Migrations;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Twilio.TwiML.Voice;
using Action = Syncfusion.Blazor.Grids.Action;
using Task = System.Threading.Tasks.Task;
using UserPersist = BedBrigade.Common.Models.UserPersist;

namespace BedBrigade.Client.Components.Pages.Administration.SMS;

public partial class SmsSummary : ComponentBase, IDisposable
{
    [Inject] private IUserDataService? _svcUser { get; set; }
    [Inject] private IAuthService? _svcAuth { get; set; }
    [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
    [Inject] private ISmsQueueDataService? _svcSmsQueue { get; set; }
    [Inject] private ILocationDataService _svcLocation { get; set; }
    [Inject] private ISmsState _smsState { get; set; }
    protected List<SmsQueueSummary>? SummaryList { get; set; }
    private ClaimsPrincipal? Identity { get; set; }
    private const string LastPage = "LastPage";
    private const string PrevPage = "PrevPage";
    private const string NextPage = "NextPage";
    private const string FirstPage = "First";
    protected List<string>? ToolBar;
    protected List<string>? ContextMenu;
    protected bool ShowLocationDropdown { get; set; }
    protected List<Location>? Locations { get; set; }
    protected int CurrentLocationId { get; set; }
    protected SfGrid<SmsQueueSummary>? Grid { get; set; }
    protected string? RecordText { get; set; } = "Loading Text Message Summary ...";
    protected bool NoPaging { get; private set; }
    protected override async Task OnInitializedAsync()
    {
        try
        {
            Identity = _svcAuth.CurrentUser;
            ContextMenu = new List<string>
            {
                FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending",
                "SortDescending"
            };

            if (_svcAuth.UserHasRole(RoleNames.CanSendSms))
            {
                ToolBar = new List<string>
                    { "Print", "Pdf Export", "Excel Export", "Csv Export", "Reset", "Search",  };
            }
            else
            {
                ToolBar = new List<string> { "Reset", "Search" };
            }

            await LoadLocations();
            await LoadSummary();
            _smsState.OnChange += OnSmsStateChange;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }

    }

    private async Task OnSmsStateChange(SmsQueue smsQueue)
    {
        if (CurrentLocationId == smsQueue.LocationId)
        {
            await LoadSummary();
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        _smsState.OnChange -= OnSmsStateChange;
    }

    private async Task LoadSummary()
    {
        var result = await _svcSmsQueue.GetSummaryForLocation(CurrentLocationId);

        if (result.Success && result.Data != null && result.Data.Any())
        {
            SummaryList = result.Data.ToList();
        }
        else
        {
            SummaryList = new List<SmsQueueSummary>();
        }
    }

    private async Task LoadLocations()
    {
        var locationResult = await _svcLocation.GetAllAsync();
        if (locationResult.Success)
        {
            Locations = locationResult.Data.ToList();
            var item = Locations.Single(r => r.LocationId == (int)LocationNumber.National);
            if (item != null)
            {
                Locations.Remove(item);
            }
        }

        if (_svcAuth.UserHasRole(RoleNames.NationalAdmin))
        {
            ShowLocationDropdown = true;
            CurrentLocationId = Locations.First().LocationId;
        }
        else
        {
            CurrentLocationId = await _svcUser.GetUserLocationId();
        }
    }

    private async void LocationChangeEvent(ChangeEventArgs<int, Location> args)
    {
        CurrentLocationId = args.Value;
        await LoadSummary();
        StateHasChanged();
    }

    protected void DataBound()
    {
        if (SummaryList.Count == 0) RecordText = "No Text Messages found";
        if (Grid.TotalItemCount <= Grid.PageSettings.PageSize) //compare total grid data count with pagesize value 
        {
            NoPaging = true;
        }
        else
            NoPaging = false;

    }
    /// <summary>
    /// On destroying of the grid save its current state
    /// </summary>
    /// <returns></returns>
    protected async Task OnDestroyed()
    {
        await SaveGridPersistence();
    }

    /// <summary>
    /// On loading of the Grid get the user grid persisted data
    /// </summary>
    /// <returns></returns>
    protected async Task OnLoad()
    {
        string userName = await _svcUser.GetUserName();
        UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.SmsSummary };
        var result = await _svcUserPersist.GetGridPersistence(persist);
        if (result.Success && result.Data != null)
        {
            await Grid.SetPersistDataAsync(result.Data);
        }
    }

    private async Task SaveGridPersistence()
    {
        string state = await Grid.GetPersistData();
        string userName = await _svcUser.GetUserName();
        UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.SmsSummary, Data = state };
        var result = await _svcUserPersist.SaveGridPersistence(persist);
        if (!result.Success)
        {
            Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.SmsSummary} : {result.Message}");
        }
    }

    protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
    {
        switch (args.Item.Text)
        {
            case "Reset":
                await Grid.ResetPersistData();
                await SaveGridPersistence();
                break;
            case "Pdf Export":
                await PdfExport();
                break;
            case "Excel Export":
                await ExcelExport();
                break;
            case "Csv Export":
                await CsvExportAsync();
                break;
        }

    }

    protected async Task PdfExport()
    {
        PdfExportProperties ExportProperties = new PdfExportProperties
        {
            FileName = "TextMessageSummary " + DateTime.Now.ToShortDateString() + ".pdf",
            PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
        };
        await Grid.PdfExport(ExportProperties);
    }

    protected async Task ExcelExport()
    {
        ExcelExportProperties ExportProperties = new ExcelExportProperties
        {
            FileName = "TextMessageSummary " + DateTime.Now.ToShortDateString() + ".xlsx",

        };

        await Grid.ExcelExport();
    }

    protected async Task CsvExportAsync()
    {
        ExcelExportProperties ExportProperties = new ExcelExportProperties
        {
            FileName = "TextMessageSummary " + DateTime.Now.ToShortDateString() + ".csv",

        };

        await Grid.CsvExport(ExportProperties);
    }

    public async Task OnActionBegin(ActionEventArgs<SmsQueueSummary> args)
    {
        var requestType = args.RequestType;
        switch (requestType)
        {
            case Action.Searching:
                RecordText = "Searching ... Record Not Found.";
                break;
        }
    }



    public void RowBound(RowDataBoundEventArgs<SmsQueueSummary> args)
    {
        if (args.Data.UnRead)
        {
            args.Row.AddClass(new string[] { "new-message" });
        }
    }
}

