using Microsoft.AspNetCore.DataProtection;
using Serilog;

namespace BedBrigade.Client;

public static class DataProtectionKeyDirectoryLogic
{
    private const string DataProtectionApplicationName = "BedBrigadeNational";
    private const string DataProtectionKeysDirectoryName = "DataProtection-Keys";
    private const string DataProtectionKeysPathConfigKey = "DataProtection:KeysPath";
    private const string DataProtectionKeysPathEnvironmentVariable = "BedBrigadeDataProtectionKeysPath";

    public static void Configure(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Log.Logger.Information("ConfigureDataProtection");
        var dataProtectionBuilder = builder.Services
            .AddDataProtection()
            .SetApplicationName(DataProtectionApplicationName);

        DirectoryInfo? keysDirectory = TryResolveDataProtectionKeysDirectory(builder);
        if (keysDirectory is null)
        {
            Log.Logger.Warning("Falling back to the ASP.NET Core default data protection key storage. Configure {ConfigKey} or {EnvironmentVariable} if you need a specific persistent location.",
                DataProtectionKeysPathConfigKey,
                DataProtectionKeysPathEnvironmentVariable);
            return;
        }

        dataProtectionBuilder.PersistKeysToFileSystem(keysDirectory);
        Log.Logger.Information("ASP.NET Core data protection keys persisted to {KeysPath}", keysDirectory.FullName);
    }

    private static DirectoryInfo? TryResolveDataProtectionKeysDirectory(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        foreach (string candidatePath in GetDataProtectionKeysPathCandidates(builder))
        {
            if (TryCreateDataProtectionKeysDirectory(candidatePath, out DirectoryInfo? keysDirectory, out Exception? exception))
            {
                return keysDirectory;
            }

            Log.Logger.Warning(exception,
                "Unable to use data protection key directory candidate {KeysPath}. Trying the next candidate.",
                candidatePath);
        }

        return null;
    }

    private static IEnumerable<string> GetDataProtectionKeysPathCandidates(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        List<string> candidates = [];
        AddCandidate(candidates, GetConfiguredDataProtectionKeysPath(builder));
        AddCandidate(candidates, CombineWithPathRoot(Environment.GetEnvironmentVariable("HOME")));
        AddCandidate(candidates, CombineWithPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));
        AddCandidate(candidates, CombineWithPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)));
        AddCandidate(candidates, Path.Combine(builder.Environment.ContentRootPath, "App_Data", DataProtectionKeysDirectoryName));
        AddCandidate(candidates, Path.Combine(builder.Environment.ContentRootPath, DataProtectionKeysDirectoryName));
        AddCandidate(candidates, Path.Combine(Path.GetTempPath(), DataProtectionApplicationName, DataProtectionKeysDirectoryName));
        return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? GetConfiguredDataProtectionKeysPath(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        string? configuredPath = Environment.GetEnvironmentVariable(DataProtectionKeysPathEnvironmentVariable);
        configuredPath ??= builder.Configuration[DataProtectionKeysPathConfigKey];
        return string.IsNullOrWhiteSpace(configuredPath)
            ? null
            : NormalizeDataProtectionKeysPath(builder, configuredPath);
    }

    private static string? CombineWithPathRoot(string? rootPath)
    {
        return string.IsNullOrWhiteSpace(rootPath)
            ? null
            : Path.Combine(rootPath, "ASP.NET", DataProtectionKeysDirectoryName, DataProtectionApplicationName);
    }

    private static bool TryCreateDataProtectionKeysDirectory(string path, out DirectoryInfo? keysDirectory, out Exception? exception)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            keysDirectory = Directory.CreateDirectory(path);
            exception = null;
            return true;
        }
        catch (Exception ex)
        {
            keysDirectory = null;
            exception = ex;
            return false;
        }
    }

    private static string NormalizeDataProtectionKeysPath(WebApplicationBuilder builder, string configuredPath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredPath);

        string expandedPath = configuredPath.Trim();
        if (expandedPath == "~")
        {
            expandedPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        else if (expandedPath.StartsWith("~/", StringComparison.Ordinal))
        {
            string relativeToHome = expandedPath[2..].Replace('/', Path.DirectorySeparatorChar);
            expandedPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), relativeToHome);
        }

        return Path.IsPathRooted(expandedPath)
            ? Path.GetFullPath(expandedPath)
            : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, expandedPath));
    }

    private static void AddCandidate(ICollection<string> candidates, string? candidatePath)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (!string.IsNullOrWhiteSpace(candidatePath))
        {
            candidates.Add(candidatePath);
        }
    }
}

