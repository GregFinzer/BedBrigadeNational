﻿using BedBrigade.Common.Models;
using System.Text;
using BedBrigade.Common.Constants;

namespace BedBrigade.Common.Logic
{
    public static class FileUtil
    {
        private static Dictionary<string, string> _caseInsensitiveCache = new Dictionary<string, string>();
        private const string BinDirectoryName = "bin";
        private const string DataDirectoryName = "Data";
        private const string LocalDirectoryName = ".local";
        private const string SeedingDirectoryName = "Seeding";
        
        public static string BuildFileNameWithDate(string prefix, string extension)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                prefix = "File";
            }
            if (string.IsNullOrEmpty(extension))
            {
                extension = ".txt";
            }
            string datePart = DateTime.Now.ToString("yyyy-MM-dd");
            return $"{prefix}_{datePart}{extension}";
        }

        public static string GetMediaDirectory(string directoryName)
        {
            directoryName = directoryName.TrimStart('/').TrimStart('\\');
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            baseDirectory = GetPathBeforeBin(baseDirectory);
            return MediaPathUtil.GetMediaDirectory(baseDirectory, directoryName);
        }

        private static string GetPathBeforeBin(string filePath)
        {
            string searchText = Path.DirectorySeparatorChar + BinDirectoryName + Path.DirectorySeparatorChar;

            int index = filePath.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);

            if (index != -1)
            {
                return filePath.Substring(0, index);
            }
            else
            {
                // Return the entire path if "/bin/" is not found or some other default action
                return filePath;
            }
        }


        public static DirectoryInfo CreateMediaSubDirectory(string directoryName)
        {
            var appRoot = GetMediaDirectory(directoryName);
            return Directory.CreateDirectory(appRoot);
        }

        public static void DeleteMediaSubDirectory(string directoryName, bool recursiveDelete)
        {
            var appRoot = GetMediaDirectory(directoryName);
            if (Directory.Exists(appRoot)) {                        
                Directory.Delete(appRoot, recursiveDelete);
            }
        } 

        public static void DeleteDirectory(string targetDir)
        {
            File.SetAttributes(targetDir, FileAttributes.Normal);

            string[] files = Directory.GetFiles(targetDir);
            string[] dirs = Directory.GetDirectories(targetDir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(targetDir, false);
        }

        public static void CopyDirectory(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            if (!Directory.Exists(target.FullName))
            {
                Directory.CreateDirectory(target.FullName);
            }

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                //Debug.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                Console.WriteLine($"Create sub dir {diSourceSubDir.Name}");
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        public static string GetSeedingDirectory()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            baseDirectory = GetPathBeforeBin(baseDirectory);
            List<string> candidatePaths =
            [
                Path.Combine(baseDirectory, "BedBrigade.Data", DataDirectoryName, SeedingDirectoryName),
                Path.Combine(baseDirectory, DataDirectoryName, SeedingDirectoryName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DataDirectoryName, SeedingDirectoryName),
                Path.Combine(Environment.CurrentDirectory, "BedBrigade.Data", DataDirectoryName, SeedingDirectoryName)
            ];

            try
            {
                candidatePaths.Insert(0, Path.Combine(GetSolutionPath(), "BedBrigade.Data", DataDirectoryName, SeedingDirectoryName));
            }
            catch (DirectoryNotFoundException)
            {
                // Ignore solution lookup failures and continue with deployment/runtime fallbacks.
            }

            foreach (string candidatePath in candidatePaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string? resolvedPath = ResolveCaseInsensitivePath(candidatePath);
                if (!string.IsNullOrWhiteSpace(resolvedPath) && Directory.Exists(resolvedPath))
                {
                    return resolvedPath;
                }
            }

            throw new DirectoryNotFoundException(
                $"Seeding directory not found. Tried: {string.Join(", ", candidatePaths)}. Current directory is: {AppDomain.CurrentDomain.BaseDirectory}");
        }

        public static void CreateLocationMediaDirectory(Location location)
        {
            var locationMediaDirectory = FileUtil.GetMediaDirectory(location.Route);

            if (!Directory.Exists(locationMediaDirectory))
            {
                Directory.CreateDirectory(locationMediaDirectory);
            }

            var locationMediaPagesDirectory = Path.Combine(locationMediaDirectory, Defaults.PagesDirectory);

            if (!Directory.Exists(locationMediaPagesDirectory))
            {
                Directory.CreateDirectory(locationMediaPagesDirectory);
            }
        }

        public static void CopyMediaFromLocation(Location sourceLocation, Location destLocation, string directory)
        {
            var sourceDirectory = Path.Combine(FileUtil.GetMediaDirectory(sourceLocation.Route), Defaults.PagesDirectory, directory);
            var destinationDirectory = Path.Combine(FileUtil.GetMediaDirectory(destLocation.Route), Defaults.PagesDirectory, directory);

            if (!Directory.Exists(sourceDirectory))
            {
                throw new DirectoryNotFoundException($"Directory {sourceDirectory} does not exist");
            }

            FileUtil.CopyDirectory(sourceDirectory, destinationDirectory);
        }

        /// <summary>
        /// Returns a valid filename, ignoring invalid characters
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="allowSpaces">If false, spaces will be turned into underscores</param>
        /// <returns></returns>
        public static string FilterFileName(string fileName, bool allowSpaces= true)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            StringBuilder sb = new StringBuilder(fileName.Length);
            string currentChar;
            string sInvalid = "";

            for (int i = 0; i < System.IO.Path.GetInvalidFileNameChars().GetUpperBound(0); i++)
                sInvalid += System.IO.Path.GetInvalidFileNameChars()[i].ToString();

            for (int i = 0; i < System.IO.Path.GetInvalidPathChars().GetUpperBound(0); i++)
                sInvalid += System.IO.Path.GetInvalidPathChars()[i].ToString();

            sInvalid += System.IO.Path.VolumeSeparatorChar.ToString();
            sInvalid += System.IO.Path.PathSeparator.ToString();
            sInvalid += System.IO.Path.DirectorySeparatorChar.ToString();
            sInvalid += System.IO.Path.AltDirectorySeparatorChar.ToString();

            for (int i = 0; i < fileName.Length; i++)
            {
                currentChar = fileName.Substring(i, 1);

                if (!allowSpaces && currentChar == " ")
                    currentChar = "_";

                if (currentChar == "," || currentChar == "'")
                {
                    continue;
                }

                if (sInvalid.IndexOf(currentChar) < 0)
                {
                    sb.Append(currentChar);
                }
            }

            return sb.ToString();
        }

        public static string GetSolutionPath()
        {
            string currentPath = AppDomain.CurrentDomain.BaseDirectory;
            int count = 0;
            const int maxDepth = 1000;

            while (count < maxDepth)
            {
                string[] files = Directory.GetFiles(currentPath, "*.sln");

                if (files.Any())
                {
                    return currentPath;
                }

                string? parentPath = Path.GetDirectoryName(currentPath);

                //We are at the root we did not find anything
                if (parentPath == null || parentPath == currentPath)
                    throw new DirectoryNotFoundException("Could not find solution path for " + AppDomain.CurrentDomain.BaseDirectory);

                currentPath = parentPath;
                count++;
            }

            throw new DirectoryNotFoundException("Reached Max Depth. Could not find solution path for " + AppDomain.CurrentDomain.BaseDirectory);
        }

        public static bool AnyCSharpFilesModifiedToday(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");
            }

            var today = DateTime.Today;
            var files = Directory.EnumerateFiles(directoryPath, "*.cs", SearchOption.AllDirectories);

            return files.Any(file => File.GetLastWriteTime(file).Date == today);
        }

        public static bool AnyHtmlFilesModifiedToday(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"The directory {directoryPath} does not exist.");
            }

            var today = DateTime.Today;
            var files = Directory.EnumerateFiles(directoryPath, "*.html", SearchOption.AllDirectories);

            return files.Any(file => File.GetLastWriteTime(file).Date == today);
        }

        /// <summary>
        /// Extract the path from a path ending in a filename 
        /// </summary>
        /// <param name="fullPath">A fully qualified path ending in a filename</param>
        /// <returns>The extacted path</returns>
        public static string ExtractPath(string fullPath)
        {
            if (fullPath.Length == 0)
                throw new ArgumentNullException("fullPath");

            //Account for already in form of path
            if (Path.GetFileName(fullPath).Length == 0
                || Path.GetExtension(fullPath).Length == 0)
            {
                return fullPath;
            }

            return Path.GetDirectoryName(fullPath);
        }

        public static bool IsVSCodeInstalledOnLinux()
        {
            if (!OperatingSystem.IsLinux())
            {
                return false;
            }

            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string[] candidatePaths =
            [
                "/usr/bin/code",
                "/usr/local/bin/code",
                "/snap/bin/code",
                "/usr/bin/code-insiders",
                "/usr/local/bin/code-insiders",
                "/snap/bin/code-insiders",
                "/var/lib/flatpak/exports/bin/com.visualstudio.code",
                "/var/lib/flatpak/exports/bin/com.visualstudio.code-insiders",
                string.IsNullOrWhiteSpace(homeDirectory) ? string.Empty : Path.Combine(homeDirectory, LocalDirectoryName, BinDirectoryName, "code"),
                string.IsNullOrWhiteSpace(homeDirectory) ? string.Empty : Path.Combine(homeDirectory, LocalDirectoryName, BinDirectoryName, "code-insiders"),
                string.IsNullOrWhiteSpace(homeDirectory) ? string.Empty : Path.Combine(homeDirectory, LocalDirectoryName, "share", "flatpak", "exports", BinDirectoryName, "com.visualstudio.code"),
                string.IsNullOrWhiteSpace(homeDirectory) ? string.Empty : Path.Combine(homeDirectory, LocalDirectoryName, "share", "flatpak", "exports", BinDirectoryName, "com.visualstudio.code-insiders")
            ];

            if (candidatePaths.Any(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path)))
            {
                return true;
            }

            string? environmentPath = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(environmentPath))
            {
                return false;
            }

            string[] executableNames = ["code", "code-insiders", "com.visualstudio.code", "com.visualstudio.code-insiders"];

            return environmentPath
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(directory => executableNames.Any(executableName => File.Exists(Path.Combine(directory, executableName))));
        }
        
        public static bool IsVSCodeInstalledOnWindows()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return false;
            }

            var programDirs = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            return programDirs
                .Select(p => Path.Combine(p, "Microsoft VS Code", "Code.exe"))
                .Any(File.Exists);
        }
        
        public static string? ResolveCaseInsensitivePath(string path)
        {
            if (_caseInsensitiveCache.TryGetValue(path, out string? cached))
            {
                if (Directory.Exists(cached) || File.Exists(cached))
                {
                    return cached;
                }

                _caseInsensitiveCache.Remove(path);
            }
            
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            bool endsWithSeparator = EndsWithDirectorySeparator(path);
            string fullPath = GetFullPathOrNull(path);
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return path;
            }

            if (Directory.Exists(fullPath) || File.Exists(fullPath))
            {
                return fullPath;
            }

            string? root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrWhiteSpace(root))
            {
                return path;
            }

            string? resolved = ResolveByEnumeratingSegments(root, fullPath);
            if (string.IsNullOrWhiteSpace(resolved))
            {
                return path;
            }

            string? result = endsWithSeparator
                ? (Directory.Exists(resolved) ? resolved : null)
                : ((Directory.Exists(resolved) || File.Exists(resolved)) ? resolved : null);

            if (!string.IsNullOrWhiteSpace(result))
            {
                _caseInsensitiveCache.Add(path, result);
            }
            return result;
        }

        private static bool EndsWithDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar)
                || path.EndsWith(Path.AltDirectorySeparatorChar)
                || path.EndsWith('/')
                || path.EndsWith('\\');
        }

        private static string GetFullPathOrNull(string path)
        {
            string normalized = path.Replace('\\', Path.DirectorySeparatorChar)
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            try
            {
                return Path.GetFullPath(normalized);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string? ResolveByEnumeratingSegments(string root, string fullPath)
        {
            // IMPORTANT: On Linux, Path.GetPathRoot("/a/b") is "/". We must keep "/" as the current directory.
            string current = root;
            string remainder = fullPath.Substring(root.Length);

            var parts = remainder.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return Directory.Exists(root) ? root : null;
            }

            for (int i = 0; i < parts.Length; i++)
            {
                current = ResolveNextSegment(current, parts[i]);
                if (string.IsNullOrWhiteSpace(current))
                {
                    return null;
                }
            }

            return current;
        }

        private static string ResolveNextSegment(string currentDirectory, string segment)
        {
            if (!Directory.Exists(currentDirectory))
            {
                return string.Empty;
            }

            string? match = Directory.EnumerateFileSystemEntries(currentDirectory)
                .Select(Path.GetFileName)
                .FirstOrDefault(name =>
                    !string.IsNullOrWhiteSpace(name)
                    && string.Equals(name, segment, StringComparison.OrdinalIgnoreCase));

            return match is null ? string.Empty : Path.Combine(currentDirectory, match);
        }
    }
}
