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
        new() { "Print", "Pdf Export", "Excel Export", "Csv Export", "Search" };

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
}
