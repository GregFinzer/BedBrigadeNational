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

        public static string GetHtml(string fileName)
        {
            string filePath = $"{FileUtil.GetSeedingDirectory()}/SeedHtml/{fileName}";

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
    }
}
