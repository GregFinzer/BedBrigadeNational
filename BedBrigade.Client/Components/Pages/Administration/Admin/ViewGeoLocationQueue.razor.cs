using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using Syncfusion.Blazor.Grids;

namespace BedBrigade.Client.Components.Pages.Administration.Admin;

public partial class ViewGeoLocationQueue : ComponentBase
{
    [Inject] private IGeoLocationQueueDataService GeoLocationQueueService { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;

    protected SfGrid<GeoLocationQueue>? Grid { get; set; }
    protected List<GeoLocationQueue>? Items { get; set; }

    protected List<string> Toolbar { get; set; } =
        new() { "View", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search" };

    private GeoLocationQueue? _selected;
    protected bool ViewDialogVisible { get; set; }
    protected string SelectedAddress { get; set; } = string.Empty;
    private const string GeoLocationQueueTitle = "GeoLocation Queue";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            if (!AuthService.IsNationalAdmin)
            {
                Items = new List<GeoLocationQueue>();
                Toast.Error(GeoLocationQueueTitle, "Not authorized.");
                return;
            }

            var response = await GeoLocationQueueService.GetAllAsync();
            if (!response.Success || response.Data is null)
            {
                Items = new List<GeoLocationQueue>();
                Log.Error("Failed to load GeoLocation queue data: " + response.Message);
                Toast.Error(GeoLocationQueueTitle, response.Message);
                return;
            }

            Items = response.Data;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load GeoLocation queue");
            Toast.Error(GeoLocationQueueTitle, ex.Message);
            Items = new List<GeoLocationQueue>();
        }
    }

    protected async Task OnToolbarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
    {
        if (Grid is null) return;

        switch (args.Item.Text)
        {
            case "View":
                ShowDialogForSelected();
                break;
            case "Pdf Export":
                var pdfExportProperties = new PdfExportProperties
                {
                    FileName = FileUtil.BuildFileNameWithDate("GeoLocationQueue", ".pdf"),
                    PageOrientation = PageOrientation.Landscape
                };
                await Grid.ExportToPdfAsync(pdfExportProperties);
                break;
            case "Excel Export":
                var excelExportProperties = new ExcelExportProperties
                {
                    FileName = FileUtil.BuildFileNameWithDate("GeoLocationQueue", ".xlsx")
                };
                await Grid.ExportToExcelAsync(excelExportProperties);
                break;
            case "Csv Export":
                var csvExportProperties = new ExcelExportProperties
                {
                    FileName = FileUtil.BuildFileNameWithDate("GeoLocationQueue", ".csv")
                };
                await Grid.ExportToCsvAsync(csvExportProperties);
                break;
        }
    }

    private void ShowDialogForSelected()
    {
        if (_selected == null)
        {
            Toast.Warning(GeoLocationQueueTitle, "Select a row to view.");
            return;
        }

        SelectedAddress = string.Join(", ", new[]
        {
            _selected.Street,
            _selected.City,
            _selected.State,
            _selected.PostalCode
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
        ViewDialogVisible = true;
    }

    protected void CloseDialog()
    {
        ViewDialogVisible = false;
    }

    protected async Task OnRowSelected(RowSelectEventArgs<GeoLocationQueue> args)
    {
        _selected = args.Data;
        if (Grid != null)
        {
            await Grid.EnableToolbarItemsAsync(new List<string> { "ViewGeoLocationQueue_View" }, true);
        }
    }

    protected void OnRecordDoubleClick(RecordDoubleClickEventArgs<GeoLocationQueue> args)
    {
        _selected = args.RowData;
        ShowDialogForSelected();
    }

    protected void OnDialogOpen(Syncfusion.Blazor.Popups.BeforeOpenEventArgs args)
    {
        args.MaxHeight = "90%";
    }
}
