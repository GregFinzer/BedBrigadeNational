using BedBrigade.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Security.Claims;
using Action = Syncfusion.Blazor.Grids.Action;

using static BedBrigade.Common.Logic.Extensions;
using ContentType = BedBrigade.Common.Enums.ContentType;
using BedBrigade.Data.Services;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Models;
using Serilog;

namespace BedBrigade.Client.Components.Pages.Administration.Manage;

public partial class ManageMetroAreas : ComponentBase
{
    [Inject] private IMetroAreaDataService? _svcMetroArea { get; set; }
    [Inject] private IUserDataService? _svcUser { get; set; }
    [Inject] private AuthenticationStateProvider? _authState { get; set; }
    [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }

    [Parameter] public string? Id { get; set; }


    private const string LastPage = "LastPage";
    private const string PrevPage = "PrevPage";
    private const string NextPage = "NextPage";
    private const string FirstPage = "First";
    private ClaimsPrincipal? Identity { get; set; }
    protected List<MetroArea>? MetroAreas { get; set; }
    protected SfGrid<MetroArea>? Grid { get; set; }
    protected List<string>? ToolBar;
    protected List<string>? ContextMenu;
    protected string? _state { get; set; }
    protected string? HeaderTitle { get; set; }
    protected string? ButtonTitle { get; private set; }
    protected string? addNeedDisplay { get; private set; }
    protected string? editNeedDisplay { get; private set; }
    protected SfToast? ToastObj { get; set; }
    protected string? ToastTitle { get; set; }
    protected string? ToastContent { get; set; }
    protected int ToastTimeout { get; set; } = 1000;

    protected string? RecordText { get; set; } = "Loading Metro Areas ...";
    protected string? Hide { get; private set; } = "true";
    public bool NoPaging { get; private set; }

    protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "200px" };

    /// <summary>
    /// Setup the configuration Grid component
    /// Establish the Claims Principal
    /// </summary>
    /// <returns></returns>
    protected override async Task OnInitializedAsync()
    {
        var authState = await _authState.GetAuthenticationStateAsync();
        Identity = authState.User;
        if (Identity.IsInRole(RoleNames.NationalAdmin))
        {
            ToolBar = new List<string>
                { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search" };
            ContextMenu = new List<string>
            {
                "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending",
                "SortDescending"
            }; 
        }
        else
        {
            ToolBar = new List<string> { "Search"};
            ContextMenu = new List<string>
            {
                FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending"
            }; 
        }

        var result = await _svcMetroArea.GetAllAsync();
        if (result.Success)
        {
            MetroAreas = result.Data.ToList();
        }
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            if (Identity.IsInRole(RoleNames.NationalAdmin))
            {
                Grid.EditSettings.AllowEditOnDblClick = true;
                Grid.EditSettings.AllowDeleting = true;
                Grid.EditSettings.AllowAdding = true;
                Grid.EditSettings.AllowEditing = true;
                StateHasChanged();
            }
        }

        return base.OnAfterRenderAsync(firstRender);
    }






    protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
    {
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

    public async Task OnActionBegin(ActionEventArgs<MetroArea> args)
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
                Add(args);
                break;

            case Action.Save:
                await Save(args);
                break;

            case Action.BeginEdit:
                BeginEdit();
                break;
        }

    }

    private async Task Delete(ActionEventArgs<MetroArea> args)
    {
        string reason = string.Empty;
        List<MetroArea> records = await Grid.GetSelectedRecordsAsync();
        ToastTitle = "Delete Metro Area";
        ToastTimeout = 6000;
        ToastContent = $"Unable to Delete. {reason}";
        foreach (var rec in records)
        {
            try
            {
                var deleteResult = await _svcMetroArea.DeleteAsync(rec.MetroAreaId);
                if (deleteResult.Success)
                {
                    ToastContent = "Delete Successful!";
                }
                else
                {
                    args.Cancel = true;
                }

            }
            catch (Exception ex)
            {
                args.Cancel = true;
                reason = ex.Message;

            }

            await ToastObj.ShowAsync(new ToastModel
                { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });

        }
    }

    private void Add(ActionEventArgs<MetroArea> args)
    {
        HeaderTitle = "Add Metro Area";
        ButtonTitle = "Add Metro Area";
    }

    private async Task Save(ActionEventArgs<MetroArea> args)
    {
        MetroArea metroArea = args.Data;
        if (metroArea.MetroAreaId != 0)
        {
            //Update Metro Area Record
            await UpdateMetroAreaAsync(metroArea);
        }
        else
        {
            await AddNewMetroAreaAsync(metroArea);
        }

        await Grid.CallStateHasChangedAsync();
        await Grid.Refresh();
    }

    private async Task AddNewMetroAreaAsync(MetroArea metroArea)
    {
        var result = await _svcMetroArea.CreateAsync(metroArea);

        ToastTitle = "Create Metro Area";

        if (result.Success)
        {
            ToastContent = "Metro Area Created Successfully!";
            await ToastObj.ShowAsync(new ToastModel
                { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
        }
        else
        {
            ToastContent = result.Message;
            await ToastObj.ShowAsync(new ToastModel
                { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
        }
    }

    private async Task UpdateMetroAreaAsync(MetroArea metroArea)
    {
        var updateResult = await _svcMetroArea.UpdateAsync(metroArea);
        ToastTitle = "Update Metro Area";
        if (updateResult.Success)
        {
            ToastContent = "Metro Area Updated Successfully!";
        }
        else
        {
            ToastContent = "Unable to update Metro Area!";
        }

        await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
    }



    private void BeginEdit()
    {
        HeaderTitle = "Update Metro Area";
        ButtonTitle = "Update";
    }

    protected async Task Save(MetroArea metroArea)
    {
        await Grid.EndEdit();
    }

    protected async Task Cancel()
    {
        await Grid.CloseEdit();
    }

    protected void DataBound()
    {
        if (MetroAreas.Count == 0) RecordText = "No Metro Area records found";
        if (Grid.TotalItemCount <= Grid.PageSettings.PageSize) //compare total grid data count with pagesize value 
        {
            NoPaging = true;
        }
        else
            NoPaging = false;

    }

    protected async Task PdfExport()
    {
        PdfExportProperties ExportProperties = new PdfExportProperties
        {
            FileName = "MetroAreas" + DateTime.Now.ToShortDateString() + ".pdf",
            PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
        };
        await Grid.PdfExport(ExportProperties);
    }

    protected async Task ExcelExport()
    {
        ExcelExportProperties ExportProperties = new ExcelExportProperties
        {
            FileName = "MetroAreas " + DateTime.Now.ToShortDateString() + ".xlsx",

        };

        await Grid.ExcelExport();
    }

    protected async Task CsvExportAsync()
    {
        ExcelExportProperties ExportProperties = new ExcelExportProperties
        {
            FileName = "MetroAreas " + DateTime.Now.ToShortDateString() + ".csv",

        };

        await Grid.CsvExport(ExportProperties);
    }

    /// <summary>
    /// On loading of the Grid get the user grid persisted data
    /// </summary>
    /// <returns></returns>
    protected async Task OnLoad()
    {
        string userName = await _svcUser.GetUserName();
        UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.MetroAreas };
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
        string userName = await _svcUser.GetUserName();
        UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.MetroAreas, Data = state };
        var result = await _svcUserPersist.SaveGridPersistence(persist);
        if (!result.Success)
        {
            Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.MetroAreas} : {result.Message}");
        }
    }

}

