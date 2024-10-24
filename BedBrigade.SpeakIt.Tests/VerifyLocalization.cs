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
            parms.ResourceFilePath =
                Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Client", "Resources", "en-US.yml");
            parms.RemoveLocalizedKeys = true;

            var result = _logic.GetLocalizableStrings(parms);

            if (result.Any())
            {
                StringBuilder sb = new StringBuilder(result.Count * 80);
                sb.AppendLine(
                    "VerifyAllSourceCodeFilesAreLocalized failed. Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md");
                foreach (var parseResult in result)
                {
                    sb.AppendLine(
                        $"{Path.GetFileName(parseResult.FilePath)} | {parseResult.LocalizableString}");
                }

                Assert.Fail(sb.ToString());
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
                StringBuilder sb = new StringBuilder(failedKeys.Count * 80);
                sb.AppendLine("VerifyDuplicateKeys Failed. Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md");
                foreach (var failedKey in failedKeys)
                {
                    foreach (var item in failedKey.Value)
                    {
                        sb.AppendLine($"{failedKey.Key} : {item}");
                    }
                }

                Assert.Fail(sb.ToString());
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
            parms.ResourceFilePath =
                Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Client", "Resources", "en-US.yml");

            var result = _logic.GetExistingLocalizedStrings(parms)
                .Where(o => String.IsNullOrEmpty(o.LocalizableString));

            if (result.Any())
            {
                StringBuilder sb = new StringBuilder(result.Count() * 80);
                sb.AppendLine(
                    "VerifyAllKeysCanBeFound Failed. Missing key in en-US.yml Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md");
                foreach (var parseResult in result)
                {
                    sb.AppendLine($"{parseResult.FilePath} | {parseResult.MatchValue}");
                }

                Assert.Fail(sb.ToString());
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
                StringBuilder sb = new StringBuilder(result.Count * 80);
                sb.AppendLine(
                    "Unused keys in en-US.yml Follow these instructions:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Localization.md");

                foreach (var item in result)
                {
                    sb.AppendLine(item);
                }
                Assert.Fail(sb.ToString());
            }
        }
    }
}
