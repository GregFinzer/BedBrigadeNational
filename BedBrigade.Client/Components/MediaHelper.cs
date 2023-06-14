using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components;
using System.Drawing.Drawing2D;
using System.Security.Claims;
using BedBrigade.Common;

namespace BedBrigade.Client.Components
{
    public partial class MediaHelper
    {
        public static bool IsFileExists(Media dbFile)
        {
            bool response = false;
            var fileFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/" + dbFile.FilePath);
            var fullFileName = fileFolder + "/" + dbFile.FileName + "." + dbFile.MediaType;
            if (File.Exists(fullFileName))
            {
                response = true;
            }
            return response;
        } // File Exists?

        public static string FormatFileSize(long intFileSize)
        {
            string strFileSize = string.Empty;
            try
            {
                // Return formatted file size (source size in bytes)
                double dblSize = 0.0;

                if (intFileSize > 1024)
                {
                    dblSize = Convert.ToDouble(intFileSize) / 1024;
                    intFileSize = Convert.ToInt32(Math.Ceiling(dblSize));
                    strFileSize = Convert.ToString(intFileSize) + " KB";
                    if (intFileSize > 1024)
                    {
                        dblSize = Convert.ToDouble(intFileSize) / 1024;
                        strFileSize = Microsoft.VisualBasic.Strings.FormatNumber(dblSize, 1) + " MB";
                    }
                }
                else // bytes
                {
                    strFileSize = Convert.ToString(intFileSize) + " B";
                }
            }
            catch (Exception ex) { strFileSize = "???"; }
            return strFileSize;
        }//FormatFileSize
        public static string RenameFile(ref Media myFile, string OldFileName, string PathDivider, string PathDot)
        {
            var result = "norename"; // restore if file cannot be renamed                             

            if (OldFileName != null && myFile.FileName != null)
            {
                if (OldFileName.Trim() != myFile.FileName.Trim())
                { // rename file required
                    try
                    {
                        var FileLocation = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/" + myFile.FilePath);
                        string FullNameOldFile = FileLocation + PathDivider + OldFileName + PathDot + myFile.MediaType;
                        string FullNameNewFile = FileLocation + PathDivider + myFile.FileName + PathDot + myFile.MediaType;
                        File.Move(FullNameOldFile, FullNameNewFile);
                        result = "success";
                    }
                    catch (Exception ex)
                    {
                        result = "restore";
                    }
                }
            }

            if (result == "restore")
            {
                myFile.FileName = OldFileName; // restore old name in Media record
            }

            return result;

        } // Rename File

        public static void ReviewUserData(ref MediaUser MediaUser, ClaimsPrincipal Identity, string PathDot)
        {
            MediaUser.Name = Identity.Identity.Name;

            if (Identity.IsInRole(RoleNames.NationalAdmin)) // not perfect! for initial testing
            {
                MediaUser.IsAdmin = true;
                MediaUser.Role = RoleNames.NationalAdmin;

            }
            else // Location User
            {
                if (Identity.IsInRole(RoleNames.LocationAdmin))
                {
                    MediaUser.IsAdmin = true;
                    MediaUser.Role = RoleNames.LocationAdmin;
                }

                MediaUser.LocationId = int.Parse(Identity.Claims.FirstOrDefault(c => c.Type == "LocationId").Value);
            }

        } // Review User Data

        public static string GetLocationName(ref List<Location> lstLocations, int LocationId)
        {
            string locationName = "Unknown";
            try
            {
                var myLocation = lstLocations.SingleOrDefault(a => a.LocationId == LocationId);
                if (myLocation != null)
                {
                    locationName = myLocation.Name;
                }
            }
            catch (Exception ex) { }
            return locationName;
        } // Get Location Name

        public static void GetUserLocation(ref MediaUser MediaUser, ref List<Location> lstLocations, Dictionary<string, string?> dctConfiguration, string PathDivider, string SubfolderKey)
        {
            // select Location Route for User Location ID
            if (MediaUser.LocationId > 0)
            {
                var intLocationId = MediaUser.LocationId;
                //  var myLocation = lstLocations.Select()
                var myLocation = lstLocations.SingleOrDefault(a => a.LocationId == intLocationId);
                if (myLocation != null)
                {
                    MediaUser.LocationFolder = myLocation.Route;
                    MediaUser.LocationName = myLocation.Name;
                    if (MediaUser.LocationFolder == PathDivider)
                    {
                        MediaUser.LocationFolder = dctConfiguration[SubfolderKey]; // Very rare situation, because "/" is national and available only for National Admin
                    }
                    else
                    {
                        MediaUser.LocationFolder = MediaUser.LocationFolder.Replace(PathDivider, "");
                    }
                }              
            }
            else
            { // Admin User
                MediaUser.LocationFolder = dctConfiguration[SubfolderKey];
            }

            if (MediaUser.LocationId > 0)
            {
                // single location
                MediaUser.FolderList.Add(MediaUser.LocationName + " [" + PathDivider + MediaUser.LocationFolder + "]");

            }
            else
            { // the list of locations/folders

                foreach (Location myRoute in lstLocations)
                {
                    if (myRoute.Route == PathDivider)
                    {
                        MediaUser.FolderList.Add(myRoute.Name + " [" + PathDivider + dctConfiguration[SubfolderKey] + "]");
                    }
                    else
                    {
                        MediaUser.FolderList.Add(myRoute.Name + " [" + myRoute.Route + "]");
                    }
                }
            }

            UpdateUserFolderList(ref MediaUser);

            } // User Location

        public static void UpdateUserFolderList(ref MediaUser MediaUser)
        {
      
            MediaUser.FolderList.Sort();          

            if (MediaUser.FolderList.Count > 1)
            {
                if (MediaUser.Role.Contains("National"))
                {
                    // set National pre-selecterd
                    var nationalFolder = MediaUser.FolderList.Where(f => f.ToLower().Contains("national")).ToList();
                    MediaUser.DropFileFolder = nationalFolder[0];
                }
                else
                {
                    MediaUser.DropFileFolder = MediaUser.FolderList[0];
                }
            }
            else
            {
                MediaUser.DropFileFolder = MediaUser.FolderList[0];
            }

        } // User Folder List


        public static List<Media> SetLocationFilter(List<Media> dbFileList, MediaUser mediaUser)
        {
            // throw new NotImplementedException();
            // if Not National Admin User linked to location, Grid should show only location files

            if (mediaUser.LocationId > (int)LocationNumber.National || mediaUser.Role == RoleNames.LocationAdmin && mediaUser.LocationId == (int)LocationNumber.National)
            {
                List<Media> LocationFiles = dbFileList.FindAll(a => a.LocationId == mediaUser.LocationId);
                return LocationFiles;
            }
            else
            {
                return dbFileList;
            }

        } // Set Location Filter

        public class MediaUser
        {
            public string Name { get; set; } = string.Empty;
            public string SearchName { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public bool IsAdmin { get; set; } = false;
            public int LocationId { get; set; } = 0;
            public string LocationName { get; set; } = string.Empty;
            public string LocationFolder { get; set; } = string.Empty;
            public List<string> FolderList { get; set; } = new List<string>(); // LocationName + [location Folder]
            public string DropFileFolder { get; set; } = string.Empty;
                      
        }


    }
}
