using BedBrigade.Common.Models;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
