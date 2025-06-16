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
Partners
Andrew and John
Jayce
Jasmine and Makayla
Jasawn and Javion
Maddox
Capital University - Kappa Sigma Fraternity
Eagle Scout Project Builds Beds for Refugees
A New Bed and Teddy Bear for William
Ana, Ulise and Ivon receive new beds
Ohio Christian University Women's Volleyball Serve Day
LifePoint Women's Bible Study
The Naz 100 Bed Blitz
Grace Fellowship Build
Advent Lutheran Serves
Covenant Church Serves
Meta Church Builds Beds for The Lord
National Stories
National News
Grove City Bed Brigade Stories
Grove City Bed Brigade News
Rock City Polaris Bed Brigade Stories
Rock City Polaris Bed Brigade News
New Bed Brigade Columbus Website
Meet The President
2020 Community Leadership Award
Bed Brigade Featured on Spectrum 1 News
";

            string[] titleArray = titles.Split(new char[] {'\r','\n'},StringSplitOptions.RemoveEmptyEntries);
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
                //if (file.Contains("New-Bed-Brigade-Columbus-Website.html"))
                //    Console.WriteLine("Here");

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
