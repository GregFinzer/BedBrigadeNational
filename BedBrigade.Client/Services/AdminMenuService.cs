using BedBrigade.Common.Constants;
using System.Text;
using System.Text.RegularExpressions;
using BedBrigade.Data.Services;

namespace BedBrigade.Client.Services
{
    public class AdminMenuService : IAdminMenuService
    {
        private readonly IAuthService? _auth;

        private const string _pattern =
            @"<div\s+(?=[^>]*\bdata-component=""(?<component>[^""]+)"")(?=[^>]*\bdata-style=""(?<style>[^""]*)"")(?=[^>]*\bid=""(?<id>[^""]+)"")[^>]*>\s*</div>";
        private static readonly Regex _regex = new Regex(_pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public AdminMenuService(IAuthService? authService)
        {
            _auth = authService;
        }

        public string ReplaceAdminMenu(string htmlText)
        {
            if (string.IsNullOrEmpty(htmlText))
                return htmlText;

            var match = _regex.Match(htmlText);

            if (!match.Success) return htmlText;

            string id = match.Groups["id"].Value;
            var style = match.Groups["style"].Value;
            string navItemStyle = String.IsNullOrEmpty(style) ? string.Empty : $"{style}-nav-item";
            string navLinkStyle = String.IsNullOrEmpty(style) ? string.Empty : $"{style}-nav-link";
            string dropDownItemStyle = String.IsNullOrEmpty(style) ? string.Empty : $"{style}-dropdown-item";
            string replacement= BuildAdminMenu(id, navItemStyle, navLinkStyle, dropDownItemStyle);

            return htmlText.Replace(match.Value, replacement);
        }

        private string BuildAdminMenu(string id, string navItemStyle, string navLinkStyle, string dropDownItemStyle )
        {
            if (_auth == null || !_auth.IsLoggedIn)
                return string.Empty;


            // Collect visible items based on role gates inferred from AdminMenu.html
            List<(string Text, string Href, string Icon, bool Show)> items = BuildMenuItems();

            // If nothing to show, return empty
            if (!items.Any(i => i.Show)) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine($"<li id=\"{id}\" class=\"dropdown nav-item {navItemStyle}\">");
            sb.AppendLine(
                $"  <a class=\"nav-link dropdown-toggle {navLinkStyle}\" data-bs-toggle=\"dropdown\" href=\"administration/dashboard\">");
            sb.AppendLine("    <i class=\"fas fa-tools me-1\" aria-hidden=\"true\"></i>Administration");
            sb.AppendLine("  </a>");
            sb.AppendLine("  <ul class=\"dropdown-menu admin-dropdown\">");

            foreach (var (text, href, icon, show) in items)
            {
                if (!show) continue;
                sb.AppendLine("    <li class=\"nav-item\">");
                sb.AppendLine($"      <a class=\"dropdown-item mx-2 {dropDownItemStyle}\" href=\"{href}\">");
                sb.AppendLine($"        <i class=\"{icon} me-2\" aria-hidden=\"true\"></i>{text}");
                sb.AppendLine("      </a>");
                sb.AppendLine("    </li>");
            }

            sb.AppendLine("  </ul>");
            sb.AppendLine("</li>");

            return sb.ToString();
        }

        private List<(string Text, string Href, string Icon, bool Show)> BuildMenuItems()
        {
            var items = new List<(string Text, string Href, string Icon, bool Show)>
            {
                ("Bed Requests", "administration/manage/bedrequests", "fas fa-bed",
                    _auth.UserHasRole(RoleNames.CanViewBedRequests)),
                ("Bulk Email", "administration/admin/email", "fas fa-envelope",
                    _auth.UserHasRole(RoleNames.CanSendBulkEmail)),
                ("Bulk Text Messages", "administration/SMS/BulkSms", "fas fa-sms",
                    _auth.UserHasRole(RoleNames.CanSendSms)),
                ("Configuration", "administration/manage/configuration", "fas fa-cogs", _auth.IsNationalAdmin),
                ("Contacts", "administration/manage/Contacts", "fas fa-address-book",
                    _auth.UserHasRole(RoleNames.CanViewContacts)),
                ("Dashboard", "administration/dashboard", "fas fa-tachometer-alt",
                    _auth.UserHasRole(RoleNames.CanViewAdminDashboard)),
                ("Donations", "administration/manage/donations", "fas fa-donate",
                    _auth.UserHasRole(RoleNames.CanManageDonations)),
                ("Locations", "administration/manage/locations", "fas fa-map-marker-alt",
                    _auth.UserHasRole(RoleNames.CanViewLocations)),
                ("Media", "administration/manage/fm", "fas fa-images", _auth.UserHasRole(RoleNames.CanManageMedia)),
                ("Metro Areas", "administration/manage/metroareas", "fas fa-city",
                    _auth.UserHasRole(RoleNames.CanViewMetroAreas)),
                ("Pages", "administration/manage/pages/Body", "fas fa-file-alt",
                    _auth.UserHasRole(RoleNames.CanViewPages)),
                ("News", "administration/manage/pages/News", "fas fa-newspaper",
                    _auth.UserHasRole(RoleNames.CanViewPages)),
                ("Newsletters", "administration/manage/newsletters", "fas fa-paper-plane",
                    _auth.UserHasRole(RoleNames.CanManageNewsletters)),
                ("Stories", "administration/manage/pages/Stories", "fas fa-book-open",
                    _auth.UserHasRole(RoleNames.CanViewPages)),
                ("Server Info", "administration/admin/serverinfo", "fas fa-server",
                    _auth.UserHasRole(RoleNames.NationalAdmin)),
                ("Schedules", "administration/manage/schedules", "fas fa-calendar-alt",
                    _auth.UserHasRole(RoleNames.CanViewSchedule)),
                ("Sign-Ups", "administration/manage/sign-ups", "fas fa-user-plus",
                    _auth.UserHasRole(RoleNames.CanViewSignUps)),
                ("Text Messages", "administration/SMS/SmsSummary", "fas fa-sms",
                    _auth.UserHasRole(RoleNames.CanViewSmsSummary)),
                ("Users", "administration/manage/users", "fas fa-users", _auth.UserHasRole(RoleNames.CanViewUsers)),
                ("View Logs", "administration/admin/viewlogs", "fas fa-clipboard-list",
                    _auth.UserHasRole(RoleNames.NationalAdmin)),
                ("Volunteers", "administration/manage/Volunteers", "fas fa-user-friends",
                    _auth.UserHasRole(RoleNames.CanViewVolunteers)),
            };
            return items;
        }
    }
}
