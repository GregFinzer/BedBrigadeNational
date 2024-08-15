using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        /// <summary>
        /// Returns the path to the file contained in the specified foldername
        /// </summary>
        /// <param name="fileName">The name of the file to find</param>
        /// <param name="folderName">A string terminating in a folder to be searched (Note this may be a path of folders)</param>
        /// <returns>A string containing where the file was found</returns>
        public static string ToApplicationPath(string fileName, string folderName = "")
        {
            string appRoot = GetMediaDirectory(folderName);
            return Path.Combine(appRoot, fileName);
        }


        public static bool MediaSubDirectoryExists(string directoryName)
        {
            var appRoot = GetMediaDirectory(directoryName);
            return Directory.Exists(appRoot);
        }

        public static DirectoryInfo CreateMediaSubDirectory(string directoryName)
        {
            var appRoot = GetMediaDirectory(directoryName);
            return Directory.CreateDirectory(appRoot);
        }

        public static void DeleteMediaSubDirectory(string directoryName, bool recursiveDelete)
        {
            var appRoot = GetMediaDirectory(directoryName);
            Directory.Delete(appRoot, recursiveDelete);
        }

        public static void DeleteMediaFiles(string directoryName)
        {
            var appRoot = GetMediaDirectory(directoryName);
            string[] files = Directory.GetFiles(appRoot);
            foreach (string file in files)
            {
                File.Delete(file);
            }
        }
        public static void CreateDirectory(string targetDir)
        {
            Directory.CreateDirectory(targetDir);
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
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
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

            Console.WriteLine("Directory does not exist: " + localFilePath);

            string deployedFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Seeding");

            if (Directory.Exists(deployedFilePath))
            {
                return deployedFilePath;
            }

            Console.WriteLine("Directory does not exist: " + deployedFilePath);

            throw new DirectoryNotFoundException("Seeding directory not found. Current directory is : " + AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}
