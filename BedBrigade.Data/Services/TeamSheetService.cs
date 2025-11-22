using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using Syncfusion.XlsIO;

namespace BedBrigade.Data.Services
{
    public class TeamSheetService : ITeamSheetService
    {
        public string CreateTeamSheetFileName(Location location, List<BedRequest> bedRequests)
        {
            string locationName = FileUtil.FilterFileName(location.Name, false);
            string fileName = $"Team_Sheet_{locationName}";
            if (bedRequests.Any(o => o.DeliveryDate.HasValue))
            {
                // Use first delivery date that exists
                var firstDate = bedRequests.First(o => o.DeliveryDate.HasValue).DeliveryDate!.Value;
                fileName += "_" + firstDate.ToString("yyyy-MM-dd");
            }
            fileName += ".xlsx";
            return fileName;
        }

        public Stream CreateTeamSheet(Location location, List<BedRequest> bedRequests, string? deliveryChecklist = null)
        {
            using var excelEngine = new ExcelEngine();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Excel2016;
            IWorkbook workbook = application.Workbooks.Create(1);
            IWorksheet sheet = workbook.Worksheets[0];

            SetLandscape(sheet);
            SetMargins(sheet);

            // Group bed requests by Team (non-null, ordered)
            var grouped = bedRequests
                .Where(b => !string.IsNullOrWhiteSpace(b.Team))
                .GroupBy(b => b.Team!)
                .OrderBy(g => g.Key, StringComparer.InvariantCultureIgnoreCase)
                .ToList();

            int currentRow = 1;
            for (int i = 0; i < grouped.Count; i++)
            {
                var teamGroup = grouped[i].OrderBy(b => b.FullName).ToList();
                // Team header row: left merged half with "Team X" right merged half with date
                currentRow = CreateTeamHeader(sheet, teamGroup, currentRow);
                // Column headers
                CreateColumnHeaders(sheet, currentRow);
                currentRow++;
                // Output rows (no Team column per requirements)
                currentRow = OutputTeamRows(sheet, teamGroup, currentRow);
                // Blank line
                currentRow++;
                // Delivery checklist for this team
                currentRow = OutputDeliveryChecklist(sheet, deliveryChecklist, currentRow);

                // Add page break after team except last
                if (i < grouped.Count - 1)
                {
                    sheet.HPageBreaks.Add(sheet.Range[currentRow,1]);
                }
            }

            var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }

        private static int CreateTeamHeader(IWorksheet sheet, List<BedRequest> teamGroup, int startRow)
        {
            string teamName = teamGroup.First().Team ?? "";
            DateTime? deliveryDate = teamGroup.FirstOrDefault(b => b.DeliveryDate.HasValue)?.DeliveryDate;
            // Merge A..E for team, F..J for date (we will use 10 columns total similar style as delivery sheet minus team column)
            sheet.Range[startRow, 1, startRow, 5].Merge();
            sheet.Range[startRow, 6, startRow, 10].Merge();

            sheet.Range[startRow, 1].Text = $"Team {teamName}";
            sheet.Range[startRow, 6].Text = deliveryDate.HasValue ? deliveryDate.Value.ToShortDateString() : string.Empty;

            // Style
            sheet.Range[startRow, 1].CellStyle.Font.FontName = "Arial";
            sheet.Range[startRow, 1].CellStyle.Font.Size = 36;
            sheet.Range[startRow, 1].CellStyle.Font.Bold = true;
            sheet.Range[startRow, 1].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignLeft;
            sheet.Range[startRow, 1].RowHeight = 42;

            sheet.Range[startRow, 6].CellStyle.Font.FontName = "Arial";
            sheet.Range[startRow, 6].CellStyle.Font.Size = 36;
            sheet.Range[startRow, 6].CellStyle.Font.Bold = true;
            sheet.Range[startRow, 6].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignRight;
            sheet.Range[startRow, 6].RowHeight = 42;

            return startRow + 1; // next row
        }

