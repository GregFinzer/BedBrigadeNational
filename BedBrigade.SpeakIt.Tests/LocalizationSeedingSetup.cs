using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework.Internal;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace BedBrigade.SpeakIt.Tests
{
    [TestFixture]
    public class LocalizationSeedingSetup
    {
        private ParseLogic _parseLogic = new ParseLogic();
        private TranslateLogic _translateLogic = new TranslateLogic(null);
        private Dictionary<string, string> _localizableStrings = new Dictionary<string, string>();

        [Test, Ignore("This test should be ignored except locally")]
        //[Test]
        public void Setup()
        {
            if (TestHelper.IsRunningUnderGitHubActions())
            {
                Assert.Fail("This test should be ignored on the build server. Please add:\r\n[Ignore(\"This test should be ignored except locally\")]");
            }

            FillLocalizableStrings();
            FillTitles();
            CreateYamlFile();
        }

        private void FillTitles()
        {
            string titles = @"About Us
Assembly Instructions
Calendar
Donations
History
History of Bed Brigade
Home
Inventory
Locations
Partners";

            string[] titleArray = titles.Split('\r','\n',StringSplitOptions.RemoveEmptyEntries);
            foreach (string title in titleArray)
            {
                string trimmed = title.Trim();
                string key = _translateLogic.ComputeSHA512Hash(trimmed);
                if (!_localizableStrings.ContainsKey(key))
                {
                    _localizableStrings.Add(key, trimmed);
                }
                else
                {
                    if (_localizableStrings[key] != trimmed)
                    {
                        throw new ArgumentException(
                            $"Duplicate key found with different values. Key: {key}. Value1: {_localizableStrings[key]}. Value2: {trimmed}");
                    }
                }
            }
        }

        private void CreateYamlFile()
        {
            string filePath = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Data", "Data", "Seeding", "SeedTranslations", "en-US.yml");
            var newSerializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            string newContent = newSerializer.Serialize(_localizableStrings);
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write(newContent);
            }
        }

        private void FillLocalizableStrings()
        {
            string htmlPath =
                Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Data", "Data", "Seeding", "SeedHtml");

            string[] files = Directory.GetFiles(htmlPath, "*.html");
            foreach (string file in files)
            {
                if (file.Contains("RockCityPolarisAboutUs.html"))
                    Console.WriteLine("Here");

                string text = File.ReadAllText(file);
                text = _translateLogic.CleanUpSpacesAndLineFeedsFromHtml(text);

                List<ParseResult> parseResults = _parseLogic.GetLocalizableStringsInText(text);

                foreach (ParseResult parseResult in parseResults)
                {
                    string key = _translateLogic.ComputeSHA512Hash(parseResult.LocalizableString);
                    if (!_localizableStrings.ContainsKey(key))
                    {
                        _localizableStrings.Add(key, parseResult.LocalizableString);
                    }
                    else
                    {
                        if (_localizableStrings[key] != parseResult.LocalizableString)
                        {
                            throw new ArgumentException(
                                $"Duplicate key found with different values. Key: {key}. Value1: {_localizableStrings[key]}. Value2: {parseResult.LocalizableString}");
                        }
                    }
                }
            }
        }


    }
}
