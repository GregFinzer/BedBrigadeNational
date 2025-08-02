using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using Syncfusion.Blazor.Grids;
using System.Security.Claims;
using Action = Syncfusion.Blazor.Grids.Action;

namespace BedBrigade.Client.Components.Pages.Administration.Manage;

public partial class ManageMetroAreas : ComponentBase
{
    [Inject] private IMetroAreaDataService? _svcMetroArea { get; set; }
    [Inject] private IUserDataService? _svcUser { get; set; }
    [Inject] private IAuthService? _svcAuth { get; set; }
    [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
    [Inject] private ToastService _toastService { get; set; }
    [Parameter] public string? Id { get; set; }


    private const string LastPage = "LastPage";
    private const string PrevPage = "PrevPage";
    private const string NextPage = "NextPage";
    private const string FirstPage = "First";
    protected List<MetroArea>? MetroAreas { get; set; }
    protected SfGrid<MetroArea>? Grid { get; set; }
    protected List<string>? ToolBar;
    protected List<string>? ContextMenu;
    protected string? _state { get; set; }
    protected string? HeaderTitle { get; set; }
    protected string? ButtonTitle { get; private set; }

    protected string? RecordText { get; set; } = "Loading Metro Areas ...";
    public bool NoPaging { get; private set; }

    protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "200px" };
    public string ManageMetroAreasMessage { get; set; }
    /// <summary>
    /// Setup the configuration Grid component
    /// Establish the Claims Principal
    /// </summary>
    /// <returns></returns>
    protected override async Task OnInitializedAsync()
    {
        Log.Information($"{_svcAuth.UserName} went to the ManageMetroAreas Page");

        if (_svcAuth.IsNationalAdmin)
        {
            ToolBar = new List<string>
                { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search" };
            ContextMenu = new List<string>
            {
                "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending",
                "SortDescending"
            }; 
            ManageMetroAreasMessage = "Manage Metro Areas";
        }
        else
        {
            ToolBar = new List<string> { "Search"};
            ContextMenu = new List<string>
            {
                FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending"
            }; 
            ManageMetroAreasMessage = "View Metro Areas";
        }

        var result = await _svcMetroArea.GetAllAsync();
        if (result.Success && result.Data != null)
        {
            MetroAreas = result.Data.ToList();
        }
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            if (_svcAuth.IsNationalAdmin)
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
        List<MetroArea> records = await Grid.GetSelectedRecordsAsync();
        foreach (var rec in records)
        {
            try
            {
                var deleteResult = await _svcMetroArea.DeleteAsync(rec.MetroAreaId);
                if (deleteResult.Success)
                {
                    _toastService.Success("Delete Metro Area", $"Metro Area {rec.Name} deleted successfully!");
                }
                else
                {
                    Log.Error($"Unable to delete Metro Area {rec.Name}. " + deleteResult.Message);
                    _toastService.Error("Delete Metro Area", $"Unable to delete Metro Area {rec.Name}!");
                    args.Cancel = true;
                    break;
                }
            }
            catch (Exception ex)
            {
                args.Cancel = true;
                Log.Error(ex, $"Unable to delete Metro Area {rec.Name}!");
                _toastService.Error("Delete Metro Area", $"Unable to delete Metro Area {rec.Name}!");
            }
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

        if (result.Success)
        {
            _toastService.Success("Create Metro Area", "Metro Area Created Successfully!");
        }
        else
        {
            Log.Error("Unable to create Metro Area!" + result.Message);
            _toastService.Error("Create Metro Area", "Unable to create Metro Area!");
        }
    }

    private async Task UpdateMetroAreaAsync(MetroArea metroArea)
    {
        var updateResult = await _svcMetroArea.UpdateAsync(metroArea);
        if (updateResult.Success)
        {
            _toastService.Success("Update Metro Area", "Metro Area Updated Successfully!");
        }
        else
        {
            Log.Error("Unable to update Metro Area!" + updateResult.Message);
            _toastService.Error("Update Metro Area", "Unable to update Metro Area!");
        }
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
        if (Grid != null)
        {
            PdfExportProperties exportProperties = new PdfExportProperties
            {
                FileName = FileUtil.BuildFileNameWithDate("MetroAreas", ".pdf"),
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
                FileName = FileUtil.BuildFileNameWithDate("MetroAreas", ".xlsx"),
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
                FileName = FileUtil.BuildFileNameWithDate("MetroAreas", ".csv"),
            };

            await Grid.ExportToCsvAsync(exportProperties);
        }
    }

    /// <summary>
    /// On loading of the Grid get the user grid persisted data
    /// </summary>
    /// <returns></returns>
    protected async Task OnLoad()
    {
        string userName = _svcUser.GetUserName();
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
        string userName = _svcUser.GetUserName();
        UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.MetroAreas, Data = state };
        var result = await _svcUserPersist.SaveGridPersistence(persist);
        if (!result.Success)
        {
            Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.MetroAreas} : {result.Message}");
        }
    }

}

