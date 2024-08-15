using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Web;
using System.Diagnostics;
using System.Reflection;

namespace BedBrigade.Common.Logic
{
    public static class Extensions
    {
        public static string FormatPhoneNumber(this string phone)
        {
            //Console.WriteLine($"Phone Number: {phone} ");
            try
            {
                var formatted = string.Empty;
                if (!string.IsNullOrEmpty(phone))
                {
                    var trimmed = phone.Trim()
                        .Replace(" ", string.Empty)
                        .Replace("(", string.Empty)
                        .Replace(")", string.Empty)
                        .Replace("-", string.Empty)
                        .Replace(".", string.Empty);
                    formatted = string.Format("{0:(###) ###-####}", Convert.ToInt64(trimmed));
                }
                return formatted;
            }
            catch (Exception ex)
            {
                Debug.Print($"Error in phone number format {phone}");
                return string.Empty;
            }
        }

        public static string StripHTML(this string HTMLText, bool decode = true)
        {
            Regex reg = new Regex("<[^>]+>", RegexOptions.IgnoreCase);
            string stripped = string.Empty;
            try
            {
                if (HTMLText != null)
                    stripped = reg.Replace(HTMLText, "");
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Console.WriteLine(msg);
            }
            return decode ? HttpUtility.HtmlDecode(stripped) : stripped;
        }
        /// <summary>
        /// Checks if the role is authorized (found) by the Claims
        /// </summary>
        /// <param name="identity">A ClaimsPrincipal Object</param>
        /// <param name="roles">A string of comma seperated roles of which the user must have at least one</param>
        /// <returns> true or false</returns>
        public static bool HasRole(this ClaimsPrincipal identity, string roles)
        {

            var roleArray = roles.Split(',');
            foreach (var role in roleArray)
            {
                if (identity.IsInRole(role.Trim()))
                {
                    return true;
                }
            }
            return false;

        }

        /// <summary>
        /// Returns the path to the file contained in the specified foldername
        /// </summary>
        /// <param name="fileName">The name of the file to find</param>
        /// <param name="folderName">A string terminating in a folder to be searched (Note this may be a path of folders)</param>
        /// <returns>A string containing where the file was found</returns>
        public static string ToApplicationPath(this string fileName, string folderName = "")
        {
            string appRoot = GetMediaDirectory(folderName);
            return Path.Combine(appRoot, fileName);
        }


        public static bool DirectoryExists(this string directoryName)
        {
            var appRoot = GetMediaDirectory(directoryName);
            return Directory.Exists(appRoot);
        }

        public static DirectoryInfo CreateDirectory(this string directoryName)
        {
            var appRoot = GetMediaDirectory(directoryName);
            return Directory.CreateDirectory(appRoot);
        }

        public static void DeleteDirectory(this string directoryName, bool recursiveDelete)
        {
            var appRoot = GetMediaDirectory(directoryName);
            Directory.Delete(appRoot, recursiveDelete);
        }

        public static void DeleteFiles(this string directoryName)
        {
            var appRoot = GetMediaDirectory(directoryName);
            string[] files = Directory.GetFiles(appRoot);
            foreach (string file in files)
            {
                File.Delete(file);
            }
        }

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


    }
}
