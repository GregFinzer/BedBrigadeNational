using System.Diagnostics;
using BedBrigade.Common.Logic;
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
            _qualityLogic = LibraryFactory.CreateQualityLogic();
            _qualityLogic.Config.SetConfig("CSharpMaxNotImplementedException", -1);

            //This has duplicate strings but it is okay since roles have multiple similar permissions
            _qualityLogic.Config.FilesToExclude.Add("RoleNames.cs");
            _solutionPath = TestHelper.GetSolutionPath();
        }

        [Test]
        public void QualityCheckBedBrigadeClient()
        {
            const string project = "BedBrigade.Client";
            string projectPath = Path.Combine(_solutionPath, project);
            QualityResult result = _qualityLogic.GetQualityViolationsForDirectory(projectPath);

            if (result.QualityViolations.Any())
            {
                ReportViolations(result);
                Assert.Fail($"Failed Quality Check for {project}");
            }
            else
            {
                Console.WriteLine($"Passed Quality Check for {project}");
            }
        }

        [Test]
        public void QualityCheckBedBrigadeCommon()
        {
            const string project = "BedBrigade.Common";
            string projectPath = Path.Combine(_solutionPath, project);
            QualityResult result = _qualityLogic.GetQualityViolationsForDirectory(projectPath);

            if (result.QualityViolations.Any())
            {
                ReportViolations(result);
                Assert.Fail($"Failed Quality Check for {project}");
            }
            else
            {
                Console.WriteLine($"Passed Quality Check for {project}");
            }
        }

        [Test]
        public void QualityCheckBedBrigadeData()
        {
            const string project = "BedBrigade.Data";
            _qualityLogic.Config.DirectoriesToExclude.Add("Migrations");
            _qualityLogic.Config.DirectoriesToExclude.Add("Seeding");

            string projectPath = Path.Combine(_solutionPath, project);
            QualityResult result = _qualityLogic.GetQualityViolationsForDirectory(projectPath);

            if (result.QualityViolations.Any())
            {
                ReportViolations(result);
                Assert.Fail($"Failed Quality Check for {project}");
            }
            else
            {
                Console.WriteLine($"Failed Quality Check for  {project}");
            }
        }



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
