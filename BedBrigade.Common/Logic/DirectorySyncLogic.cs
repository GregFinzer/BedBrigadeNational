namespace BedBrigade.Common.Logic;

public static class DirectorySyncLogic
{
    public static void CopyMissingFilesAndDirectories(string sourceDirectory, string targetDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetDirectory);

        if (!Directory.Exists(sourceDirectory))
        {
            throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDirectory}");
        }

        Directory.CreateDirectory(targetDirectory);

        foreach (string sourceFilePath in Directory.EnumerateFiles(sourceDirectory))
        {
            string targetFilePath = Path.Combine(targetDirectory, Path.GetFileName(sourceFilePath));
            if (!File.Exists(targetFilePath))
            {
                File.Copy(sourceFilePath, targetFilePath);
            }
        }

        foreach (string sourceSubdirectoryPath in Directory.EnumerateDirectories(sourceDirectory))
        {
            string targetSubdirectoryPath = Path.Combine(targetDirectory, Path.GetFileName(sourceSubdirectoryPath));
            CopyMissingFilesAndDirectories(sourceSubdirectoryPath, targetSubdirectoryPath);
        }
    }
}

