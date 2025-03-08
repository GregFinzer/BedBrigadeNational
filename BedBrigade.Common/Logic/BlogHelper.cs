using BedBrigade.Common.Models;
using BedBrigade.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using BedBrigade.Common.Constants;
using System.Diagnostics;
using System.Text.Json;
using System.ComponentModel;

namespace BedBrigade.Common.Logic
{

public static class BlogHelper
    {
        private static readonly string[] RouterFolders = { "leftImageRotator", "middleImageRotator", "rightImageRotator" };

        public static readonly Dictionary<string, string> ValidContentTypes = new()
        {
            { "news", "News" },
            { "new", "News" },      // Corrects "new" → "News"
            { "stories", "Stories" },
            { "story", "Stories" }  // Corrects "story" → "Stories"
        };        
    
        public static string FormatDateForRazor(DateTime? date)
        {
            if (date.HasValue)
            {
                DateTime actualDate = date.Value;

                // Extract the month name and format it as Dynamic{MonthName}
                string monthKey = $"Dynamic{actualDate.ToString("MMMM", CultureInfo.InvariantCulture)}";

                // Return the month key and the day and year
                string day = actualDate.Day.ToString();
                string year = actualDate.Year.ToString();

                // Return just the month key, day, and year
                return $"{monthKey}| {day}, {year}";
            }

            // Return a default value (or empty string) if the DateTime is null
            return "N/A"; // Default value if the date is null
        }// FormatDateForRazor

        public static string GetFormattedDate(DateTime? theDate, string part)
        {
            // Check if theDate is null
            if (!theDate.HasValue)
            {
                return ""; // Or you could return an empty string, or handle it as you prefer
            }

            // If theDate is not null, use the value
            DateTime myDate = theDate.Value;

            switch (part.ToLower())
            {
                case "month":
                    return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(myDate.Month);
                case "period":
                    return $"{myDate:dd}, {myDate:yyyy}";
                default:
                    throw new ArgumentException("Invalid part. Use 'Month' or 'Period'.");
            }
        }

        public static bool IsValidContentType(string? contentTypeName)
        {
            if (!string.IsNullOrWhiteSpace(contentTypeName))
            {
                contentTypeName = StringUtil.ProperCase(contentTypeName);
            }

            return Enum.TryParse(contentTypeName, out ContentType _);
        }//IsValidContentType

        public static int CountImageFiles(string folderPath, string[] imageExtensions)
        {
            if (!Directory.Exists(folderPath))
                return 0; // Return 0 if folder doesn't exist

            //string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

            return Directory.EnumerateFiles(folderPath)
                .Count(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()));
        }
        public static List<BlogData> GetBlogItemsDataList(List<Content>? lstContentData, string? LocationRoute, string? LocationName, string? ContentType, string[] AllowedExtensions)
        {
            // Content Data already filtered to Location & Type

            List<BlogData> myBlogData = new();

            if (lstContentData != null && lstContentData.Count > 0)
            {


                myBlogData = lstContentData.Select(s =>
                {

                    //  folder path for images
                    string BlogFolderPath = $"media/{LocationRoute}/pages/{ContentType}/BlogItem_{s.ContentId}";

                    // Use the new utility function to get main image and optional images
                    var (mainImageUrl, optImagesUrls) = BlogHelper.GetBlogImages(s.Name, BlogFolderPath, AllowedExtensions);

                    return new BlogData
                    {
                        ContentId = s.ContentId,
                        LocationId = s.LocationId,
                        LocationRoute = LocationRoute,
                        LocationName = LocationName,
                        ContentType = s.ContentType,
                        Title = StringUtil.IsNull(s.Title, ""),
                        Name = StringUtil.IsNull(s.Name, ""),
                        CreateDate = s.CreateDate,
                        UpdateDate = s.UpdateDate ?? s.CreateDate,
                        ContentHtml = s.ContentHtml,
                        BlogFolder = BlogFolderPath,
                        MainImageUrl = mainImageUrl, // Set the main image URL
                        OptImagesUrl = optImagesUrls,   // Set the optional image URLs
                        CreatedDateMonth = GetFormattedDate(s.CreateDate, "month"),
                        CreatedDatePeriod = GetFormattedDate(s.CreateDate, "period"),
                        UpdatedDateMonth = GetFormattedDate(s.UpdateDate ?? s.CreateDate, "month"),
                        UpdatedDatePeriod = GetFormattedDate(s.UpdateDate ?? s.CreateDate, "period")
                    };
                }).OrderByDescending(b => b.CreateDate).ToList();
            }

            return myBlogData;
        } // Blog Data


