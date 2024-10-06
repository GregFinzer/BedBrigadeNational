namespace BedBrigade.SpeakIt.Tests
{
    [TestFixture]
    public class CreateTests
    {
        private CreateLogic _logic = new CreateLogic();

        [Test, Ignore("This test should be ignored except locally")]
        public void CreateLocalizationStringsTest()
        {
            if (TestHelper.IsRunningUnderGitHubActions())
            {
                Assert.Fail("This test should be ignored on the build server. Please add:\r\n[Ignore(\"This test should be ignored except locally\")]");
            }

            SpeakItParms parms = new SpeakItParms();
            string solutionPath = TestHelper.GetSolutionPath();
            string componentsPath = Path.Combine(solutionPath, "BedBrigade.Client", "Components");
            string modelPath = Path.Combine(solutionPath, "BedBrigade.Common", "Models");
            parms.SourceDirectories = new List<string>() { componentsPath, modelPath };
            parms.WildcardPatterns = new List<string>() { "*.razor", "*.cs" };
            parms.ExcludeDirectories = TestHelper.ExcludeDirectories;
            parms.ExcludeFiles = TestHelper.ExcludeFiles;
            parms.ResourceFilePath = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Client", "Resources", "en-US.yml");
            _logic.CreateLocalizationStrings(parms);
        }
    }
}
