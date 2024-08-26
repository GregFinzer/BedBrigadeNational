using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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



        public static bool CreateOrValidateLocationFolders(string locationRoute)
        {

           // LocationRoute = LocationRoute.Replace("/", String.Empty);                        
            
            string templateLocationRoot = Path.Combine(GetMediaDirectory("grove-city"),"pages"); // temporary - should be replaced by configuration variable
            // default pages List
            string[] defaultPageFolders = Directory.GetDirectories(templateLocationRoot);
            // Clear page List - remove path


            try
            {
                // Step 1 - Location Root Folder & pages folder
                string locationRoot = GetMediaDirectory(locationRoute);
                string locationPagesPath = Path.Combine(locationRoot, "pages"); // location top default sub-folder
                CreateFolderIfNotExists(locationRoot); // check location folder
                CreateFolderIfNotExists(locationPagesPath); // check "pages" sub-folder                           

                // step 2 - loop in default pages list
                //foreach (string pageFolderName in defaultPageFolders)
                Parallel.ForEach(defaultPageFolders, pageFolderName =>                
                {   // create location page folder
                    string pageName = Path.GetFileName(pageFolderName);
                    string pageFolderPath = Path.Combine(locationPagesPath, pageName);
                    CreateFolderIfNotExists(pageFolderPath);
                    if (pageName == "Home") // special structure
                    {
                        string homeRotatorPath = Path.Combine(pageFolderPath, "headerImageRotator");
                        CreateFolderIfNotExists(homeRotatorPath);
                        if (Directory.GetFiles(homeRotatorPath).Length == 0)
                        {
                            CopyRotatorImages(homeRotatorPath, true);
                        }
                    }
                    else
                    {
                        // Rotator Images Folder
                        CreatePageRotatorFolders(pageFolderPath);                      

                    } // Loop in default pages

                } // end page loop
                ); // end Parallel process

               return (true);

            }
            catch (Exception ex) 
            {
                   // Debug.WriteLine(ex.Message);
                    return(false);
            }            

        } // Validate Location Folders


        private static void CreatePageRotatorFolders(string pageFolderPath)
        {
            string[] RotatorFolders = ["leftImageRotator", "middleImageRotator", "rightImageRotator"];
            // Rotator Images Folder
            foreach (var rotatorFolderName in RotatorFolders)
            {
                string rotatorFolderPath = Path.Combine(pageFolderPath, rotatorFolderName);
                CreateFolderIfNotExists(rotatorFolderPath);
                if (Directory.GetFiles(rotatorFolderPath).Length == 0)
                {
                    CopyRotatorImages(rotatorFolderPath);
                }
            } // loop in rotator folders
        } // Create Page Rotator Folders

        private static void CreateFolderIfNotExists(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    //Debug.WriteLine($"Created folder: {folderPath}");
                }
                else
                {
                    //Debug.WriteLine($"Folder already exists: {folderPath}");
                }
            }
            catch (Exception ex)
            {
                //Debug.WriteLine($"Failed to create folder: {folderPath}. Error: {ex.Message}");
            }
        } // Create Folder if Not Exists

        private static void CopyRotatorImages(string targetFolderPath, bool bHomePage = false)
        {            
            string imagesFolderPath = Path.Combine(GetSeedingDirectory(), "SeedImages"); // seeding path
            List<string> imagesToCopy = new List<string> { "middleHomeImage.jpg", "rightHomeImage.jpg" }; // for home paqe
           
            if (!bHomePage) // Rotator Single Image in Folder
            {
                imagesToCopy.Clear();
                imagesToCopy = new List<string> { "Default.jpg" };
            }                      

            foreach (string imageName in imagesToCopy)
                {

                var imageFileName = imageName;

                if (imagesToCopy.Count == 1)
                {
                    string rotatorFolderName = Path.GetFileName(targetFolderPath); // image rotator name without path
                    imageFileName = rotatorFolderName + imageFileName; // default rotator image
                }

                    string sourceImagePath = Path.Combine(imagesFolderPath, imageFileName);
                    string destinationImagePath = Path.Combine(targetFolderPath, imageFileName);

                    try
                    {
                        if (File.Exists(sourceImagePath) && !File.Exists(destinationImagePath))
                        {
                            File.Copy(sourceImagePath, destinationImagePath);
                            //Debug.WriteLine($"Copied image: {imageFileName} to {targetFolderPath}");
                        }
                        else
                        {
                            //Debug.WriteLine($"Cannot copy image: {imageFileName} to {targetFolderPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        //Debug.WriteLine($"Failed to copy image: {imageFileName} to {targetFolderPath}. Error: {ex.Message}");
                    }
                }
            
        }//CopyRotatorImages



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
            if (Directory.Exists(appRoot)) {                        
                Directory.Delete(appRoot, recursiveDelete);
            }
        } // Delete Media SubDirectory

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
    }
}
