using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace BedBrigade.Common.Logic
{
    public static class StringUtil
    {
        private const char spaceChar = ' ';
        private const string hrefExpression = @"<a[^>]*>.*<\/a>";
        private static Regex _hrefRegex = new Regex(hrefExpression, RegexOptions.Compiled);
        private const string javaScriptExpression = @"<a.+javascript[^>]+>[^>]+>";
        private static Regex _javaScriptRegex = new Regex(javaScriptExpression, RegexOptions.Compiled);

        public static string ProperCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToUpper(input[0]) + input.Substring(1).ToLower();
        }

        public static string FilterAlphaNumeric(string input)
        {
            var sb = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string FilterAlphanumericAndDash(string input)
        {
            var sb = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c) || c == '-')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static string RestoreHrefWithJavaScript(string? original, string? updated)
        {
            original ??= string.Empty;
            updated ??= string.Empty;

            var originalMatches = _javaScriptRegex.Matches(original);

            if (originalMatches.Count == 0)
                return updated;

            var updatedLinks = _hrefRegex.Matches(updated);

            var sb = new StringBuilder(updated, updated.Length * 2);
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
            if (string.IsNullOrEmpty(searchText))
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
            if (string.IsNullOrEmpty(searchText))
                return searchText;

            int startTagPos = searchText.IndexOf(startTag, StringComparison.Ordinal);

            if (startTagPos < 0)
                return string.Empty;

            int endTagPos = searchText.IndexOf(endTag, startTagPos + startTag.Length, StringComparison.Ordinal);

            if (endTagPos < 0)
                return string.Empty;

            return searchText.Substring(startTagPos + startTag.Length, endTagPos - startTagPos - startTag.Length);
        }

        /// <summary>
        /// Insert spaces into a string 
        /// </summary>
        /// <example>
        /// OrderDetails = Order Details
        /// 10Net30 = 10 Net 30
        /// FTPHost = FTP Host
        /// </example> 
        /// <param name="input"></param>
        /// <returns></returns>
        public static string InsertSpaces(string input)
        {
            const string space = " ";
            bool isSpace = false;
            bool isUpperOrNumber = false;
            bool isLower = false;
            bool isLastUpper = true;
            bool isNextCharLower = false;

            if (string.IsNullOrEmpty(input))
                return string.Empty;

            StringBuilder sb = new StringBuilder(input.Length + input.Length / 2);

            //Replace underline with spaces
            input = input.Replace("_", space);
            input = input.Replace("-", space);
            input = input.Replace("  ", space);

            //Trim any spaces
            input = input.Trim();

            char[] chars = input.ToCharArray();

            sb.Append(chars[0]);

            for (int i = 1; i < chars.Length; i++)
            {
                isUpperOrNumber = chars[i] >= 'A' && chars[i] <= 'Z' || chars[i] >= '0' && chars[i] <= '9';
                isNextCharLower = i < chars.Length - 1 && chars[i + 1] >= 'a' && chars[i + 1] <= 'z';
                isSpace = chars[i] == ' ';
                isLower = chars[i] >= 'a' && chars[i] <= 'z';

                //There was a space already added
                if (isSpace)
                {
                }
                //Look for upper case characters that have lower case characters before
                //Or upper case characters where the next character is lower
                else if (isUpperOrNumber && isLastUpper == false
                    || isUpperOrNumber && isNextCharLower && isLastUpper == true)
                {
                    sb.Append(space);
                    isLastUpper = true;
                }
                else if (isLower)
                {
                    isLastUpper = false;
                }

                sb.Append(chars[i]);

            }

            //Replace double spaces
            sb.Replace("  ", space);

            return sb.ToString();
        }

        /// <summary>
        /// Replaces the tag value.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="beginTag">The begin tag.</param>
        /// <param name="endTag">The end tag.</param>
        /// <param name="replaceText">The replace text.</param>
        /// <returns>System.String.</returns>
        public static string ReplaceTagValue(string text,
            string beginTag,
            string endTag,
            string replaceText)
        {
            int beginTagPos = text.IndexOf(beginTag);

            if (beginTagPos < 0)
                return text;

            string leftText = string.Empty;
            int endTagPos = text.IndexOf(endTag, beginTagPos + beginTag.Length);

            if (endTagPos <= 0)
                return text;

            string rightText = string.Empty;

            leftText = text.Substring(0, beginTagPos + beginTag.Length);

            rightText = text.Substring(endTagPos);

            return string.Format("{0}{1}{2}", leftText, replaceText, rightText);
        }

        public static string IsNull(string expression, string replacement)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return replacement;
            }
            else
            {
                return expression;
            }
        } // Is Null


        public static string ExtractDigits(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return new string(input.Where(char.IsDigit).ToArray());
        }

        /// <summary>
        /// Removes currency characters from a string like $, and spaces using the current culture
        /// </summary>
        /// <param name="currency">Currency string to parse</param>
        /// <returns>Pure currency with no formatting</returns>
        public static string RemoveCurrency(string currency)
        {
            string mask = @"\s|[$]";
            string symbol = Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencySymbol.ToString();

            //Get the dollar sign for the current culture
            if (symbol.Length > 0)
            {
                mask = mask.Replace("$", symbol);
            }

            currency = Regex.Replace(currency, mask, "");

            if (currency.StartsWith("(") && currency.EndsWith(")") && currency.Length > 2)
            {
                currency = "-" + currency.Substring(1, currency.Length - 2);
            }

            return currency.Trim();
        }

        public static string CleanUpSpacesAndLineFeeds(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input.Replace('\r', spaceChar).Replace('\n', spaceChar).Replace('\t', spaceChar).Replace("  ", " ");
        }

    } // class


} // name space