        public static (string MainImageUrl, List<string> OptImagesUrls) GetBlogImages(string sourceFileName, string fullUrlPathToParentFolder, string[] AllowedExtensions)
        {

            var optImagesUrls = new List<string>();

            // Validate file name and extension
            bool isValidFileName = !string.IsNullOrWhiteSpace(sourceFileName) &&
                                   !Path.GetInvalidFileNameChars().Any(sourceFileName.Contains) &&
                                   AllowedExtensions.Contains(Path.GetExtension(sourceFileName), StringComparer.OrdinalIgnoreCase);

            // Combine folder and file paths
            string folderPath = ConvertToPhysicalPath(fullUrlPathToParentFolder);
           // Debug.WriteLine("Blog Folder: " + folderPath);
            string filePath = Path.Combine(folderPath, sourceFileName);
            //Debug.WriteLine("Check Main File:" + filePath);

            string mainImageUrl = Defaults.ErrorImagePath;

            try
            {
                // Ensure the folder exists
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Collect all files with allowed extensions in the folder
                var files = Directory.GetFiles(folderPath)
                                      .Where(file => AllowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                                      .OrderBy(File.GetCreationTime)
                                      .Select(file => Path.Combine(fullUrlPathToParentFolder, Path.GetFileName(file)))
                                      .ToList();

                // Determine the main image
                if (isValidFileName && File.Exists(filePath))
                {
                    mainImageUrl = $"{fullUrlPathToParentFolder}/{sourceFileName}";
                }
                else if (files.Any())
                {
                    mainImageUrl = files[0]; // Use the first file in the folder as the main image
                }

                // Populate optional images (excluding the main image)
                string mainFileName = Path.GetFileName(mainImageUrl);
                optImagesUrls.AddRange(files.Where(file => !file.Equals(mainFileName, StringComparison.OrdinalIgnoreCase)));
            }
            catch
            {
                // In case of exceptions, main image is NoImageFound, and optional images are empty

                optImagesUrls.Clear();
            }

            return (mainImageUrl, optImagesUrls);
        } // Get Blog Images

        public static string ConvertToPhysicalPath(string relativePath)
        {
            // Ensure the relative path uses correct separators
            relativePath = relativePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());

            // Combine the base path with the relative path
            string basePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string physicalPath = Path.Combine(basePath, relativePath);

            return physicalPath;
        }

        public class BlogConfiguration
        {
            public bool TestMode { get; set; }
            public bool ShowBanner { get; set; }
            public bool CardSettings { get; set; }
            public bool CardPaging { get; set; }
            public int CardColumns { get; set; }
            public int CardRows { get; set; }
            public int CardTextSize { get; set; }

            public BlogConfiguration(string configString)
            {
                LoadFromConfigString(configString);
            }

            public void LoadFromConfigString(string configString)
            {
                var settings = configString.Split('|');
                foreach (var setting in settings)
                {
                    var keyValue = setting.Split(':');
                    if (keyValue.Length == 2)
                    {
                        string key = keyValue[0].Trim();
                        string value = keyValue[1].Trim();

                        // Use reflection to set the property dynamically
                        var property = typeof(BlogConfiguration).GetProperty(key);
                        if (property != null)
                        {
                            if (property.PropertyType == typeof(bool) && bool.TryParse(value, out bool boolResult))
                                property.SetValue(this, boolResult);
                            else if (property.PropertyType == typeof(int) && int.TryParse(value, out int intResult))
                                property.SetValue(this, intResult);
                        }
                    }
                }
            }
        }// BlogConfiguration

        public static void ValidateAndPrepareBannerFolders(string route, string contentType, string WebRootPath)
        {
            if (string.IsNullOrWhiteSpace(route) || string.IsNullOrWhiteSpace(contentType))
            {
                Debug.WriteLine("BlogHelper: Route or ContentType parameter is missing.");
                return;
            }

            if (route.Equals("national", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine("BlogHelper: Skipping validation for national route.");
                return;
            }

            string basePath = Path.Combine(WebRootPath, "media", route, "pages", contentType);
            string modelPath = Path.Combine(WebRootPath, "media", "national", "pages", contentType);

            try
            {
                EnsureDirectoryExists(basePath);

                foreach (var subfolder in RouterFolders)
                {
                    var subfolderPath = Path.Combine(basePath, subfolder);
                    if (!Directory.Exists(subfolderPath) || !Directory.EnumerateFiles(subfolderPath).Any())
                    {
                        Debug.WriteLine($"BlogHelper: Missing or empty: {subfolderPath}, copying from model...");
                        CopyModelFolder(modelPath, basePath, subfolder);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BlogHelper: Error in ValidateAndPrepareBannerFolders - {ex.Message}");
            }
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.WriteLine($"BlogHelper: Created directory: {path}");
            }
        }

        private static void CopyModelFolder(string modelPath, string destinationBasePath, string subfolder)
        {
            string sourcePath = Path.Combine(modelPath, subfolder);
            string destinationPath = Path.Combine(destinationBasePath, subfolder);

            if (!Directory.Exists(sourcePath))
            {
                Debug.WriteLine($"BlogHelper: Model folder not found: {sourcePath}, skipping copy.");
                return;
            }

            Directory.CreateDirectory(destinationPath);

            foreach (var file in Directory.GetFiles(sourcePath))
            {
                string destFile = Path.Combine(destinationPath, Path.GetFileName(file));
                File.Copy(file, destFile, true);
                Debug.WriteLine($"BlogHelper: Copied: {file} -> {destFile}");
            }
        }


    } // BlogHelper Class
} // namespace

