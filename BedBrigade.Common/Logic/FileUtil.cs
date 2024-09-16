using BedBrigade.Common.Models;
using System.Text;
using BedBrigade.Common.Constants;

namespace BedBrigade.Common.Logic
{
    public static class FileUtil
    {
        public static string GetMediaDirectory(string directoryName)
        {
            directoryName = directoryName.TrimStart('/').TrimStart('\\');
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            baseDirectory = GetPathBeforeBin(baseDirectory);
            string result = Path.Combine(baseDirectory, "wwwroot", "Media", directoryName);
            return result;
        }

        private static string GetPathBeforeBin(string filePath)
        {
            string searchText = Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar;

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
            string localFilePath = "../BedBrigade.Data/Data/Seeding";

            if (Directory.Exists(localFilePath))
            {
                return localFilePath;
            }

           // Debug.WriteLine("Directory does not exist: " + localFilePath);

            string deployedFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Seeding");

            if (Directory.Exists(deployedFilePath))
            {
                return deployedFilePath;
            }

            Console.WriteLine("Directory does not exist: " + deployedFilePath);

            throw new DirectoryNotFoundException("Seeding directory not found. Current directory is : " + AppDomain.CurrentDomain.BaseDirectory);
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
                    throw new Exception("Could not find solution path for " + AppDomain.CurrentDomain.BaseDirectory);

                currentPath = parentPath;
                count++;
            }

            throw new Exception("Reached Max Depth. Could not find solution path for " + AppDomain.CurrentDomain.BaseDirectory);
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
    }
}
