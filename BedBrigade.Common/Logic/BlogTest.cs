using BedBrigade.Common.Enums;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Common.Logic
    {
    public static class BlogTest
    {
        // Static list to hold the blog items
        private static List<BlogItemDto> blogItemList = new List<BlogItemDto>();
        public const string MediaRoot = "media";
        private static int ControlCountItems = 66;
        private static int ControlCountFolders = 66;
        private static int ControlCountImages = 165;
        public class TestValidationResult
        {
            public int BlogCount { get; set; }
            public int FolderCount { get; set; }
            public int ImageCount { get; set; }
            public string TestBarStyleClass = "row bg-danger";
            public bool IsComplete { get; set; }

            public TestValidationResult(int blogCount, int folderCount, int imageCount, string testBarClass, bool isComplete)
            {
                BlogCount = blogCount;
                FolderCount = folderCount;
                ImageCount = imageCount;
                TestBarStyleClass = testBarClass;
                IsComplete = isComplete;
            }
        }//TestValidationResult


        // Method to validate and create test data if needed
        public static TestValidationResult? ValidateAndCreateTestData(string? connectionString, string? webRootPath, string ResetAction = "")
        {
            int blogCount = 0;
            int folderCount = 0;
            int imageCount = 0;
            bool isComplete = false;

            //Debug.WriteLine($"Current WebRootPath: {webRootPath}");

            if (String.IsNullOrEmpty(ResetAction)) return null; // If not in Test Mode, return null (no validation).

            // Test Data Status - how to not run each time?
            if (ResetAction == "load")
            {
                // Get current counts
                (blogCount, folderCount, imageCount) = GetTestDataCounters(connectionString, webRootPath);

                isComplete = blogCount == ControlCountItems &&
                                  folderCount == ControlCountFolders &&
                                  imageCount == ControlCountImages;
                if(isComplete)
                {
                    Debug.WriteLine("TEST DATA: All data, folders & images are already present. No changes required.");
                    return new TestValidationResult(blogCount, folderCount, imageCount, "row bg-success", true);
                }
                else
                {
                    ResetAction = "reset";
                }
            }// initial page load

            if (ResetAction=="clear" || ResetAction=="reset") // Delete all data first
            {
                DeleteTestData(connectionString, webRootPath);
            }

            if (ResetAction == "reset")
            {

                // Step 1: Validate and add blog records in the Content table if missing.
                if (!AreTestBlogsAdded(connectionString))
                {
                    AddTestBlogData(connectionString, webRootPath);
                }

                // Step 2: Check if all blog items folder are exists
                if (CountBlogItemFolders(webRootPath) == 0)
                {
                    // create all required Blog Item Folders
                    var intFolderCount = CreateBlogItemFolders(connectionString, webRootPath);  
                }

                // Step 3: Check if images are extracted and count the number of files and folders
               if (!AreImagesUnzipped(webRootPath))
               {
                    UnzipTestImages(webRootPath);
                }

            }

            // Step 3: Get the test data counts
            (blogCount, folderCount, imageCount) = GetTestDataCounters(connectionString, webRootPath);
            string TestModeStyle = GetTestBarClass(blogCount, folderCount, imageCount);
            if (TestModeStyle == "bg-success")
            {
                isComplete = true;
            }

            // Return the results as a TestValidationResult object
            return new TestValidationResult(blogCount, folderCount, imageCount, TestModeStyle,isComplete);
        }//TestValidationResult

        public static (int blogCount, int folderCount, int imageCount) GetTestDataCounters(string connectionString, string webRootPath)
        {
            int blogCount = GetBlogDataCount(connectionString);
            int folderCount = GetBlogItemFolderCount(webRootPath);
            int imageCount = GetImageCountInFolders(webRootPath);

            return (blogCount, folderCount, imageCount);
        }//GetTestDataCounters

        public static string GetTestBarClass(int blogCount, int folderCount, int imageCount)
        {         
            bool allZero = (blogCount == 0 && folderCount == 0 && imageCount == 0);
            bool allMatch = (blogCount == ControlCountItems && folderCount == ControlCountFolders && imageCount == ControlCountImages);

            if (allZero)
                return "row bg-danger"; // Red (No test data)

            if (allMatch)
                return "row bg-success"; // Green (All correct)

            return "row bg-warning"; // Yellow (Mismatch detected)
        
        }// GetTestBarClass()

        // Section 1 - BLOG TEST DATA in Table "Content" ===========

        private static void DeleteTestData(string connectionString, string webRootPath)
        {
            string sqlDelete = "DELETE FROM dbo.Content WHERE ContentType IN (11, 12); SELECT @@ROWCOUNT;";
            string sqlResetIdentity = "declare @max int select @max=ISNULL(max([ContentId]),0) from dbo.Content; DBCC CHECKIDENT ('[Content]', RESEED, @max );";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    int deletedRows;

                    // Execute DELETE and get row count
                    using (SqlCommand deleteCommand = new SqlCommand(sqlDelete, connection))
                    {
                        deletedRows = (int)deleteCommand.ExecuteScalar();
                        if (deletedRows > 0)
                        {
                            Debug.WriteLine($"Deleted {deletedRows} records from table 'Content'.");
                        }
                        else
                        {
                            Debug.WriteLine("No records to delete from table 'Content'.");
                        }
                    }


                    if (deletedRows > 0)
                    {
                        // Reset identity
                        using (SqlCommand resetCommand = new SqlCommand(sqlResetIdentity, connection))
                        {
                            resetCommand.ExecuteNonQuery();
                            Debug.WriteLine("Reseeded ContentId in table 'Content'");
                        }
                    }                   

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }

            // Delete all BlogItem_XX folders with images
            var DelFolderCount = 0;
            string mediaPath = Path.Combine(webRootPath, MediaRoot);
            var blogFolders = Directory.EnumerateDirectories(mediaPath, "BlogItem_*", SearchOption.AllDirectories);
            foreach (var folder in blogFolders)
            {
                Directory.Delete(folder, true);
                DelFolderCount++;
            }
            Debug.WriteLine($"{DelFolderCount} BlogItem folder deleted");


        }//ResetTestData


        // Check if test blog data is already added in the Content table
        private static bool AreTestBlogsAdded(string connectionString)
        {
            const string query = "SELECT COUNT(*) FROM Content WHERE ContentType IN (11, 12)";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand(query, connection);
                int count = (int)command.ExecuteScalar();
                return count > 0;
            }
        } // AreTestBlogsAdded

        // Add test blog data if missing
                private static void AddTestBlogData(string connectionString, string webRootPath)
                {
                    var scriptPath = Path.Combine(webRootPath, "scripts", "AddTestBlogData.sql");
                    string sql = File.ReadAllText(scriptPath);
                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        var command = new SqlCommand(sql, connection);
                        command.ExecuteNonQuery();
                    }
                } // AddTestBlogData

        // Section 1 - TEST DATA - END =======================================

        // SECTION 2 - BLOG ITEM FOLDERS ====================================   

        public static int CountBlogItemFolders(string webRootPath)
        {
            string mediaPath = Path.Combine(webRootPath, MediaRoot);

            // Check if the media directory exists
            if (!Directory.Exists(mediaPath))
                return 0;

            // Get all directories under wwwroot/media recursively
            var allDirectories = Directory.GetDirectories(mediaPath, "BlogItem_*", SearchOption.AllDirectories);

            return allDirectories.Length; // Return the number of matching directories
        }

        //Get the list of Locations

        public static int CreateBlogItemFolders(string connectionString, string webRootPath)
        {
            string mediaPath = Path.Combine(webRootPath, MediaRoot);
            int folderCount = 0;
            // Get Locations List
            List<LocationDto> locations = GetLocationList(connectionString);
            SetBlogItemList(connectionString);
            // Create Blog Folder List
            foreach (var blogItem in blogItemList)
            {
                // Find the matching LocationDto based on LocationId
                var location = locations.FirstOrDefault(loc => loc.LocationID == blogItem.LocationID);

                if (location != null)
                {
                    // Construct the BlogFolderUrl based on the provided pattern
                    blogItem.BlogFolderUrl = $"{location.Route}/pages/{blogItem.ContentTypeName}/BlogItem_{blogItem.ContentID}";
                    blogItem.FileNameRoot = GetFileNameRoot(blogItem.MainFileName) ?? string.Empty;
                    // Debug output for the updated BlogFolderUrl
                    Debug.WriteLine($"[DEBUG] ContentID: {blogItem.ContentID}, BlogFolderUrl: {blogItem.BlogFolderUrl}");

                    string folderPath = Path.Combine(webRootPath, MediaRoot, blogItem.BlogFolderUrl);

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                        Debug.WriteLine($"Created folder: {folderPath}");
                    }
                    else
                    {
                        Debug.WriteLine($"Folder already exists: {folderPath}");
                    }
                }
                else
                {
                    // If no matching Location found, set an empty or default URL (optional)
                    blogItem.BlogFolderUrl = string.Empty;
                }
            }

            return blogItemList.Count;
        } // CreateBlogItemFolders

        public static string? GetFileNameRoot(string filename)
        {
            // Extract root part before the first underscore (_)
            int underscoreIndex = filename.IndexOf('_');
            return (underscoreIndex > 0) ? filename.Substring(0, underscoreIndex) : null;
        }

        public static List<LocationDto> GetLocationList(string connectionString)
        {
            string query = "SELECT LocationID, Route FROM Locations";
            List<LocationDto> locations = new List<LocationDto>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string route = reader.GetString(1);

                        // Remove leading "/" or "\" if present
                        if (!string.IsNullOrEmpty(route) && (route.StartsWith("/") || route.StartsWith("\\")))
                        {
                            route = route.Substring(1);
                        }

                        var location = new LocationDto
                        {
                            LocationID = reader.GetInt32(0),
                            Route = route
                        };

                        locations.Add(location);

                        // Debug output
                        Debug.WriteLine($"[DEBUG] ID: {location.LocationID}, Route: {location.Route}");
                        Console.WriteLine($"[DEBUG] ID: {location.LocationID}, Route: {location.Route}");
                    }
                }
            }
            return locations;
        } // Get List of Locations

        public static void SetBlogItemList(string connectionString)
        {
            // Select current blog items from Content Table
            string query = "SELECT ContentID, LocationID, ContentType, Name FROM Content WHERE ContentType IN (11, 12)";
            blogItemList = new List<BlogItemDto>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int contentType = reader.GetInt32(2);
                        string contentTypeName = contentType switch
                        {
                            11 => "News",
                            12 => "Stories",
                            _ => "Unknown"
                        };

                        var blogItem = new BlogItemDto
                        {
                            ContentID = reader.GetInt32(0),
                            LocationID = reader.GetInt32(1),
                            ContentType = contentType,
                            ContentTypeName = contentTypeName,
                            MainFileName = reader.IsDBNull(3) ? null : reader.GetString(3), // Handling NULL values for MainFileName
                            FileNameRoot = string.Empty, // Empty for now
                            BlogFolderUrl = string.Empty  // Empty for now
                        };

                        blogItemList.Add(blogItem);

                        // Debug output
                        Debug.WriteLine($"[DEBUG] ID: {blogItem.ContentID}, LocationID: {blogItem.LocationID}, Type: {blogItem.ContentTypeName}, MainFileName: {blogItem.MainFileName}");
                        Console.WriteLine($"[DEBUG] ID: {blogItem.ContentID}, LocationID: {blogItem.LocationID}, Type: {blogItem.ContentTypeName}, MainFileName: {blogItem.MainFileName}");
                    }
                }
            }

            //return blogItems;
        } // Get Blog Item/folders list


        // SECTION 2 - BLOG ITEM FOLDERS - END ==============================
        // SECTION 3 - UNZIP TEST IMAGES ===================================

        // Validate if images have been unzipped correctly
        private static bool AreImagesUnzipped(string webRootPath)
        {
            string zipFilePath = Path.Combine(webRootPath, "scripts", "BlogTestImages.zip");
            string mediaFolder = Path.Combine(webRootPath, MediaRoot);

            if (!File.Exists(zipFilePath))
                return true;

            using (var zip = System.IO.Compression.ZipFile.OpenRead(zipFilePath))
            {
                var zipFileNames = zip.Entries
                    .Where(e => !string.IsNullOrEmpty(e.Name))
                    .Select(e => e.FullName)
                    .ToHashSet();

                var extractedFiles = Directory.EnumerateFiles(mediaFolder, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.Contains("BlogItem_"))
                    .Select(f => f.Replace(mediaFolder, "").Replace("\\", "/"))
                    .ToHashSet();

                return zipFileNames.IsSubsetOf(extractedFiles);
            }
        }

        // Unzip the test images to the correct folders
        private static void UnzipTestImages(string webRootPath)
        {
            string zipFilePath = Path.Combine(webRootPath, "scripts", "BlogTestImages.zip");
            string mediaFolder = Path.Combine(webRootPath, MediaRoot);

            if (!File.Exists(zipFilePath))
            {
                Debug.WriteLine($"Zip file not found: {zipFilePath}");
                return;
            }

            using var zip = ZipFile.OpenRead(zipFilePath);

            foreach (var blogItem in blogItemList)
            {
                if (string.IsNullOrEmpty(blogItem.BlogFolderUrl) || string.IsNullOrEmpty(blogItem.FileNameRoot))
                {
                    Debug.WriteLine($"Missing data for ContentID: {blogItem.ContentID}");
                    continue;
                }

                string targetFolder = Path.Combine(mediaFolder, blogItem.BlogFolderUrl);
                EnsureDirectoryExists(targetFolder);

                ExtractMatchingFiles(zip, blogItem.FileNameRoot, targetFolder);
            }
        }// unzip

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        private static void ExtractMatchingFiles(ZipArchive zip, string fileNameRoot, string targetFolder)
        {
            foreach (var entry in zip.Entries.Where(e => e.FullName.Contains(fileNameRoot)))
            {
                string destinationPath = Path.Combine(targetFolder, entry.Name);
                entry.ExtractToFile(destinationPath, true);
                Debug.WriteLine($"Extracted file: {entry.Name} to: {targetFolder}");
            }
        }//ExtractMatchingFiles

        // SECTION 3 - UNZIP IMAGES - END

        // Get the count of blogs in the Content table
        private static int GetBlogDataCount(string connectionString)
        {
            const string query = "SELECT COUNT(*) FROM Content WHERE ContentType IN (11, 12)";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand(query, connection);
                return (int)command.ExecuteScalar();
            }
        }

        // Get the number of folders starting with BlogItem_ in the media folder
        private static int GetBlogItemFolderCount(string webRootPath)
        {
            string mediaFolder = Path.Combine(webRootPath, MediaRoot);
            return Directory.GetDirectories(mediaFolder, "BlogItem_*", SearchOption.AllDirectories).Length;
        }

        // Get the number of image files in all BlogItem_XX folders
        private static int GetImageCountInFolders(string webRootPath)
        {
            string mediaFolder = Path.Combine(webRootPath, MediaRoot);
            return Directory.EnumerateFiles(mediaFolder, "*.*", SearchOption.AllDirectories)
                .Where(f => f.Contains("BlogItem_") && (f.EndsWith(".jpg") || f.EndsWith(".png") || f.EndsWith(".gif") || f.EndsWith(".webp")))
                .Count();
        }
    }

    public class LocationDto
    {
        public int LocationID { get; set; }
        public string? Route { get; set; }
    }

    public class BlogItemDto
    {
        public int ContentID { get; set; }
        public int LocationID { get; set; }
        public int ContentType { get; set; }
        public string? ContentTypeName { get; set; }
        public string? MainFileName { get; set; }
        public string? FileNameRoot { get; set; }  
        public string? BlogFolderUrl { get; set; } 
    }




} // Namespace Common


