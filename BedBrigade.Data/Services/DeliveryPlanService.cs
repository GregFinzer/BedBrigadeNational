using BedBrigade.Common.Models;
using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Data.Services
{
    public class DeliveryPlanService : IDeliveryPlanService
    {
        private readonly IDashboardDataService _dashboardDataService;
        private readonly ILocationDataService _locationDataService;

        public DeliveryPlanService(IDashboardDataService dashboardDataService,
            ILocationDataService locationDataService)
        {
            _dashboardDataService = dashboardDataService;
            _locationDataService = locationDataService;
        }

        public async Task<ServiceResponse<DeliveryPlanExportResult>> CreateDeliveryPlanExcel(int locationId)
        {
            var scheduled = (await _dashboardDataService.GetDeliveryPlan(locationId)).Data;

            if (!scheduled.Any())
            {
                return new ServiceResponse<DeliveryPlanExportResult>("No scheduled deliveries found", false, null);
            }

            var location = (await _locationDataService.GetByIdAsync(locationId)).Data;

            // Build Excel using Syncfusion XlsIO
            using var excelEngine = new ExcelEngine();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Excel2016;

            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];

            PageSetup(sheet);
            BuildHeaderRow(sheet);
            AutoFitColumns(sheet);
            BuildTitleRow(location, sheet);

            // Data rows with gray bar style alternating
            BuildDetailRows(scheduled, sheet);

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"Delivery Plan {DateTime.UtcNow:yyyy-MM-dd}.xlsx";

            var result = new DeliveryPlanExportResult
            {
                FileName = fileName,
                Stream = stream
            };

            return new ServiceResponse<DeliveryPlanExportResult>("Delivery plan Excel created", true, result);
        }

        private static void BuildDetailRows(List<DeliveryPlan> scheduled, IWorksheet sheet)
        {
            int row = 3;
            bool gray = true;
            foreach (var d in scheduled)
            {
                gray = !gray;
                var bg = gray ? Syncfusion.Drawing.Color.LightGray : Syncfusion.Drawing.Color.White;

                sheet.Range[row, 1].DateTime = d.DeliveryDate;
                sheet.Range[row, 1].NumberFormat = "m/d/yyyy";
                sheet.Range[row, 2].Text = d.Group;
                sheet.Range[row, 3].Text = d.Team;
                sheet.Range[row, 4].Number = d.NumberOfBeds;
                sheet.Range[row, 5].Number = d.Stops;

                for (int c = 1; c <= 5; c++)
                {
                    sheet.Range[row, c].CellStyle.Color = bg;
                    sheet.Range[row, c].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                    sheet.Range[row, c].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                    sheet.Range[row, c].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                    sheet.Range[row, c].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                    sheet.Range[row, c].CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
                }

                row++;
            }
        }

        private static void BuildTitleRow(Location? location, IWorksheet sheet)
        {
            string title = $"Delivery Plan for {location.Name}";
            sheet.Range[1, 1, 1, 5].Merge();
            sheet.Range[1, 1].Text = title;
            sheet.Range[1, 1].CellStyle.Font.Bold = true;
            sheet.Range[1, 1].CellStyle.Font.Size = 14;
            sheet.Range[1, 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            sheet.Range[1, 1].CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
            sheet.Range[1, 1].RowHeight = 20;
        }

        private static void AutoFitColumns(IWorksheet sheet)
        {
            sheet.Range[2, 1].ColumnWidth = 14; // date
            sheet.Range[2, 2].ColumnWidth = 18; // group
            sheet.Range[2, 3].ColumnWidth = 8; // team
            sheet.Range[2, 4].ColumnWidth = 8; // beds
            sheet.Range[2, 5].ColumnWidth = 8; // stops
        }

        private static void BuildHeaderRow(IWorksheet sheet)
        {
            string[] headers = new[] { "Delivery Date", "Group", "Team", "Beds", "Stops" };
            for (int i = 0; i < headers.Length; i++)
            {
                sheet.Range[2, i + 1].Text = headers[i];
                sheet.Range[2, i + 1].CellStyle.Font.Bold = true;
                sheet.Range[2, i + 1].CellStyle.Color = Syncfusion.Drawing.Color.Black;
                sheet.Range[2, i + 1].CellStyle.Font.Color = ExcelKnownColors.White;
                sheet.Range[2, i + 1].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                sheet.Range[2, i + 1].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                sheet.Range[2, i + 1].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                sheet.Range[2, i + 1].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
            }
        }

        private static void PageSetup(IWorksheet sheet)
        {
            sheet.PageSetup.Orientation = ExcelPageOrientation.Landscape;
            sheet.PageSetup.TopMargin = 0.25;
            sheet.PageSetup.BottomMargin = 0.25;
            sheet.PageSetup.LeftMargin = 0.25;
            sheet.PageSetup.RightMargin = 0.25;
        }
    }
}
