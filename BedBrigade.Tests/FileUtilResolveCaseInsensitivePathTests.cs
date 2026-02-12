using BedBrigade.Common.Logic;
using NUnit.Framework;

namespace BedBrigade.Tests
{
    [TestFixture]
    public class FileUtilResolveCaseInsensitivePathTests
    {
        private string _tempRoot = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "BedBrigade_FileUtil_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, true);
            }
        }

        [Test]
        public void ResolveCaseInsensitivePath_DirectoryExistsWithDifferentCase_ReturnsResolvedDirectory()
        {
            string actual = Path.Combine(_tempRoot, "Media", "National", "pages", "Home", "heroImageRotator");
            Directory.CreateDirectory(actual);

            string input = Path.Combine(_tempRoot, "media", "national", "PAGES", "home", "heroimagerotator");
            string? resolved = FileUtil.ResolveCaseInsensitivePath(input);

            Assert.That(resolved, Is.Not.Null);
            Assert.That(Path.GetFullPath(resolved!), Is.EqualTo(Path.GetFullPath(actual)));
        }

        [Test]
        public void ResolveCaseInsensitivePath_MixedSeparators_ReturnsResolvedDirectory()
        {
            string actual = Path.Combine(_tempRoot, "Media", "National", "pages", "Home", "heroImageRotator");
            Directory.CreateDirectory(actual);

            // Intentionally use backslashes in the middle to mimic Windows-style input on Linux.
            string input = $"{_tempRoot}\\media\\national\\pages\\HOME\\heroImageRotator";
            string? resolved = FileUtil.ResolveCaseInsensitivePath(input);

            Assert.That(resolved, Is.Not.Null);
            Assert.That(Path.GetFullPath(resolved!), Is.EqualTo(Path.GetFullPath(actual)));
        }

        [Test]
        public void ResolveCaseInsensitivePath_FileExistsWithDifferentCase_ReturnsResolvedFile()
        {
            string dir = Path.Combine(_tempRoot, "Media", "National");
            Directory.CreateDirectory(dir);

            string actualFile = Path.Combine(dir, "Index.HTML");
            File.WriteAllText(actualFile, "test");

            string input = Path.Combine(_tempRoot, "media", "national", "index.html");
            string? resolved = FileUtil.ResolveCaseInsensitivePath(input);

            Assert.That(resolved, Is.Not.Null);
            Assert.That(Path.GetFullPath(resolved!), Is.EqualTo(Path.GetFullPath(actualFile)));
        }

        [Test]
        public void ResolveCaseInsensitivePath_TrailingSeparator_RequiresDirectory()
        {
            string actual = Path.Combine(_tempRoot, "Media", "National");
            Directory.CreateDirectory(actual);

            string input = Path.Combine(_tempRoot, "media", "national") + Path.DirectorySeparatorChar;
            string? resolved = FileUtil.ResolveCaseInsensitivePath(input);

            Assert.That(resolved, Is.Not.Null);
            Assert.That(Path.GetFullPath(resolved!), Is.EqualTo(Path.GetFullPath(actual)));
        }

        [Test]
        public void ResolveCaseInsensitivePath_NotFound_ReturnsNull()
        {
            string input = Path.Combine(_tempRoot, "does-not-exist", "nope");
            string? resolved = FileUtil.ResolveCaseInsensitivePath(input);
            Assert.That(resolved, Is.Null);
        }
    }
}

