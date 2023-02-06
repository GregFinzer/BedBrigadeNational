using System.Diagnostics;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Web;

namespace BedBrigade.Common
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
                    var trimmed = phone.Trim().Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace(".", "");
                    formatted = String.Format("{0:(###) ###-####}", Convert.ToInt64(trimmed));
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

        public static bool HasRole(this ClaimsPrincipal identity, string roles)
        {

            var roleArray = roles.Split(',');
            foreach (var role in roleArray)
            {
                if (identity.HasRole(role.Trim()))
                {
                    return true;
                }
            }
            return false;

        }

    }
}
