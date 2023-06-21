using System.Text;
using System.Text.RegularExpressions;

namespace BedBrigade.Common
{
    public static class StringUtil
    {
        private const string hrefExpression = @"<a[^>]*>.*<\/a>";
        private static Regex _hrefRegex = new Regex(hrefExpression, RegexOptions.Compiled);
        private const string javaScriptExpression = @"<a.+javascript[^>]+>[^>]+>";
        private static Regex _javaScriptRegex = new Regex(javaScriptExpression, RegexOptions.Compiled);

        public static string RestoreHrefWithJavaScript(string? original, string? updated)
        {
            original ??= string.Empty;
            updated ??= string.Empty;

            var originalMatches = _javaScriptRegex.Matches(original);

            if (originalMatches.Count == 0)
                return updated;
            
            var updatedLinks = _hrefRegex.Matches(updated);

            var sb = new StringBuilder(updated, updated.Length*2);
            foreach (Match originalMatch in originalMatches)
            {
                var originalLink = originalMatch.Value;
                var originalHrefText = GetBetweenTags(originalLink, ">", "</a>");

                foreach (Match updatedLinkMatch in updatedLinks)
                {
                    var updatedLink = updatedLinkMatch.Value;
                    if (updatedLink.Contains($">{originalHrefText}</a>"))
                    {
                        sb.Replace(updatedLink, originalLink);
                        break;
                    }
                }
            }

            return sb.ToString();
        }

        public static string GetTag(string searchText, string startTag, string endTag)
        {
            if (String.IsNullOrEmpty(searchText))
                return searchText;

            int startTagPos = searchText.IndexOf(startTag, StringComparison.Ordinal);

            if (startTagPos < 0)
                return string.Empty;

            int endTagPos = searchText.IndexOf(endTag, startTagPos + startTag.Length, StringComparison.Ordinal);

            if (endTagPos < 0)
                return string.Empty;

            return searchText.Substring(startTagPos, endTagPos - startTagPos + endTag.Length);
        }

        public static string GetBetweenTags(string searchText, string startTag, string endTag)
        {
            if (String.IsNullOrEmpty(searchText))
                return searchText;

            int startTagPos = searchText.IndexOf(startTag, StringComparison.Ordinal);

            if (startTagPos < 0)
                return string.Empty;

            int endTagPos = searchText.IndexOf(endTag, startTagPos + startTag.Length, StringComparison.Ordinal);

            if (endTagPos < 0)
                return string.Empty;

            return searchText.Substring(startTagPos + startTag.Length, endTagPos - startTagPos - startTag.Length);
        }
    }
}
