using System.Text;

namespace BedBrigade.SpeakIt.Tests
{
    public class ParseTests
    {
        private ParseLogic _logic = new ParseLogic();

        [Test]
        public void ParseSpan()
        {
            //Arrange
            string html = "<p><span>No images were found</span></p>";
            string expected = "No images were found";

            //Act
            var result = _logic.GetLocalizableStringsInText(html);

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
            var result = _logic.GetLocalizableStringsInText(html);

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
            var result = _logic.GetLocalizableStringsInText(html);

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
            var result = _logic.GetLocalizableStringsInText(html);

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
            var result = _logic.GetLocalizableStringsInText(html);

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
            var result = _logic.GetLocalizableStringsInText(html);

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
            var result = _logic.GetLocalizableStringsInText(html);

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
            var result = _logic.GetLocalizableStringsInText(html);

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
            var result = _logic.GetLocalizableStringsInText(html);

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
            var result = _logic.GetLocalizableStringsInText(html);

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
            var result = _logic.GetLocalizableStringsInText(html);

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

            SpeakItParms parms = new SpeakItParms();
            parms.SourceDirectories = TestHelper.GetSourceDirectories();
            parms.WildcardPatterns = TestHelper.WildcardPatterns;
            parms.ExcludeDirectories = TestHelper.ExcludeDirectories;
            parms.ExcludeFiles = TestHelper.ExcludeFiles;
            parms.ResourceFilePath = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Client", "Resources", "en-US.yml");
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

            SpeakItParms parms = new SpeakItParms();
            string htmlDirectory = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Data", "Data", "Seeding", "SeedHtml");
            parms.SourceDirectories = new List<string>() { htmlDirectory };
            parms.WildcardPatterns = new List<string>() { "*.html" };
            parms.ResourceFilePath = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Client", "Resources", "en-US.yml");
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

        [Test]
        public void ParseH4WithSpaces()
        {
            string input = @"    <section id=""nat-our-mission-section""
             class=""container-fluid py-5""
             style=""background-color: #f8f9fa"">
        <div class=""container"">
            <div class=""row justify-content-center"">
                <div class=""col-lg-8 text-center"">
                    <h2 class=""display-4 mb-4 wow fadeInUp"">NUESTRA MISIÓN</h2>
                    <h4 class=""mb-4 wow fadeInUp""
                        style=""font-style: italic; color: #495057"">
                        We are compelled by the love of Christ to impact the Kingdom of
                        God by serving those in need. We will not give up in doing good.
                    </h4>
                    <p class=""mb-4 wow fadeInUp"" style=""color: #6c757d"">
                        Bed Brigade is a non-profit faith based charity that builds and
                        delivers beds for those in need of a bed. We want you to know
                        that God sees the situation that you are in. God knows who you
                        are. God loves you.
                    </p>
                    <div class=""wow fadeInUp mb-4"">
                        <img src=""media/national/pages/home/YouAreLoved.webp""
                             alt=""You Are Loved""
                             class=""img-fluid rounded-10 shadow""
                             style=""max-width: 100%; height: auto"" />
                    </div>
                    <a href=""/National/AboutUs"" class=""btn btn-primary wow fadeInUp"">Más Sobre Nosotros</a>
                </div>
            </div>
        </div>
    </section>";

            //Act
            List<ParseResult> result = _logic.GetLocalizableStringsInText(input);

            //Assert
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.Any(o => o.LocalizableString.Contains("We are compelled")));

        }

        [Test]

        public void LiWithIcon()
        {
            string input = @"        <div class=""row"">
            <!-- Left Column: Subtitle with Bullet Points -->
            <div class=""col-md-6 wow fadeInUp"">
                <h3 class=""mb-3"">Estamos marcando la diferencia una cama a la vez</h3>
                <ul class=""list-unstyled"">
                    <li>
                        <i class=""fas fa-check-circle me-2""></i> Do you or your child
                        need a good nights sleep?
                    </li>
                    <li>
                        <i class=""fas fa-check-circle me-2""></i> Do you or your child
                        want to be rested so that they can be all that God created them
                        to be?
                    </li>
                    <li>
                        <i class=""fas fa-check-circle me-2""></i> We are ready to serve
                        you
                    </li>
                </ul>
                <a class=""btn btn-primary"" href=""/request-bed"">Solicitar una cama</a>
                <div class=""spacer-single""></div>
            </div>

            <!-- Right Column: Image -->
            <div class=""col-md-6 text-center wow fadeInRight"">
                <img src=""media/national/pages/home/RequestABedRotator/20231007_103601.webp""
                     width=""300px""
                     alt=""Bed Brigade""
                     class=""img-fluid rounded-10"" />
            </div>
        </div>";

            //Act
            List<ParseResult> result = _logic.GetLocalizableStringsInText(input);

            //Assert
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.Any(o => o.LocalizableString.Contains("Do you or your child")));

        }

        [Test]

        public void PWithSpace()
        {
            string input = @"    <section id=""nat-our-mission-section""
             class=""container-fluid py-5""
             style=""background-color: #f8f9fa"">
        <div class=""container"">
            <div class=""row justify-content-center"">
                <div class=""col-lg-8 text-center"">
                    <h2 class=""display-4 mb-4 wow fadeInUp"">NUESTRA MISIÓN</h2>
                    <h4 class=""mb-4 wow fadeInUp""
                        style=""font-style: italic; color: #495057"">
                        We are compelled by the love of Christ to impact the Kingdom of
                        God by serving those in need. We will not give up in doing good.
                    </h4>
                    <p class=""mb-4 wow fadeInUp"" style=""color: #6c757d"">
                        Bed Brigade is a non-profit faith based charity that builds and
                        delivers beds for those in need of a bed. We want you to know
                        that God sees the situation that you are in. God knows who you
                        are. God loves you.
                    </p>
                    <div class=""wow fadeInUp mb-4"">
                        <img src=""media/national/pages/home/YouAreLoved.webp""
                             alt=""You Are Loved""
                             class=""img-fluid rounded-10 shadow""
                             style=""max-width: 100%; height: auto"" />
                    </div>
                    <a href=""/National/AboutUs"" class=""btn btn-primary wow fadeInUp"">Más Sobre Nosotros</a>
                </div>
            </div>
        </div>
    </section>";

            //Act
            List<ParseResult> result = _logic.GetLocalizableStringsInText(input);

            //Assert
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.Any(o => o.LocalizableString.Contains("faith based charity")));

        }

        [Test]
        public void ParseHomePage()
        {
            string filePath = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Data", "Data", "Seeding", "SeedHtml", "Home.html");
            string html = File.ReadAllText(filePath);

            //Act
            List<ParseResult> result = _logic.GetLocalizableStringsInText(html);

            //Assert
            Assert.IsTrue(result.Count > 0);
            Assert.IsTrue(result.Any(o => o.LocalizableString.StartsWith("Do you or your child") && o.LocalizableString.EndsWith("need a good nights sleep?") ));
        }

        [Test]
        public void GroveCityHomePageShouldNotHaveIdEqual()
        {
            string filePath = Path.Combine(TestHelper.GetSolutionPath(), "BedBrigade.Data", "Data", "Seeding", "SeedHtml", "GroveCityHome.html");
            string html = File.ReadAllText(filePath);

            //Act
            List<ParseResult> result = _logic.GetLocalizableStringsInText(html);

            //Assert
            Assert.IsTrue(result.Count > 0);

            foreach (var parseResult in result)
            {
                Console.WriteLine(parseResult.LocalizableString);
            }
            Assert.IsFalse(result.Any(o => o.LocalizableString.Contains("id=")));
        }
    }
}