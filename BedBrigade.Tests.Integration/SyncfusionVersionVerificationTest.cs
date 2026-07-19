using NUnit.Framework;
using System.Text.RegularExpressions;
using System.Xml;

namespace BedBrigade.Tests.Integration;

/// <summary>
/// Integration test to verify Syncfusion package version matches CDN versions in App.razor.
/// This test detects version mismatches between the NuGet package and CDN resources.
/// </summary>
[TestFixture]
public class SyncfusionVersionVerificationTest
{
    private const string ClientProjectName = "BedBrigade.Client";
    private const string AppRazorFile = "Components/App.razor";
    private const string SyncfusionPackageName = "Syncfusion.Blazor";

    /// <summary>
    /// Gets the solution root directory by traversing up from the test assembly location.
    /// </summary>
    private static string GetSolutionRoot()
    {
        string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

        while (!string.IsNullOrEmpty(currentDirectory))
        {
            if (Directory.Exists(Path.Combine(currentDirectory, ".git")))
            {
                return currentDirectory;
            }

            currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not find solution root directory with .git folder");
    }

    /// <summary>
    /// Extracts the Syncfusion.Blazor package version from BedBrigade.Client.csproj.
    /// </summary>
    private static string ExtractNuGetPackageVersion()
    {
        string solutionRoot = GetSolutionRoot();
        string projectPath = Path.Combine(solutionRoot, ClientProjectName, $"{ClientProjectName}.csproj");

        if (!File.Exists(projectPath))
        {
            throw new FileNotFoundException($"Project file not found: {projectPath}");
        }

        using (XmlReader reader = XmlReader.Create(projectPath))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && 
                    reader.Name == "PackageReference" && 
                    reader.GetAttribute("Include") == SyncfusionPackageName)
                {
                    string? version = reader.GetAttribute("Version");
                    if (!string.IsNullOrEmpty(version))
                    {
                        return version;
                    }
                }
            }
        }

        throw new InvalidOperationException($"Syncfusion.Blazor package version not found in {projectPath}");
    }

    /// <summary>
    /// Extracts the Syncfusion CSS version from the CDN URL in App.razor.
    /// </summary>
    private static string ExtractCssVersion()
    {
        string appRazorContent = ReadAppRazorFile();

        // Pattern: https://cdn.syncfusion.com/blazor/VERSION/styles/bootstrap5.css
        Match match = Regex.Match(appRazorContent, @"https://cdn\.syncfusion\.com/blazor/([\d.]+)/styles/bootstrap5\.css");

        if (!match.Success || match.Groups.Count < 2)
        {
            throw new InvalidOperationException("Could not extract Syncfusion CSS version from App.razor");
        }

        return match.Groups[1].Value;
    }

    /// <summary>
    /// Extracts the Syncfusion JavaScript version from the CDN URL in App.razor.
    /// </summary>
    private static string ExtractJavaScriptVersion()
    {
        string appRazorContent = ReadAppRazorFile();

        // Pattern: https://cdn.syncfusion.com/blazor/VERSION/syncfusion-blazor.min.js
        Match match = Regex.Match(appRazorContent, @"https://cdn\.syncfusion\.com/blazor/([\d.]+)/syncfusion-blazor\.min\.js");

        if (!match.Success || match.Groups.Count < 2)
        {
            throw new InvalidOperationException("Could not extract Syncfusion JavaScript version from App.razor");
        }

        return match.Groups[1].Value;
    }

    /// <summary>
    /// Reads the App.razor file content.
    /// </summary>
    private static string ReadAppRazorFile()
    {
        string solutionRoot = GetSolutionRoot();
        string appRazorPath = Path.Combine(solutionRoot, ClientProjectName, AppRazorFile);

        if (!File.Exists(appRazorPath))
        {
            throw new FileNotFoundException($"App.razor file not found: {appRazorPath}");
        }

        return File.ReadAllText(appRazorPath);
    }

    /// <summary>
    /// Test that verifies CSS version matches NuGet package version.
    /// </summary>
    [Test]
    [Description("Ensures that the Syncfusion CSS version in App.razor matches the NuGet package version")]
    public void CssVersion_ShouldMatchNuGetPackageVersion()
    {
        // Arrange
        string nugetVersion = ExtractNuGetPackageVersion();
        string cssVersion = ExtractCssVersion();

        // Act & Assert
        Assert.That(
            cssVersion,
            Is.EqualTo(nugetVersion),
            $"Syncfusion CSS version mismatch. Expected: {nugetVersion}, but found: {cssVersion} in App.razor. " +
            $"Update the CSS URL in App.razor from version {cssVersion} to {nugetVersion}.");
    }

    /// <summary>
    /// Test that verifies JavaScript version matches NuGet package version.
    /// </summary>
    [Test]
    [Description("Ensures that the Syncfusion JavaScript version in App.razor matches the NuGet package version")]
    public void JavaScriptVersion_ShouldMatchNuGetPackageVersion()
    {
        // Arrange
        string nugetVersion = ExtractNuGetPackageVersion();
        string jsVersion = ExtractJavaScriptVersion();

        // Act & Assert
        Assert.That(
            jsVersion,
            Is.EqualTo(nugetVersion),
            $"Syncfusion JavaScript version mismatch. Expected: {nugetVersion}, but found: {jsVersion} in App.razor. " +
            $"Update the JS URL in App.razor from version {jsVersion} to {nugetVersion}.");
    }

    /// <summary>
    /// Test that verifies CSS and JavaScript versions match each other.
    /// </summary>
    [Test]
    [Description("Ensures that the Syncfusion CSS and JavaScript versions both match in App.razor")]
    public void CssAndJavaScriptVersions_ShouldMatch()
    {
        // Arrange
        string cssVersion = ExtractCssVersion();
        string jsVersion = ExtractJavaScriptVersion();

        // Act & Assert
        Assert.That(
            cssVersion,
            Is.EqualTo(jsVersion),
            $"Syncfusion CSS and JavaScript versions don't match in App.razor. " +
            $"CSS version: {cssVersion}, JavaScript version: {jsVersion}. " +
            $"Both should be the same version for consistency.");
    }

    /// <summary>
    /// Test that verifies all three versions (NuGet, CSS, and JavaScript) match.
    /// This is a comprehensive check combining the above tests.
    /// </summary>
    [Test]
    [Description("Comprehensive test ensuring NuGet package, CSS, and JavaScript versions all match")]
    public void AllSyncfusionVersions_ShouldMatch()
    {
        // Arrange
        string nugetVersion = ExtractNuGetPackageVersion();
        string cssVersion = ExtractCssVersion();
        string jsVersion = ExtractJavaScriptVersion();

        // Act & Assert
        Assert.Multiple(() =>
        {
            Assert.That(
                cssVersion,
                Is.EqualTo(nugetVersion),
                $"CSS version {cssVersion} does not match NuGet package version {nugetVersion}");

            Assert.That(
                jsVersion,
                Is.EqualTo(nugetVersion),
                $"JavaScript version {jsVersion} does not match NuGet package version {nugetVersion}");

            Assert.That(
                cssVersion,
                Is.EqualTo(jsVersion),
                $"CSS version {cssVersion} does not match JavaScript version {jsVersion}");
        });
    }
}
