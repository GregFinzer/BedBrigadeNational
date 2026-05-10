using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Serilog;
using Syncfusion.Blazor.Grids;

namespace BedBrigade.Client.Components.Pages.Administration.Admin;

public partial class ViewTranslationQueue : ComponentBase
{
    [Inject] private ITranslationQueueDataService TranslationQueueDataService { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;
    protected SfGrid<TranslationQueueView>? Grid { get; set; }
    protected List<TranslationQueueView>? Translations { get; set; }

    protected List<string> Toolbar { get; set; } =
        new() { "Print", "Pdf Export", "Excel Export", "Csv Export", "Search" };

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var data = await TranslationQueueDataService.GetTranslationQueueView();
            Translations = data
                .OrderByDescending(t => t.QueueDate)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load translation queue");
            Toast.Error("Translation Queue", ex.Message);
            Translations = new List<TranslationQueueView>();
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
                    FileName = FileUtil.BuildFileNameWithDate("TranslationQueue", ".pdf"),
                    PageOrientation = PageOrientation.Landscape
                };
                await Grid.ExportToPdfAsync(pdfExportProperties);
                break;
            case "Excel Export":
                var excelExportProperties = new ExcelExportProperties
                {
                    FileName = FileUtil.BuildFileNameWithDate("TranslationQueue", ".xlsx")
                };
                await Grid.ExportToExcelAsync(excelExportProperties);
                break;
            case "Csv Export":
                var csvExportProperties = new ExcelExportProperties
                {
                    FileName = FileUtil.BuildFileNameWithDate("TranslationQueue", ".csv")
                };
                await Grid.ExportToCsvAsync(csvExportProperties);
                break;
        }
    }
}
