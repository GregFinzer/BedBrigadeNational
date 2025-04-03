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
using System.Security.Principal;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace BedBrigade.Common.Logic
{

public static class BlogHelper
    {
        private static readonly string[] RouterFolders = { "leftImageRotator", "middleImageRotator", "rightImageRotator" };
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const string PeriodMonth = "month";
        private const string PeriodPeriod = "period";

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
                case PeriodMonth:
                    return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(myDate.Month);
                case PeriodPeriod:
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

        public static List<BlogData> GetBlogDataList(List<Content> lstContentData, List<Location> lstLocations)
        {
            List<BlogData> myBlogData = new();

            if (lstContentData != null && lstContentData.Count > 0)
            {
                // Optimize lookups by creating a dictionary
                var locationLookup = lstLocations.ToDictionary(loc => loc.LocationId);

                myBlogData = lstContentData.Select(s =>
                {
                   

                    // Try to get the location from the dictionary
                    Location? myLocation = locationLookup.TryGetValue(s.LocationId, out var loc) ? loc : null;
                    string locationRoute = myLocation?.Route ?? string.Empty;
                    string locationName = myLocation?.Name ?? string.Empty;

                    string BlogFolderPath = $"media/{locationRoute}/pages/{s.ContentType.ToString()}/BlogItem_{s.ContentId}";
                    var (mainImageUrl, optImagesUrls) = BlogHelper.GetBlogImages(s.Name, BlogFolderPath, AllowedExtensions);

                    // Handle null Enum.GetName cases
                    string ContentTypeName = Enum.GetName(typeof(ContentType), s.ContentType) ?? "Unknown";

                    return new BlogData
                    {
                        ContentId = s.ContentId,
                        LocationId = s.LocationId,
                        LocationRoute = locationRoute,
                        LocationName = locationName,
                        ContentType = s.ContentType,
                        Title = StringUtil.IsNull(s.Title, ""),
                        Name = StringUtil.IsNull(
                            !string.IsNullOrEmpty(s.Name)
                                ? s.Name
                                : (!string.IsNullOrEmpty(mainImageUrl)
                                    ? Path.GetFileName(new Uri(mainImageUrl).LocalPath)
                                    : ""),
                                ""),
                        CreateDate = s.CreateDate,
                        UpdateDate = s.UpdateDate ?? s.CreateDate,
                        ContentHtml = s.ContentHtml,
                        MainImageUrl = mainImageUrl, // Set the main image URL
                        OptImagesUrl = optImagesUrls,   // Set the optional image URLs 
                        BlogFolder = BlogFolderPath.Replace("//", "/"),
                        CreatedDateMonth = GetFormattedDate(s.CreateDate, PeriodMonth),
                        CreatedDatePeriod = GetFormattedDate(s.CreateDate, PeriodPeriod),
                        UpdatedDateMonth = GetFormattedDate(s.UpdateDate ?? s.CreateDate, PeriodMonth),
                        UpdatedDatePeriod = GetFormattedDate(s.UpdateDate ?? s.CreateDate, PeriodPeriod)

                    };
                }).OrderByDescending(b => b.CreateDate).ToList();
            }

            return myBlogData;
        }// Blog Data List

        public static string GetCardMainImageUrl(string sourceFileName, int contentId, string LocationRoute, string? ContentTypeName)
        {
            var locationMediaDirectory = FileUtil.GetMediaDirectory(LocationRoute);
            string fullPathToParentFolder = Path.Combine(locationMediaDirectory, $"Pages/{ContentTypeName}/BlogItem_{contentId}");
            string? imageFileUrl = Defaults.ErrorImagePath;

            // Step 1: Validate file name and extension
            if (string.IsNullOrWhiteSpace(sourceFileName) ||
                Path.GetInvalidFileNameChars().Any(sourceFileName.Contains) ||
                !AllowedExtensions.Contains(Path.GetExtension(sourceFileName), StringComparer.OrdinalIgnoreCase))
            {
                return imageFileUrl;
            }

            // Combine folder and file path
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fullPathToParentFolder.Replace("/", Path.DirectorySeparatorChar.ToString()));
            string filePath = Path.Combine(folderPath, sourceFileName);

            try
            {
                // Step 2: Check if folder exists
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    return imageFileUrl;
                }

                // Step 3: Check if the specified file exists
                if (File.Exists(filePath))
                {
                    imageFileUrl = GetRelativePathFromWebRoot(filePath);
                    return imageFileUrl;
                }

                // Step 4: Check if there are other files in the folder with allowed extensions
                var files = Directory.GetFiles(folderPath)
                                      .Where(file => AllowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                                      .OrderBy(File.GetCreationTime)
                                      .Select(Path.GetFileName)
                                      .ToArray();

                if (files.Length > 0)
                {
                    filePath = Path.Combine(folderPath, files[0]);
                    imageFileUrl = GetRelativePathFromWebRoot(filePath);
                    return imageFileUrl;
                }

                // No files found, return NoImageFound URL
                return imageFileUrl;
            }
            catch
            {
                // Handle any exceptions (e.g., permissions issues) and return NoImageFound URL
                return imageFileUrl;
            }

        } // Get Card Main Image Url


        public static string GetBlogLocationFolder(string LocationRoute, string? ContentTypeName)
        {
            string locationMediaDirectory = FileUtil.GetMediaDirectory(LocationRoute);
            string imageFolderPath = Path.Combine(locationMediaDirectory, $"Pages/{ContentTypeName}");
            string relatedUrl = GetRelativePathFromWebRoot(imageFolderPath);

            return (relatedUrl);
        }

        public static List<string> GetBlogItemAdditionalFiles(int contentId, string LocationRoute, string? ContentTypeName)
        {

            string locationMediaDirectory = FileUtil.GetMediaDirectory(LocationRoute);
            string imageFolderPath = Path.Combine(locationMediaDirectory, $"Pages/{ContentTypeName}/BlogItem_{contentId}");
            string relatedUrl = GetRelativePathFromWebRoot(imageFolderPath);

            List<string>? FileUrls = new();
            if (Directory.Exists(imageFolderPath))
            {
                // Get all files in the directory
                string[] filesArray = Directory.GetFiles(imageFolderPath);

                // Add a prefix to each file name
                var prefix = relatedUrl + "/";
                string[] prefixedFilesArray = Array.ConvertAll(filesArray, file => prefix + Path.GetFileName(file));

                // Convert the array to a List<string>
                FileUrls = new List<string>(prefixedFilesArray);
            }

            return (FileUrls);
        }//GetBlogItemAdditionalFiles


        public static string GetRelativePathFromWebRoot(string fullPath)
        {
            const string webRootKeyword = "wwwroot";
            int index = fullPath.IndexOf(webRootKeyword, StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                // Extract the part after "wwwroot"
                return fullPath.Substring(index + webRootKeyword.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            throw new ArgumentException("The fullPath does not contain 'wwwroot'.");
        } // Relative URL

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
            fullUrlPathToParentFolder=NormalizePath(fullUrlPathToParentFolder);
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

        public static string NormalizePath(string? inputPath, bool isUrl = true)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
            {
                return string.Empty;
            }

            if (isUrl)
            {
                // Normalize for URL: Replace backslashes with forward slashes
                return inputPath.Replace("\\", "/").Replace("//", "/");
            }
            else
            {
                // Normalize for file path: Use Path.Combine to ensure the correct path separator
                string normalizedPath = inputPath.Replace("/", Path.DirectorySeparatorChar.ToString())
                                             .Replace("\\\\", Path.DirectorySeparatorChar.ToString());

                // Handle multiple separators (if necessary)
                return normalizedPath;
            }
        }

       
        public static BlogData CloneBlog(BlogData BlogOriginal)
        {
            return JsonSerializer.Deserialize<BlogData>(JsonSerializer.Serialize(BlogOriginal));
        } // Clone Blog
        public static List<string> AuditBlogFiles(string? folderPath, string? baseUrl)
        {
            List<string> fileUrls = new List<string>();
           
            // Check if the directory exists
            if (Directory.Exists(folderPath))
            {
                // Get all files from the directory
                string[] files = Directory.GetFiles(folderPath);

                foreach (string file in files)
                {
                    // Get the file name and convert to URL format
                    string fileName = Path.GetFileName(file);

                    // Create the URL by combining the base URL and file name
                    string fileUrl = $"{baseUrl.TrimEnd('/')}/{fileName}";

                    // Add the URL to the list
                    fileUrls.Add(fileUrl);
                }
            }
            else
            {
                Debug.WriteLine($"Directory not found: {folderPath}");
            }

            return fileUrls;
        }
        // 
        public static void DeleteBlogFiles(string wwwrootPath, List<string> fileList)
        {
            foreach (var fileUrl in fileList)
            {
                // Convert URL path to physical file path
                string filePath = Path.Combine(wwwrootPath, fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                // Check if the file exists before attempting to delete
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                        Debug.WriteLine($"File Deleted: {filePath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting file: {filePath}. Exception: {ex.Message}");
                    }
                }
                else
                {
                    Debug.WriteLine($"File not found: {filePath}");
                }
            }
        }

        public static string SanitizeFileName(string fileName)
        {
            // Replace any invalid characters (e.g., commas, spaces) with underscores or remove them
            fileName = Regex.Replace(fileName, @"[^a-zA-Z0-9_\-\.]", "_");

            // Optionally, if you want to handle multiple dots or other cases:
            fileName = fileName.Replace("..", "_");

            return fileName;
        }

        public static string GenerateBlogHtml(BlogData CurrentBlog, bool IsPdf = false)
        {
            StringBuilder sb = new StringBuilder();
            string divClose = "</div>";
            string divOpen = "<div class=\"";

            if (IsPdf)
            {

                sb.AppendLine("<!DOCTYPE html>");
                sb.AppendLine("<html>");
                sb.AppendLine("<head>");
                sb.AppendLine("<link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css\" integrity=\"sha384-rbsA2VBKQ1eq+1JOGFNRaCW/KeLY1NHhFIMiIjRrL6v67Is92RuK0MlohR8c56Dd\" crossorigin=\"anonymous\">");
                sb.AppendLine("<style>");
                sb.AppendLine(".detail-image { max-width: 100%; height: auto; margin-top: 10px; }");
                sb.AppendLine("</style>");
                sb.AppendLine("</head>");
                sb.AppendLine("<body class=\"container\">");
            }

            sb.AppendLine("<!-- Blog Content Display Area -->");

            // Top content row
            sb.AppendLine($"{divOpen}row mb-2\">");
            sb.AppendLine($"{divOpen}col-md-6 jd-flex justify-content-center align-items-left\">");
            sb.AppendLine($"        <h4>{CurrentBlog.LocationName}</h4>");
            sb.AppendLine($"{divClose}");
            sb.AppendLine($"{divOpen}col-md-6 jd-flex justify-content-center align-items-right\">");

            // Placeholder for translation logic
            string contentCreateMonth = CurrentBlog.CreatedDateMonth;
            string contentUpdatedMonth = CurrentBlog.UpdatedDateMonth;

            sb.AppendLine("        <h6 style=\"text-align: right; font-size: small; color: green\">");
            sb.AppendLine("            <i>");
            sb.AppendLine($"                Posted on {contentCreateMonth} {CurrentBlog.CreatedDatePeriod} | ");
            sb.AppendLine($"                Last modified on {contentUpdatedMonth} {CurrentBlog.UpdatedDatePeriod}");
            sb.AppendLine("            </i>");
            sb.AppendLine("        </h6>");
            sb.AppendLine($"{divClose}");
            sb.AppendLine($"{divClose}");

            // Title row
            sb.AppendLine($"{divOpen}row mb-2\">");
            sb.AppendLine($"{divOpen}col-md-12 jd-flex justify-content-center align-items-left\">");
            sb.AppendLine($"        <h4>{CurrentBlog.Title}</h4>");
            sb.AppendLine($"{divClose}");
            sb.AppendLine($"{divClose}");

            // Main Blog Image
            sb.AppendLine($"{divOpen}row mb-2\">");
            sb.AppendLine($"{divOpen}col-md-12 jd-flex justify-content-center align-items-center\">");
            sb.AppendLine($"        <img src=\"{CurrentBlog.MainImageUrl}\" alt=\"{CurrentBlog.Title}\" class=\"detail-image\" />");
            sb.AppendLine($"        <span style=\"font-size: smaller; display: none\">{CurrentBlog.MainImageUrl}</span>");
            sb.AppendLine($"{divClose}");
            sb.AppendLine($"{divClose}");

            // Blog Content (HTML)
            sb.AppendLine($"{divOpen}row mb-2\">");
            sb.AppendLine($"{divOpen}col-md-12\">");
            sb.AppendLine("        <p style=\"width: 100%\">");
            sb.AppendLine(SanitizeHtml(CurrentBlog.ContentHtml));
            sb.AppendLine("        </p>");
            sb.AppendLine($"{divClose}");
            sb.AppendLine($"{divClose}");

            if (IsPdf)
            {

                sb.AppendLine($"{divClose}");
                sb.AppendLine("</body>");
                sb.AppendLine("</html>");
            }

            return sb.ToString();
        } // Get Blog Html

        public static string SanitizeHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            // Remove empty <p>, <div>, <br>, and other empty tags
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<(\w+)[^>]*>\s*</\1>", string.Empty);

            // Trim spaces and line breaks
            return html.Trim();
        }

        public static string AddBaseUrlToRelativeImages(string htmlContent, string baseUrl)
        {
            // Regex to match <img src="..."> or <img src='...'>
            string pattern = @"(<img\s+[^>]*src\s*=\s*[""'])([^""'http][^""]+)([""'])";

            // Add base URL to relative paths
            string updatedHtml = Regex.Replace(htmlContent, pattern, m =>
            {
                string originalSrc = m.Groups[2].Value;

                // Check if it's a relative URL (not starting with http:// or https://)
                if (!originalSrc.StartsWith("http://") && !originalSrc.StartsWith("https://"))
                {
                    string absoluteUrl = $"{baseUrl.TrimEnd('/')}/{originalSrc.TrimStart('/')}";
                    return $"{m.Groups[1].Value}{absoluteUrl}{m.Groups[3].Value}";
                }

                // Return the original URL if it's already absolute
                return m.Value;
            });

            return updatedHtml;
        }

        public static int GenerateTempContentId()
        {
            // Get the current UTC timestamp in seconds
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Reduce the value to fit within int range (0 to 2,147,483,647)
            int tempContentId = (int)(timestamp % int.MaxValue);

            return tempContentId;
        }

    } // BlogHelper Class
} // namespace

