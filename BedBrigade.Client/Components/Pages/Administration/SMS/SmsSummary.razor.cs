using System.Security.Claims;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Task = System.Threading.Tasks.Task;

namespace BedBrigade.Client.Components.Pages.Administration.SMS;

public partial class SmsSummary : ComponentBase, IDisposable
{
    [Inject] private IUserDataService? _svcUser { get; set; }
    [Inject] private IAuthService? _svcAuth { get; set; }
    [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
    [Inject] private ISmsQueueDataService? _svcSmsQueue { get; set; }
    [Inject] private ILocationDataService _svcLocation { get; set; }
    [Inject] private ISmsState _smsState { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }
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





    private void NavigateToDetails(string phoneNumber)
    {
        NavigationManager.NavigateTo($"/administration/SMS/SmsDetails/{CurrentLocationId}/{phoneNumber}");
    }

    private string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "#";
        var parts = name.Split(' ');
        return parts.Length > 1 ? $"{parts[0][0]}{parts[1][0]}".ToUpper() : $"{parts[0][0]}".ToUpper();
    }


}

