using BedBrigade.Common.Logic;

namespace BedBrigade.Tests
{
    public class MediaPathUtilTests
    {
        private string _testRoot = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), $"BedBrigadeMediaPathTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_testRoot))
            {
                Directory.Delete(_testRoot, true);
            }
        }

        [Test]
        public void ResolveExistingMediaPath_ShouldPreferExactLowercaseMatch_WhenBothMediaRootsExist()
        {
            string lowercaseFile = CreateFile("media", "national", "pages", "home", "banner.webp");
            CreateFile("Media", "national", "pages", "home", "banner.webp");

            string? resolvedPath = MediaPathUtil.ResolveExistingMediaPath(_testRoot, "national/pages/home", "banner.webp");

            Assert.That(resolvedPath, Is.EqualTo(lowercaseFile));
        }

        [Test]
        public void ResolveExistingMediaPath_ShouldFallbackToUppercaseDirectory_WhenNeeded()
        {
            Directory.CreateDirectory(Path.Combine(_testRoot, "wwwroot", "media"));
            string uppercaseFile = CreateFile("Media", "national", "pages", "home", "hero.webp");

            string? resolvedPath = MediaPathUtil.ResolveExistingMediaPath(_testRoot, "national\\pages\\home", "hero.webp");

            Assert.That(resolvedPath, Is.EqualTo(uppercaseFile));
        }

        [Test]
        public void GetMediaDirectory_ShouldCreateLowercaseDirectory_WhenNoMediaFolderExists()
        {
            string createdPath = MediaPathUtil.GetMediaDirectory(_testRoot, "national", "pages", "home");

            Assert.Multiple(() =>
            {
                Assert.That(createdPath, Is.EqualTo(Path.Combine(_testRoot, "wwwroot", "media", "national", "pages", "home")));
                Assert.That(Directory.Exists(createdPath), Is.True);
            });
        }

        private string CreateFile(string mediaFolderName, params string[] segments)
        {
            string directory = Path.Combine(_testRoot, "wwwroot", mediaFolderName, Path.Combine(segments[..^1]));
            Directory.CreateDirectory(directory);
            string filePath = Path.Combine(directory, segments[^1]);
            File.WriteAllText(filePath, "test");
            return filePath;
        }
    }
}

