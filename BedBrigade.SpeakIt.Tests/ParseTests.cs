using System.Text;

namespace BedBrigade.SpeakIt.Tests
{
    public class ParseTests
    {
        private SpeakItLogic _logic = new SpeakItLogic();

        [Test]
        public void ParseSpan()
        {
            //Arrange
            string html = "<p><span>No images were found</span></p>";
            string expected = "No images were found";

            //Act
            var result = _logic.GetLocalizableStringsInHtml(html);

            //Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(expected, result[0].LocalizableString);
        }

        [Test]
        public void ParseAnchor()
        {
            //Arrange
            string html = "<a href=\"/national/donations\">donate today</a>";
            string expected = "donate today";

            //Act
            var result = _logic.GetLocalizableStringsInHtml(html);

            //Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(expected, result[0].LocalizableString);
        }

        [Test]
        public void ParseAnchorInsideAListItem()
        {
            //Arrange
            string html = "<li><a href=\"/national/donations\">donate today</a></li>";
            string expected = "donate today";

            //Act
            var result = _logic.GetLocalizableStringsInHtml(html);

            //Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(expected, result[0].LocalizableString);
        }

        [Test]
        public void ParseListItem()
        {
            //Arrange
            string html = "<li>New Pillows</li>";
            string expected = "New Pillows";

            //Act
            var result = _logic.GetLocalizableStringsInHtml(html);

            //Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(expected, result[0].LocalizableString);
        }

        [Test]
        public void ParseButton()
        {
            //Arrange
            string html = "<button class=\"navbar-toggler navbar-toggler-right\" type=\"button\" data-bs-toggle=\"collapse\" data-bs-target=\"#navbarResponsive\" aria-controls=\"navbarResponsive\" aria-expanded=\"false\" aria-label=\"Toggle navigation\">Menu <i class=\"fas fa-bars ms-1\"></i></button>";
            string expected = "Menu";

            //Act
            var result = _logic.GetLocalizableStringsInHtml(html);

            //Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(expected, result[0].LocalizableString);
        }

        [Test]
        public void ParseHeading()
        {
            //Arrange
            string html = "<h1>Our Mission</h1>";
            string expected = "Our Mission";

            //Act
            var result = _logic.GetLocalizableStringsInHtml(html);

            //Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(expected, result[0].LocalizableString);
        }

        [Test]
        public void BuriedImage()
        {
            //Arrange
            string html =
                @"<p style=""text-align: center;""><br></p><p style=""text-align: center;""><a href=""https://www.stjohnsgc.org/""><img src=""/media/grove-city/pages/Partners/StJohnsChurch.png""></a></p>";

            //Act
            var result = _logic.GetLocalizableStringsInHtml(html);

            //Assert
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void ParseParagraph()
        {
            //Arrange
            string html = "<p>If you like to volunteer with Bed Brigade, please select our Location and complete the Volunteer registration form</p>";
            string expected = "If you like to volunteer with Bed Brigade, please select our Location and complete the Volunteer registration form";

            //Act
            var result = _logic.GetLocalizableStringsInHtml(html);

            //Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(expected, result[0].LocalizableString);
        }

        [Test]
        public void BuriedSpan()
        {
            string html = @"<div class=""col-12 mb-4 d-none d-md-block wow slideInUp""
	 data-wow-delay=""0.4s"">
	<div><i class=""fas fa-long-arrow-alt-up fa-3x""></i></div>
	<i class=""fas fa-layer-group fa-2x""></i> <span>Bedding</span>
</div>";
            string expected = "Bedding";

            //Act
            var result = _logic.GetLocalizableStringsInHtml(html);

            //Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(expected, result[0].LocalizableString);
        }

        [Test]
        public void HandleBr()
        {
            string html = @"<p>
2024 – Bed Brigade. All rights reserved.<br />
</p>";
            string expected = "2024 – Bed Brigade. All rights reserved.";

            //Act
            var result = _logic.GetLocalizableStringsInHtml(html);

            //Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(expected, result[0].LocalizableString);
        }

        [Test]
        public void ButtonWithOnClick()
        {
            //Arrange
            string html = @"<FooterTemplate>
	<button class=""btn btn-primary"" @onclick=@(() => Save(context as Donation)) IsPrimary=""true"">Save Donation</button>
</FooterTemplate>";
            string expected = "Save Donation";

            //Act
            var result = _logic.GetLocalizableStringsInHtml(html);

            //Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(expected, result[0].LocalizableString);
        }

        [Test]
        public void ParseRazorFiles()
        {
            if (TestHelper.IsRunningUnderGitHubActions())
            {
                Assert.Ignore("This test is not supported in GitHub Actions");
            }

            ParseParms parms = new ParseParms();
            parms.TargetDirectory = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Client", "Components");
            parms.WildcardPattern= "*.razor";
            parms.ExcludeDirectories = TestHelper.ExcludeDirectories;
            parms.ExcludeFiles = TestHelper.ExcludeFiles;
            var result = _logic.GetLocalizableStrings(parms);
            string outputFileName = "RazorFiles.txt";

            if (File.Exists(outputFileName))
                File.Delete(outputFileName);

            StringBuilder sb = new StringBuilder(result.Count * 80);
            foreach (var parseResult in result)
            {
                sb.AppendLine($"{Path.GetFileName(parseResult.FilePath)} | {parseResult.LocalizableString} ");
            }

            File.WriteAllText(outputFileName, sb.ToString());
        }

        [Test]
        public void ParseHtmlFiles()
        {
            if (TestHelper.IsRunningUnderGitHubActions())
            {
                Assert.Ignore("This test is not supported in GitHub Actions");
            }

            ParseParms parms = new ParseParms();
            parms.TargetDirectory = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Data", "Data", "Seeding", "SeedHtml");
            parms.WildcardPattern = "*.html";
            var result = _logic.GetLocalizableStrings(parms);
            string outputFileName = "HtmlFiles.txt";

            if (File.Exists(outputFileName))
                File.Delete(outputFileName);

            StringBuilder sb = new StringBuilder(result.Count * 80);
            foreach (var parseResult in result)
            {
                sb.AppendLine($"{parseResult.FilePath} - {parseResult.LocalizableString}");
            }

            File.WriteAllText(outputFileName, sb.ToString());
        }



    }
}