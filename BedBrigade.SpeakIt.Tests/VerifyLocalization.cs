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
        private ParseLogic _logic = new ParseLogic();

        /// <summary>
        /// If this test is failing it means that there are new strings in your razor file or in your model file Required Attribute that need to be localized.
        /// Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md
        /// </summary>
        [Test]
        public void VerifyAllSourceCodeFilesAreLocalized()
        {
            SpeakItParms parms = new SpeakItParms();
            parms.SourceDirectories = TestHelper.GetSourceDirectories();
            parms.WildcardPatterns = TestHelper.WildcardPatterns;
            parms.ExcludeDirectories = TestHelper.ExcludeDirectories;
            parms.ExcludeFiles = TestHelper.ExcludeFiles;
            parms.ResourceFilePath = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Client", "Resources", "en-US.yml");
            parms.RemoveLocalizedKeys = true;

            var result = _logic.GetLocalizableStrings(parms);

            if (result.Any())
            {
                if (TestHelper.IsRunningUnderGitHubActions())
                {
                    Assert.Fail("VerifyAllSourceCodeFilesAreLocalized failed. Run locally to see the issues. Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md");
                }
                else
                {
                    StringBuilder sb = new StringBuilder(result.Count * 80);
                    foreach (var parseResult in result)
                    {
                        sb.AppendLine($"{Path.GetFileName(parseResult.FilePath)} | {parseResult.MatchValue} | {parseResult.LocalizableString}");
                    }
                    File.WriteAllText("RazorFiles.txt", sb.ToString());
                    TestHelper.Shell("RazorFiles.txt", string.Empty, ProcessWindowStyle.Maximized, false);
                    Assert.Fail("VerifyAllSourceCodeFilesAreLocalized failed. Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md");
                }
            }
        }

        /// <summary>
        /// If this test is failing it means that there are new strings that need to be localized
        /// and if they were to be created automatically, there would be the same key that have different values
        /// Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md
        /// </summary>
        [Test]
        public void VerifyNoDuplicateKeys()
        {
            SpeakItParms parms = new SpeakItParms();
            parms.SourceDirectories = TestHelper.GetSourceDirectories();
            parms.WildcardPatterns = TestHelper.WildcardPatterns;
            parms.ExcludeDirectories = TestHelper.ExcludeDirectories;
            parms.ExcludeFiles = TestHelper.ExcludeFiles;
            parms.ResourceFilePath = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Client", "Resources", "en-US.yml");

            var failedKeys = _logic.GetDuplicateKeys(parms);

            if (failedKeys.Any())
            {
                if (TestHelper.IsRunningUnderGitHubActions())
                {
                    Assert.Fail("VerifyDuplicateKeys Failed. Run locally to see the issues. Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md");
                }
                else
                {
                    string outputFileName = "FailedKeys.txt";
                    if (File.Exists(outputFileName))
                        File.Delete(outputFileName);

                    StringBuilder sb = new StringBuilder(failedKeys.Count * 80);
                    foreach (var failedKey in failedKeys)
                    {
                        foreach (var item in failedKey.Value)
                        {
                            sb.AppendLine($"{failedKey.Key} : {item}");
                        }
                    }

                    File.WriteAllText(outputFileName, sb.ToString());
                    TestHelper.Shell(outputFileName, string.Empty, ProcessWindowStyle.Maximized, false);
                    Assert.Fail(
                        "VerifyDuplicateKeys Failed. Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md");
                }
            }
        }


        /// <summary>
        /// If this test is failing it means that you manually typed in a key in your razor file,
        /// and it does not exist in the en-US.yml file, or you deleted a key value pair in the en-Us.yml file that was in use.
        /// Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md
        /// </summary>
        [Test]
        public void VerifyAllKeysCanBeFound()
        {
            SpeakItParms parms = new SpeakItParms();
            parms.SourceDirectories = TestHelper.GetSourceDirectories();
            parms.WildcardPatterns = TestHelper.WildcardPatterns;
            parms.ExcludeDirectories = TestHelper.ExcludeDirectories;
            parms.ExcludeFiles = TestHelper.ExcludeFiles;
            parms.ResourceFilePath = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Client", "Resources", "en-US.yml");

            var result = _logic.GetExistingLocalizedStrings(parms).Where(o => String.IsNullOrEmpty(o.LocalizableString));

            if (result.Any())
            {
                if (TestHelper.IsRunningUnderGitHubActions())
                {
                    Assert.Fail("VerifyAllKeysCanBeFound Failed. Missing key in en-US.yml. Run locally to see the issues. Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md");
                }
                else
                {
                    StringBuilder sb = new StringBuilder(result.Count() * 80);
                    foreach (var parseResult in result)
                    {
                        sb.AppendLine($"{parseResult.FilePath} | {parseResult.MatchValue}");
                    }
                    File.WriteAllText("MissingKeys.txt", sb.ToString());

                    TestHelper.Shell("MissingKeys.txt", string.Empty, ProcessWindowStyle.Maximized, false);
                    Assert.Fail("VerifyAllKeysCanBeFound Failed. Missing key in en-US.yml Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md");
                }
            }
        }

        /// <summary>
        /// If this test is failing, it means that you have keys in your en-US.yml file that are not being used in your razor files.
        /// Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md
        /// </summary>
        [Test]
        public void VerifyNoUnusedKeys()
        {
            SpeakItParms parms = new SpeakItParms();
            parms.SourceDirectories = TestHelper.GetSourceDirectories();
            parms.WildcardPatterns = TestHelper.WildcardPatterns;
            parms.ExcludeDirectories = TestHelper.ExcludeDirectories;
            parms.ExcludeFiles = TestHelper.ExcludeFiles;
            parms.ResourceFilePath = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Client", "Resources", "en-US.yml");

            var result = _logic.GetUnusedKeys(parms);

            if (result.Any())
            {
                if (TestHelper.IsRunningUnderGitHubActions())
                {
                    Assert.Fail("Unused keys in en-US.yml. Run locally to see the issues. Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md");
                }
                else
                {
                    File.WriteAllLines("UnusedKeys.txt", result);

                    TestHelper.Shell("UnusedKeys.txt", string.Empty, ProcessWindowStyle.Maximized, false);
                    Assert.Fail("Unused keys in en-US.yml Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md");
                }
            }
        }


    }
}
