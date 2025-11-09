using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using Syncfusion.Blazor.Grids;

namespace BedBrigade.Client.Components.Pages.Administration.Admin;

public partial class ViewSmsQueue : ComponentBase
{
    [Inject] private ISmsQueueDataService SmsQueueService { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;

    protected SfGrid<SmsQueue>? Grid { get; set; }
    protected List<SmsQueue>? Items { get; set; }

    protected List<string> Toolbar { get; set; } =
        new() { "View", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search" };

    private SmsQueue? _selected;
    protected bool ViewDialogVisible { get; set; }
    protected string SelectedBody { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            if (!AuthService.IsNationalAdmin)
            {
                Items = new List<SmsQueue>();
                Toast.Error("SMS Queue", "Not authorized.");
                return;
            }

            var response = await SmsQueueService.GetSmsQueueView();
            if (!response.Success || response.Data is null)
            {
                Items = new List<SmsQueue>();
                Log.Error("Failed to load SMS queue data: " + response.Message);
                Toast.Error("SMS Queue", response.Message);
                return;
            }

            Items = response.Data;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load SMS queue");
            Toast.Error("SMS Queue", ex.Message);
            Items = new List<SmsQueue>();
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
                    FileName = FileUtil.BuildFileNameWithDate("SmsQueue", ".pdf"),
                    PageOrientation = PageOrientation.Landscape
                };
                await Grid.ExportToPdfAsync(pdfExportProperties);
                break;
            case "Excel Export":
                var excelExportProperties = new ExcelExportProperties
                {
                    FileName = FileUtil.BuildFileNameWithDate("SmsQueue", ".xlsx")
                };
                await Grid.ExportToExcelAsync(excelExportProperties);
                break;
            case "Csv Export":
                var csvExportProperties= new ExcelExportProperties
                {
                    FileName = FileUtil.BuildFileNameWithDate("SmsQueue", ".csv")
                };
                await Grid.ExportToCsvAsync(csvExportProperties);
                break;
        }
    }

    private void ShowDialogForSelected()
    {
        if (_selected == null)
        {
            Toast.Warning("SMS Queue", "Select a row to view.");
            return;
        }
        SelectedBody = _selected.Body ?? string.Empty;
        ViewDialogVisible = true;
    }

    protected void CloseDialog()
    {
        ViewDialogVisible = false;
    }

    protected async Task OnRowSelected(RowSelectEventArgs<SmsQueue> args)
    {
        _selected = args.Data;
        if (Grid != null)
        {
            await Grid.EnableToolbarItemsAsync(new List<string> { "ViewSmsQueue_View" }, true); // auto id format ComponentId_ItemText
        }
    }

    protected void OnRecordDoubleClick(RecordDoubleClickEventArgs<SmsQueue> args)
    {
        _selected = args.RowData;
        ShowDialogForSelected();
    }

    protected void OnDialogOpen(Syncfusion.Blazor.Popups.BeforeOpenEventArgs args)
    {
        args.MaxHeight = "90%"; // set to 75% of viewport height
    }
}
