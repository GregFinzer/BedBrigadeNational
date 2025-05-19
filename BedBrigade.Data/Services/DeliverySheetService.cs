using Syncfusion.XlsIO;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;


namespace BedBrigade.Data.Services
{
    public class DeliverySheetService : IDeliverySheetService
    {
        public string CreateDeliverySheetFileName(Location location, List<BedRequest> bedRequests)
        {
            string locationName = FileUtil.FilterFileName(location.Name, false);
            string fileName = $"Delivery_Sheet_{locationName}";

            if (bedRequests.Any(o => o.DeliveryDate.HasValue))
            {
                fileName += "_" + bedRequests.First(o => o.DeliveryDate.HasValue).DeliveryDate.Value.ToString("yyyy-MM-dd");
            }

            fileName += ".xlsx";
            return fileName;
        }

        public Stream CreateDeliverySheet(Location location, List<BedRequest> bedRequests, string? deliveryChecklist= null)
        {
            // Create a new Excel document
            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                IApplication application = excelEngine.Excel;
                application.DefaultVersion = ExcelVersion.Excel2016;

                // Create a new workbook
                IWorkbook workbook = application.Workbooks.Create(1);
                IWorksheet worksheet = workbook.Worksheets[0];

                SetLandscape(worksheet);
                SetMargins(worksheet);
                CreateTitle(worksheet, location.Name, bedRequests);
                CreateHeader(worksheet);
                OutputBedRequests(worksheet, bedRequests);
                OutputDeliveryChecklist(worksheet, bedRequests.Count, deliveryChecklist);

                MemoryStream stream = new MemoryStream();
                
                workbook.SaveAs(stream);
                stream.Position = 0;
                return stream;
            }
        }

        private void OutputDeliveryChecklist(IWorksheet worksheet, int bedRequestsCount, string? deliveryChecklist)
        {
            if (String.IsNullOrWhiteSpace(deliveryChecklist))
            {
                return;
            }

            int row = bedRequestsCount + 4;
            string[] lines = deliveryChecklist.Split('\n');
            foreach (string line in lines)
            {
                worksheet.Range[row, 1].Text = line;
                row++;
            }
        }

        private void CreateTitle(IWorksheet worksheet, string locationName, List<BedRequest> bedRequests)
        {
            const string titleCell = "A1";
            string title = "Delivery Sheet for " + locationName;

            if (bedRequests.Any(o => o.DeliveryDate.HasValue))
            {
                title += " - " + bedRequests.First(o => o.DeliveryDate.HasValue).DeliveryDate.Value.ToShortDateString();
            }

            worksheet.Range["A1:I1"].Merge();
            worksheet.Range[titleCell].Text = title;
            worksheet.Range[titleCell].CellStyle.Font.FontName = "Arial"; 
            worksheet.Range[titleCell].CellStyle.Font.Bold = true;
            worksheet.Range[titleCell].CellStyle.Font.Size = 14;
            worksheet.Range[titleCell].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            worksheet.Range[titleCell].CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
            worksheet.Range[titleCell].RowHeight = 20;
        }

        private void OutputBedRequests(IWorksheet worksheet, List<BedRequest> bedRequests)
        {
            int row = 3;
            string? currentTeam = null;
            bool isWhiteBackground = false;

            foreach (BedRequest bedRequest in bedRequests)
            {
                if (currentTeam != bedRequest.Team)
                {
                    currentTeam = bedRequest.Team;
                    isWhiteBackground = !isWhiteBackground;
                }

                worksheet.Range[row, 1].Text = bedRequest.FullName;
                worksheet.Range[row, 2].Text = bedRequest.Phone.FormatPhoneNumber();
                worksheet.Range[row, 3].Text = bedRequest.Street;
                worksheet.Range[row, 4].Text = bedRequest.PostalCode;
                worksheet.Range[row, 5].Text = bedRequest.CreateDate.Value.ToShortDateString();
                worksheet.Range[row, 6].Text = bedRequest.NumberOfBeds.ToString();
                worksheet.Range[row, 7].Text = bedRequest.AgesGender;
                worksheet.Range[row, 8].Text = bedRequest.Team;
                worksheet.Range[row, 9].Text = bedRequest.Notes;

                ApplyBedRequestCellFormatting(worksheet, isWhiteBackground, row);

                row++;
            }
        }

        private static void ApplyBedRequestCellFormatting(IWorksheet worksheet, bool isWhiteBackground, int row)
        {
            Syncfusion.Drawing.Color backgroundColor = isWhiteBackground ? Syncfusion.Drawing.Color.White : Syncfusion.Drawing.Color.LightGray;

            for (int col = 1; col <= 9; col++)
            {
                worksheet.Range[row, col].CellStyle.Font.FontName = "Arial";
                worksheet.Range[row, col].CellStyle.Font.Size = 11;
                worksheet.Range[row, col].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range[row, col].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range[row, col].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range[row, col].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
                worksheet.Range[row, col].CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
                worksheet.Range[row, col].CellStyle.WrapText = true;
                worksheet.Range[row, col].CellStyle.Color = backgroundColor;

                // Center horizontally for Beds (column 6) and Team (column 8)
                if (col == 6 || col == 8)
                {
                    worksheet.Range[row, col].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                }
            }
        }

        private void CreateHeader(IWorksheet worksheet)
        {
            CreateHeaderCell(worksheet, "Name", 1, 18.14);
            CreateHeaderCell(worksheet, "Phone", 2, 15.14);
            CreateHeaderCell(worksheet, "Address", 3, 20.29);
            CreateHeaderCell(worksheet, "Zip", 4, 6.14);
            CreateHeaderCell(worksheet, "Requested", 5, 11);
            CreateHeaderCell(worksheet, "Beds", 6, 5.14);
            CreateHeaderCell(worksheet, "Ages", 7, 13);
            CreateHeaderCell(worksheet, "Team", 8, 5.14);
            CreateHeaderCell(worksheet, "Notes", 9, 42.14);
        }

        private void CreateHeaderCell(IWorksheet worksheet, string text, int col, double width)
        {
            int row = 2; 
            worksheet.Range[row, col].Text = text;
            worksheet.Range[row, col].CellStyle.Font.FontName = "Arial";
            worksheet.Range[row, col].CellStyle.Color = Syncfusion.Drawing.Color.Black;
            worksheet.Range[row, col].CellStyle.Font.Color = ExcelKnownColors.White;
            worksheet.Range[row, col].CellStyle.Font.Size = 11;
            worksheet.Range[row, col].CellStyle.Font.Bold = true;
            worksheet.Range[row, col].ColumnWidth = width;
            worksheet.Range[row, col].CellStyle.Borders[ExcelBordersIndex.EdgeLeft].LineStyle = ExcelLineStyle.Thin;
            worksheet.Range[row, col].CellStyle.Borders[ExcelBordersIndex.EdgeRight].LineStyle = ExcelLineStyle.Thin;
            worksheet.Range[row, col].CellStyle.Borders[ExcelBordersIndex.EdgeTop].LineStyle = ExcelLineStyle.Thin;
            worksheet.Range[row, col].CellStyle.Borders[ExcelBordersIndex.EdgeBottom].LineStyle = ExcelLineStyle.Thin;
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
