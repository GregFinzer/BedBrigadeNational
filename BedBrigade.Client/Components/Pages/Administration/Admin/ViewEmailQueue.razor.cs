using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using Syncfusion.Blazor.Grids;

namespace BedBrigade.Client.Components.Pages.Administration.Admin;

public partial class ViewEmailQueue : ComponentBase
{
    [Inject] private IEmailQueueDataService EmailQueueService { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;
    [Inject] private IAuthService AuthService { get; set; } = default!;
    protected SfGrid<EmailQueue>? Grid { get; set; }
    protected List<EmailQueue>? Emails { get; set; }

    protected List<string> Toolbar { get; set; } =
        new() { "View", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search" };

    private EmailQueue? _selected;
    protected bool ViewDialogVisible { get; set; }
    protected string SelectedBody { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var response = await EmailQueueService.GetAllForLocationAsync(AuthService.LocationId);
            if (!response.Success || response.Data is null)
            {
                Emails = new List<EmailQueue>();
                Log.Error("Failed to load email queue data: " + response.Message);
                Toast.Error("Email Queue", response.Message);
                return;
            }

            Emails = response.Data
                .OrderByDescending(e => e.CreateDate ?? DateTime.MinValue)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load email queue");
            Toast.Error("Email Queue", ex.Message);
            Emails = new List<EmailQueue>();
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
                    FileName = FileUtil.BuildFileNameWithDate("EmailQueue", ".pdf"),
                    PageOrientation = PageOrientation.Landscape
                };
                await Grid.ExportToPdfAsync(pdfExportProperties);
                break;
            case "Excel Export":
                var excelExportProperties = new ExcelExportProperties
                {
                    FileName = FileUtil.BuildFileNameWithDate("EmailQueue", ".xlsx")
                };
                await Grid.ExportToExcelAsync(excelExportProperties);
                break;
            case "Csv Export":
                var csvExportProperties= new ExcelExportProperties
                {
                    FileName = FileUtil.BuildFileNameWithDate("EmailQueue", ".csv")
                };
                await Grid.ExportToCsvAsync(csvExportProperties);
                break;
        }
    }

    private void ShowDialogForSelected()
    {
        if (_selected == null)
        {
            Toast.Warning("Email Queue", "Select a row to view.");
            return;
        }
        SelectedBody = _selected.Body ?? string.Empty;
        ViewDialogVisible = true;
    }

    protected void CloseDialog()
    {
        ViewDialogVisible = false;
    }

    protected async Task OnRowSelected(RowSelectEventArgs<EmailQueue> args)
    {
        _selected = args.Data;
        if (Grid != null)
        {
            await Grid.EnableToolbarItemsAsync(new List<string> { "ViewEmailQueue_View" }, true); // auto id format ComponentId_ItemText
        }
    }

    protected void OnRecordDoubleClick(RecordDoubleClickEventArgs<EmailQueue> args)
    {
        _selected = args.RowData;
        ShowDialogForSelected();
    }
}
