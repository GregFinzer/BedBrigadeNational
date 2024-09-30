using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BedBrigade.SpeakIt.Tests
{
    [TestFixture]
    public class VerifyLocalization
    {
        private SpeakItLogic _logic = new SpeakItLogic();

        [Test, Ignore("Localization Not Implemented Yet")]
        public void VerifyAllRazorFilesAreLocalized()
        {
            ParseParms parms = new ParseParms();
            parms.TargetDirectory = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Client", "Components");
            parms.WildcardPattern = "*.razor";
            parms.ExcludeDirectories = new List<string> { "Administration", "Layout" };
            var result = _logic.GetLocalizableStrings(parms);

            if (result.Any())
            {


                if (TestHelper.IsRunningUnderGitHubActions())
                {
                    Assert.Fail("Failed Localization Check for Razor Files. Run locally to see the issues.");
                }
                else
                {
                    StringBuilder sb = new StringBuilder(result.Count * 80);
                    foreach (var parseResult in result)
                    {
                        sb.AppendLine($"{Path.GetFileName(parseResult.FilePath)} | {parseResult.MatchValue} | {parseResult.LocalizableString}");
                    }
                    File.WriteAllText("RazorFiles.txt", sb.ToString());
                    TestHelper.Shell("RazorFiles.txt",string.Empty,ProcessWindowStyle.Maximized, false);
                    Assert.Fail("Failed Localization Check for Razor Files");
                }
            }
        }
    }
}
