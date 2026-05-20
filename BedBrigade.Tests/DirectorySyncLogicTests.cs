using BedBrigade.Common.Logic;

namespace BedBrigade.Tests;

public class DirectorySyncLogicTests
{
    private string _sourceRoot = string.Empty;
    private string _targetRoot = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _sourceRoot = Path.Combine(Path.GetTempPath(), $"BedBrigadeSource_{Guid.NewGuid():N}");
        _targetRoot = Path.Combine(Path.GetTempPath(), $"BedBrigadeTarget_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_sourceRoot);
        Directory.CreateDirectory(_targetRoot);
    }

    [TearDown]
    public void TearDown()
    {
        DeleteDirectoryIfExists(_sourceRoot);
        DeleteDirectoryIfExists(_targetRoot);
    }

    [Test]
    public void CopyMissingFilesAndDirectories_ShouldCopyMissingNestedSeedFiles_WithoutOverwritingExistingFiles()
    {
        string existingTargetFile = CreateFile(_targetRoot, ["national"], "logo.png", "existing-logo");
        CreateFile(_sourceRoot, ["national"], "logo.png", "seed-logo");
        string copiedFile = CreateFile(_sourceRoot, ["polaris", "pages", "Home", "carousel"], "1.webp", "carousel-1");

        DirectorySyncLogic.CopyMissingFilesAndDirectories(_sourceRoot, _targetRoot);

        Assert.Multiple(() =>
        {
            Assert.That(File.ReadAllText(existingTargetFile), Is.EqualTo("existing-logo"));
            Assert.That(File.Exists(Path.Combine(_targetRoot, "polaris", "pages", "Home", "carousel", "1.webp")), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(_targetRoot, "polaris", "pages", "Home", "carousel", "1.webp")), Is.EqualTo(File.ReadAllText(copiedFile)));
        });
    }

    private static string CreateFile(string rootDirectory, string[] directorySegments, string fileName, string content)
    {
        string directory = directorySegments.Length == 0
            ? rootDirectory
            : Path.Combine(rootDirectory, Path.Combine(directorySegments));
        Directory.CreateDirectory(directory);
        string filePath = Path.Combine(directory, fileName);
        File.WriteAllText(filePath, content);
        return filePath;
    }

    private static void DeleteDirectoryIfExists(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }
}


