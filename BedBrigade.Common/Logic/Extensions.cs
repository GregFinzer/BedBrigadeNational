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
    }
}
