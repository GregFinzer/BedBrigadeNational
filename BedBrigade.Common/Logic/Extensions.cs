using System.Security.Claims;

namespace BedBrigade.Common.Logic
{
    public static class Extensions
    {
        public static string FormatPhoneNumber(this string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return phone;

            if (phone.StartsWith("+1") && phone.Length > 2)
                phone = phone.Substring(2);

            var numbersOnly = StringUtil.ExtractDigits(phone);

            if (numbersOnly.Length != 10)
                return phone;

            return string.Format("{0:(###) ###-####}", long.Parse(numbersOnly));
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
