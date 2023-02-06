using System.Diagnostics;
using KellermanSoftware.HTMLReports;
using KellermanSoftware.StaticCodeAnalysis;

namespace BedBrigade.Tests
{
    [TestFixture]
    public class QualityTests
    {
        private QualityLogic _qualityLogic;
        private string _solutionPath;
        
        [SetUp]
        public void Setup()
        {
            _qualityLogic = new QualityLogic(TestHelper.KellermanUserName, TestHelper.KellermanLicenseKey);
            _solutionPath = TestHelper.GetSolutionPath();
        }


        [Test]
        public void QualityCheckBedBrigadeClient()
        {
            string projectPath = Path.Combine(_solutionPath, "BedBrigade.Client");
            QualityResult result = _qualityLogic.GetQualityViolationsForDirectory(projectPath);

            if (result.QualityViolations.Any())
            {
                ReportViolations(result);
                Assert.Fail("Failed Quality Check for BedBrigade.Client");
            }
            else
            {
                Console.WriteLine("Passed Quality Check for BedBrigade.Client");
            }
        }

        [Test]
        public void QualityCheckBedBrigadeData()
        {
            _qualityLogic.Config.DirectoriesToExclude.Add("Migrations");
            _qualityLogic.Config.DirectoriesToExclude.Add("Seeding");

            string projectPath = Path.Combine(_solutionPath, "BedBrigade.Data");
            QualityResult result = _qualityLogic.GetQualityViolationsForDirectory(projectPath);

            if (result.QualityViolations.Any())
            {
                ReportViolations(result);
                Assert.Fail("Failed Quality Check for BedBrigade.Data");
            }
            else
            {
                Console.WriteLine("Passed Quality Check for BedBrigade.Data");
            }
        }

        //[Test]
        //public void QualityCheckBedBrigadeServer()
        //{
        //    _qualityLogic.Config.DirectoriesToExclude.Add("Migrations");
        //    _qualityLogic.Config.DirectoriesToExclude.Add("Seeding");

        //    string projectPath = Path.Combine(_solutionPath, "BedBrigade.Server");
        //    QualityResult result = _qualityLogic.GetQualityViolationsForDirectory(projectPath);

        //    if (result.QualityViolations.Any())
        //    {
        //        ReportViolations(result);
        //        Assert.Fail("Failed Quality Check for BedBrigade.Server");
        //    }
        //    else
        //    {
        //        Console.WriteLine("Passed Quality Check for BedBrigade.Server");
        //    }
        //}

        //[Test]
        //public void QualityCheckBedBrigadeShared()
        //{
        //    string projectPath = Path.Combine(_solutionPath, "BedBrigade.Shared");
        //    QualityResult result = _qualityLogic.GetQualityViolationsForDirectory(projectPath);

        //    if (result.QualityViolations.Any())
        //    {
        //        ReportViolations(result);
        //        Assert.Fail("Failed Quality Check for BedBrigade.Shared");
        //    }
        //    else
        //    {
        //        Console.WriteLine("Passed Quality Check for BedBrigade.Shared");
        //    }
        //}

        private void ReportViolations(QualityResult result)
        {
            if (TestHelper.RunningInPipeline)
            {
                Console.WriteLine(_qualityLogic.ExportViolationsToString(result));
                return;
            }

            TestHelper.DeleteOldHtmlFiles();
            string tempFilePath = Path.GetTempFileName() + ".html";
            _qualityLogic.ExportViolationsToHtmlReportFile(result, TemplateName.BlackAndBlue,tempFilePath);
            TestHelper.Shell(tempFilePath, string.Empty, ProcessWindowStyle.Normal, false);
        }
    }

}
