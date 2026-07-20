using AngleSharp.Html;
using AngleSharp.Html.Parser;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace BedBrigade.Common.Logic
{
    public static class WebHelper
    {
        private static Regex _stripHtmlRegex = new Regex("<[^>]+>", RegexOptions.IgnoreCase| RegexOptions.Compiled);

        public static bool IsProduction()
        {
            string? environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return environment == "Production";
        }

        public static bool IsDevelopment()
        {
            string? environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return environment == "Development";
        }

        public static bool IsMailToLink(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            return text.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a valid mailto link.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <param name="subject">Optional subject.</param>
        /// <param name="body">Optional body.</param>
        /// <returns>A valid mailto URI.</returns>
        /// <exception cref="ArgumentException">Thrown when email is null or empty.</exception>
        public static string CreateMailToLink(
            string email,
            string? subject = null,
            string? body = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));

            var queryParameters = new List<string>();

            if (!string.IsNullOrWhiteSpace(subject))
            {
                queryParameters.Add($"subject={Uri.EscapeDataString(subject)}");
            }

            if (!string.IsNullOrWhiteSpace(body))
            {
                queryParameters.Add($"body={Uri.EscapeDataString(body)}");
            }

            if (queryParameters.Count == 0)
            {
                return $"mailto:{email}";
            }

            return $"mailto:{email}?{string.Join("&", queryParameters)}";
        }

        public static string GetSeedingFile(string fileName)
        {
            var seedingDirectory = FileUtil.GetSeedingDirectory();
            var seedHtmlDirectory = Path.Combine(seedingDirectory, "SeedHtml");
            string filePath = Path.Combine(seedHtmlDirectory, fileName);

            if (!File.Exists(filePath))
            {
                // Case-insensitive fallback for Linux file systems.
                var matchedFile = Directory.EnumerateFiles(seedHtmlDirectory)
                    .FirstOrDefault(path => string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(matchedFile))
                {
                    filePath = matchedFile;
                }
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var html = File.ReadAllText(filePath);
            return html;
        }

        public static string StripHTML(string HTMLText, bool decode = true)
        {
            string stripped = string.Empty;
            try
            {
                if (HTMLText != null)
                    stripped = _stripHtmlRegex.Replace(HTMLText, "");
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                Console.WriteLine(msg);
            }
            return decode ? HttpUtility.HtmlDecode(stripped) : stripped;
        }

        /// <summary>
        /// Pretty-formats an HTML string but does NOT inject <html>/<body> wrappers.
        /// </summary>
        public static string FormatHtml(string html, string indent = "  ", string newLine = "\r\n")
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            var parser = new HtmlParser();

            // Parse as a fragment instead of a full document
            var context = parser.ParseDocument(""); // empty doc just to get a context
            var fragment = parser.ParseFragment(html, context.Body);

            var formatter = new PrettyMarkupFormatter
            {
                Indentation = indent,
                NewLine = newLine
            };

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                foreach (var node in fragment)
                {
                    node.ToHtml(writer, formatter);
                }
            }

            return sb.ToString();
        }
    }
}
