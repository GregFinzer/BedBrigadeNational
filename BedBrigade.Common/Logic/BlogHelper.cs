using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BedBrigade.Common.Logic
{

    public static class BlogHelper
    {
        private static readonly string[] RouterFolders = { "leftImageRotator", "middleImageRotator", "rightImageRotator" };
        private const string PeriodMonth = "month";
        private const string PeriodPeriod = "period";
        private static readonly string[] AllowedExtensions = new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".gif",
                    ".webp"
                }.Select(ext => new string(ext.Where(c => !char.IsWhiteSpace(c)).ToArray()))
                .ToArray();



        public static readonly Dictionary<string, string> ValidContentTypes = new()
        {
            { "news", "News" },
            { "new", "News" },      // Corrects "new" → "News"
            { "stories", "Stories" },
            { "story", "Stories" }  // Corrects "story" → "Stories"
        };             

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

                    string BlogFolderPath = $"media/{locationRoute}/{s.ContentType.ToString()}/{s.Name}";
                    var (mainImageUrl, optImagesUrls) = BlogHelper.GetBlogImages(s.MainImageFileName, BlogFolderPath, AllowedExtensions);

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
                        MainImageFileName = StringUtil.IsNull(
                            !string.IsNullOrEmpty(s.MainImageFileName)
                                ? s.MainImageFileName
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



        public static List<BlogData> GetBlogItemsDataList(List<Content>? lstContentData, string? LocationRoute, string? LocationName, string? ContentType, string[] AllowedExtensions)
        {
            // Content Data already filtered to Location & Type

            List<BlogData> myBlogData = new();

            if (lstContentData != null && lstContentData.Count > 0)
            {


                myBlogData = lstContentData.Select(s =>
                {

                    //  folder path for images
                    string BlogFolderPath = $"media/{LocationRoute}/{ContentType}/{s.Name}";

                    // Use the new utility function to get main image and optional images
                    var (mainImageUrl, optImagesUrls) = BlogHelper.GetBlogImages(s.MainImageFileName, BlogFolderPath, AllowedExtensions);

                    return new BlogData
                    {
                        ContentId = s.ContentId,
                        LocationId = s.LocationId,
                        LocationRoute = LocationRoute,
                        LocationName = LocationName,
                        ContentType = s.ContentType,
                        Title = StringUtil.IsNull(s.Title, ""),
                        MainImageFileName = StringUtil.IsNull(s.MainImageFileName, ""),
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


        public static (string MainImageUrl, List<string> OptImagesUrls) GetBlogImages(string? sourceFileName, string fullUrlPathToParentFolder, string[] AllowedExtensions)
        {
            var optImagesUrls = new List<string>();
            var mainImageUrl = Defaults.ErrorImagePath;
            fullUrlPathToParentFolder = NormalizePath(fullUrlPathToParentFolder);

            if (string.IsNullOrWhiteSpace(sourceFileName))
            {
                return (mainImageUrl, optImagesUrls);
            }

            sourceFileName = sourceFileName.Trim();

            bool isValidFileName = IsValidFileName(sourceFileName);

            //var fileExt = Path.GetExtension(sourceFileName);
            if (isValidFileName) // main file
            {
                Debug.WriteLine($"GetBlogImages. File Valid Name {sourceFileName}: {isValidFileName.ToString()}");
                // Combine folder and file paths

                string folderPath = ConvertToPhysicalPath(fullUrlPathToParentFolder);

                Debug.WriteLine("GetBlogImages. Blog Folder: " + folderPath);

                if (Directory.Exists(folderPath))
                {

                    string filePath = Path.Combine(folderPath, sourceFileName);
                    Debug.WriteLine("GetBlogImages. Check Main Image File:" + filePath);
                    // Check if the specified file exists   
                    if (File.Exists(filePath))
                    {
                        mainImageUrl = $"{fullUrlPathToParentFolder}/{sourceFileName}";
                        Debug.WriteLine("GetBlogImages. Main Image URL: " + mainImageUrl);
                    }
                    else
                    {
                        Debug.WriteLine("GetBlogImages. Main Image File not found: " + filePath);
                        mainImageUrl = GetBlogReplacementMainImage(fullUrlPathToParentFolder);
                        Debug.WriteLine("GetBlogImages. Replaced Main Image URL: " + mainImageUrl);
                    }
                }
                else
                {
                    Debug.WriteLine("GetBlogImages. Main Image Folder not found: " + folderPath);
                }
            }
            else
            {
                Debug.WriteLine("GetBlogImages. File Invalid Name: " + sourceFileName);
                mainImageUrl = GetBlogReplacementMainImage(fullUrlPathToParentFolder);
                // add Replacement URL to OptImages 
                optImagesUrls.Add(mainImageUrl);
                Debug.WriteLine("GetBlogImages. Replaced Main Image URL: " + mainImageUrl);
            }

            return (mainImageUrl, optImagesUrls);
        } // Get Blog Images

        public static string GetBlogReplacementMainImage(string fullUrlPathToParentFolder)
        { // return the first image in the folder
            var BlogFolderPath = ConvertToPhysicalPath(fullUrlPathToParentFolder);
            string? mainImageUrl = Defaults.ErrorImagePath;
            if (Directory.Exists(BlogFolderPath))
            {
                var files = Directory.GetFiles(BlogFolderPath);
                if (files.Length > 0)
                {
                    // Sort files by creation date (ascending order)
                    var firstFile = files
                        .Select(file => new FileInfo(file))
                        .OrderBy(fileInfo => fileInfo.CreationTime)
                        .FirstOrDefault();
                    if (firstFile != null)
                    {
                        var replacementFileName = Path.GetFileName(firstFile.FullName);
                        mainImageUrl = $"{fullUrlPathToParentFolder}/{replacementFileName}";
                    }
                }
            }
            return mainImageUrl;
        }


        public static bool IsValidFileName(string sourceFileName)
        {
            bool IsValid = false;

            string? fileExt = Path.GetExtension(sourceFileName)?.ToLowerInvariant();
            HashSet<string> AllowedExtensionHash = new(".jpg|.jpeg|.png|.gif|.webp".Split('|'), StringComparer.OrdinalIgnoreCase);
            if (fileExt != null && fileExt.Length > 0)
            {
                IsValid = !string.IsNullOrWhiteSpace(sourceFileName)
                    && !Path.GetInvalidFileNameChars().Any(sourceFileName.Contains)
                    && AllowedExtensionHash.Contains(fileExt);
            }

            return (IsValid);

        }//IsValidFileNam

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

        public static BlogData CloneBlog(BlogData original)
        {
            return new BlogData
            {
                // Base class: Content
                ContentId = original.ContentId,
                LocationId = original.LocationId,
                ContentType = original.ContentType,
                Title = original.Title,
                Name = original.Name,
                MainImageFileName = original.MainImageFileName,
                ContentHtml = original.ContentHtml,
                UploadedFiles = original.UploadedFiles,

                // BlogData fields
                LocationRoute = original.LocationRoute,
                LocationName = original.LocationName,
                BlogFolder = original.BlogFolder,
                MainImageUrl = original.MainImageUrl,
                MainImageThumbnail = original.MainImageThumbnail,
                OptImagesUrl = original.OptImagesUrl != null ? new List<string>(original.OptImagesUrl) : [],
                FileUploaded = original.FileUploaded != null ? new List<string>(original.FileUploaded) : [],
                FileDelete = original.FileDelete != null ? new List<string>(original.FileDelete) : [],
                CreatedDateMonth = original.CreatedDateMonth,
                CreatedDatePeriod = original.CreatedDatePeriod,
                UpdatedDateMonth = original.UpdatedDateMonth,
                UpdatedDatePeriod = original.UpdatedDatePeriod,
                IsNewItem = original.IsNewItem
            };
        }

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

            //sb.AppendLine("<!-- Blog Content Display Area -->");

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

            var mainImageUrl = CurrentBlog.MainImageUrl;
            bool isMainImageExist = true;
            if (CurrentBlog.OptImagesUrl != null && CurrentBlog.OptImagesUrl.Count > 0)
            {
                isMainImageExist = !CurrentBlog.OptImagesUrl.Contains(mainImageUrl);
            }

            if (isMainImageExist)
            { // show user created main image

                // Main Blog Image
                sb.AppendLine($"{divOpen}row mb-2\">");
                sb.AppendLine($"{divOpen}col-md-12 jd-flex justify-content-center align-items-center\">");
                sb.AppendLine($"        <img src=\"{CurrentBlog.MainImageUrl}\" alt=\"{CurrentBlog.Title}\" class=\"detail-image\" />");
                sb.AppendLine($"        <span style=\"font-size: smaller; display: none\">{CurrentBlog.MainImageUrl}</span>");
                sb.AppendLine($"{divClose}");
                sb.AppendLine($"{divClose}");
            }

            // Blog Content (HTML)
            sb.AppendLine($"{divOpen}row mb-2\">");
            sb.AppendLine($"{divOpen}col-md-12\">");
            sb.AppendLine("        <p style=\"width: 100%\">");
            sb.AppendLine(SanitizeHtml(CurrentBlog.ContentHtml));
            sb.AppendLine("        </p>");
            sb.AppendLine($"{divClose}");
            sb.AppendLine($"{divClose}");
                     

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

        public static string TruncateHtmlText(string? html, int maxLength)
        {
            if (String.IsNullOrEmpty(html))
            {
                return String.Empty;
            }


            // Step 1: Remove HTML tags
            string plainText = Regex.Replace(html, "<.*?>", string.Empty);

            if (plainText.Length <= maxLength)
            {
                // If the text is already shorter than maxLength, return as-is
                return plainText;
            }

            // Step 2: Truncate without cutting off in the middle of words
            string truncated = plainText.Substring(0, maxLength);

            int lastSpaceIndex = truncated.LastIndexOf(' ');
            if (lastSpaceIndex > 0)
            {
                // Trim to the last complete word
                truncated = truncated.Substring(0, lastSpaceIndex);
            }

            // Step 3: Add "..." to indicate the text has been cut
            return $"{truncated}...";
        } // Truncate HTML

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

