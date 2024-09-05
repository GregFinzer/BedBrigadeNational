using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using Syncfusion.XlsIO.Implementation;

namespace BedBrigade.Data.Services
{
    public class DeliverySheetService
    {
        public void CreateDeliverySheet(Location location, List<BedRequest> bedRequests)
        {
            // Set the Syncfusion license key
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(LicenseLogic.SyncfusionLicenseKey);

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
                CreateTitle(worksheet, location.Name);
                CreateHeader(worksheet);
                OutputBedRequests(worksheet, bedRequests);

                // Save the workbook to the current directory
                FileStream stream = new FileStream("Sample.xlsx", FileMode.Create, FileAccess.ReadWrite);
                
                workbook.SaveAs(stream);
                stream.Dispose();
            }


        }

        private void CreateTitle(IWorksheet worksheet, string locationName)
        {
            string title = "Delivery Sheet for " + locationName;
            worksheet.Range["A1:I1"].Merge();
            worksheet.Range["A1"].Text = title;
            worksheet.Range["A1"].CellStyle.Font.Bold = true;
            worksheet.Range["A1"].CellStyle.Font.Size = 14;
            worksheet.Range["A1"].CellStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
            worksheet.Range["A1"].CellStyle.VerticalAlignment = ExcelVAlign.VAlignCenter;
            worksheet.Range["A1"].RowHeight = 20;
        }

        private void OutputBedRequests(IWorksheet worksheet, List<BedRequest> bedRequests)
        {
            int row = 3;
            int? currentTeamNumber = null;
            bool isWhiteBackground = false;

            foreach (BedRequest bedRequest in bedRequests)
            {
                if (currentTeamNumber != bedRequest.TeamNumber)
                {
                    currentTeamNumber = bedRequest.TeamNumber;
                    isWhiteBackground = !isWhiteBackground;
                }

                worksheet.Range[row, 1].Text = bedRequest.FullName;
                worksheet.Range[row, 2].Text = bedRequest.Phone.FormatPhoneNumber();
                worksheet.Range[row, 3].Text = bedRequest.Street;
                worksheet.Range[row, 4].Text = bedRequest.PostalCode;
                worksheet.Range[row, 5].Text = bedRequest.CreateDate.Value.ToShortDateString();
                worksheet.Range[row, 6].Text = bedRequest.NumberOfBeds.ToString();
                worksheet.Range[row, 7].Text = bedRequest.AgesGender;
                worksheet.Range[row, 8].Text = bedRequest.TeamNumber.ToString();
                worksheet.Range[row, 9].Text = bedRequest.Notes;

                ApplyBedRequestCellFormatting(worksheet, isWhiteBackground, row);

                row++;
            }
        }

        private static void ApplyBedRequestCellFormatting(IWorksheet worksheet, bool isWhiteBackground, int row)
        {
            Syncfusion.Drawing.Color backgroundColor = isWhiteBackground ? Syncfusion.Drawing.Color.White : Syncfusion.Drawing.Color.WhiteSmoke;

            for (int col = 1; col <= 9; col++)
            {
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
            CreateHeaderCell(worksheet, "Name", 0, 18.14);
            CreateHeaderCell(worksheet, "Phone", 1, 15);
            CreateHeaderCell(worksheet, "Address", 2, 20.29);
            CreateHeaderCell(worksheet, "Zip", 3, 7);
            CreateHeaderCell(worksheet, "Requested", 4, 11);
            CreateHeaderCell(worksheet, "Beds", 5, 4.57);
            CreateHeaderCell(worksheet, "Ages", 6, 4.57);
            CreateHeaderCell(worksheet, "Team", 7, 5.14);
            CreateHeaderCell(worksheet, "Notes", 8, 27.80);
        }

        private void CreateHeaderCell(IWorksheet worksheet, string text, int column, double width)
        {
            string columnLetter = GetExcelColumnLetter(column);
            string cellRange = $"{columnLetter}2";
            worksheet.Range[cellRange].Text = text;
            worksheet.Range[cellRange].CellStyle.Color = Syncfusion.Drawing.Color.Black;
            worksheet.Range[cellRange].CellStyle.Font.Color = ExcelKnownColors.White;
            worksheet.Range[cellRange].ColumnWidth = width;
        }

        private static string GetExcelColumnLetter(int columnNumber)
        {
            columnNumber++;
            int dividend = columnNumber;
            string columnName = String.Empty;
            int modulo;

            while (dividend > 0)
            {
                modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
                dividend = (int)((dividend - modulo) / 26);
            }

            return columnName;
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