        private static void CreateColumnHeaders(IWorksheet sheet, int row)
        {
            CreateHeaderCell(sheet, "Name", row, 1, 18.14);
            CreateHeaderCell(sheet, "Phone", row, 2, 15.14);
            CreateHeaderCell(sheet, "Address", row, 3, 20.29);
            CreateHeaderCell(sheet, "Zip", row, 4, 6.14);
            CreateHeaderCell(sheet, "Requested", row, 5, 11);
            CreateHeaderCell(sheet, "Beds", row, 6, 5.14);
            CreateHeaderCell(sheet, "Ages", row, 7, 13);
            CreateHeaderCell(sheet, "Notes", row, 8, 42.14);
            // Add two blank columns to keep layout (9,10) or could omit; here reserve for future
            sheet.Range[row, 9].ColumnWidth = 2.5;
            sheet.Range[row, 10].ColumnWidth = 2.5;
        }

        private static void CreateHeaderCell(IWorksheet sheet, string text, int row, int col, double width)
        {
            sheet.Range[row, col].Text = text;
            var style = sheet.Range[row, col].CellStyle;
            style.Font.FontName = "Arial";
            style.Color = Syncfusion.Drawing.Color.Black;
            style.Font.Color = ExcelKnownColors.White;
            style.Font.Size = 11;
            style.Font.Bold = true;
            sheet.Range[row, col].ColumnWidth = width;
            style.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
            style.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
            style.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
            style.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
        }

        private static int OutputTeamRows(IWorksheet sheet, List<BedRequest> bedRequests, int row)
        {
            bool white = true;
            foreach (var b in bedRequests)
            {
                white = !white;
                var bg = white ? Syncfusion.Drawing.Color.White : Syncfusion.Drawing.Color.LightGray;

                sheet.Range[row, 1].Text = b.FullName;
                sheet.Range[row, 2].Text = b.Phone.FormatPhoneNumber();
                sheet.Range[row, 3].Text = b.Street;
                sheet.Range[row, 4].Text = b.PostalCode;
                sheet.Range[row, 5].Text = b.CreateDate?.ToShortDateString() ?? "";
                sheet.Range[row, 6].Text = b.NumberOfBeds.ToString();
                sheet.Range[row, 7].Text = b.GenderAge;
                sheet.Range[row, 8].Text = b.Notes;

                ApplyRowFormatting(sheet, row, bg);
                row++;
            }
            return row;
        }

        private static void ApplyRowFormatting(IWorksheet sheet, int row, Syncfusion.Drawing.Color bg)
        {
            for (int col = 1; col <= 8; col++)
            {
                var style = sheet.Range[row, col].CellStyle;
                style.Font.FontName = "Arial";
                style.Font.Size = 11;
                style.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                style.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                style.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                style.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                style.VerticalAlignment = ExcelVAlign.VAlignCenter;
                style.WrapText = true;
                style.Color = bg;
                if (col == 6)
                {
                    style.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                }
            }
        }

        private static int OutputDeliveryChecklist(IWorksheet sheet, string? deliveryChecklist, int startRow)
        {
            if (string.IsNullOrWhiteSpace(deliveryChecklist))
            {
                return startRow; // nothing added
            }
            int row = startRow;
            string[] lines = deliveryChecklist.Split('\n');
            foreach (var line in lines)
            {
                sheet.Range[row, 1].Text = line.TrimEnd('\r');
                row++;
            }
            return row;
        }

        private static void SetMargins(IWorksheet worksheet)
        {
            worksheet.PageSetup.TopMargin = 0.25;
            worksheet.PageSetup.BottomMargin = 0.25;
            worksheet.PageSetup.LeftMargin = 0.25;
            worksheet.PageSetup.RightMargin = 0.25;
        }

        private static void SetLandscape(IWorksheet worksheet)
        {
            worksheet.PageSetup.Orientation = ExcelPageOrientation.Landscape;
        }
    }
}
