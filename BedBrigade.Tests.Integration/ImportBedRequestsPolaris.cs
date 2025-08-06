using BedBrigade.Common.Logic;
using Syncfusion.Licensing;
using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Tests.Integration
{
    [TestFixture]
    public class ImportBedRequestsPolaris
    {
        private string[] groups = new[]
        {
            "New Requests",
            "Contacted (Waiting)",
            "August 9th delivery",
            "Grove City requests",
            "Additional needs",
            "Unable to contact after 3 months",
            "Out of town",
            "Wrong phone number listed",
            "Delivered"
        };

        [Test]
        public async Task Import()
        {
            if (!TestHelper.IsWindows() || !TestHelper.ThisComputerHasExcelInstalled())
            {
                Assert.Ignore("This test should only run locally.");
            }

            List<PolarisBedRequest> bedRequests = ExcelToPolarisBedRequests();
        }

        private List<PolarisBedRequest> ExcelToPolarisBedRequests()
        {
            List<PolarisBedRequest> results = new List<PolarisBedRequest>();
            SyncfusionLicenseProvider.RegisterLicense(LicenseLogic.SyncfusionLicenseKey);
            ExcelEngine excelEngine = new ExcelEngine();
            IApplication application = excelEngine.Excel;
            application.DefaultVersion = ExcelVersion.Xlsx;
            string filePath = @"C:\Users\gfinz\Downloads\Bed_Requests_1754480988.xlsx";
            IWorkbook workbook = application.Workbooks.Open(filePath);
            IWorksheet sheet = workbook.Worksheets.First();
            IRange usedRange = sheet.UsedRange;
            int lastRow = usedRange.LastRow;
            string group = "New Requests";

            for (int i = 1; i <= lastRow; i++)
            {
                string name = usedRange[i, 1].Value?.ToString();

                // Skip empty and header rows
                if (string.IsNullOrEmpty(name) || name == "Name" || name == "Bed Requests")
                {
                    continue; 
                }
                // Check if the group has changed
                if (groups.Contains(name))
                {
                    group = name;
                    continue; // Skip the group header row
                }
                PolarisBedRequest bedRequest = new PolarisBedRequest
                {
                    Group = group,
                    Name = usedRange[i, 1].Value?.ToString(),
                    RequestersName = usedRange[i, 2].Value?.ToString(),
                    DateOfRequest = usedRange[i, 3].DateTime,
                    BedForAdultOrChild = usedRange[i, 4].Value?.ToString(),
                    BedType = usedRange[i, 5].Value?.ToString(),
                    AdultName = usedRange[i, 6].Value?.ToString(),
                    AdultGender = usedRange[i, 7].Value?.ToString(),
                    ChildAge = usedRange[i, 8].Value?.ToString(),
                    ChildGender = usedRange[i, 9].Value?.ToString(),
                    ChildName = usedRange[i, 10].Value?.ToString(),
                    DeliveryAddress = usedRange[i, 11].Value?.ToString(),
                    Email = usedRange[i, 12].Value?.ToString(),
                    PrimaryLanguage = usedRange[i, 13].Value?.ToString(),
                    PhoneNumber = usedRange[i, 14].Value?.ToString(),
                    SpeaksEnglish = usedRange[i, 15].Value?.ToString(),
                    Status = usedRange[i, 16].Value?.ToString(),
                    DeliveryDate = usedRange[i, 17].DateTime,
                    Reference = usedRange[i, 23].Value?.ToString()
                };
                results.Add(bedRequest);
            }

            return results;
        }
    }
}
