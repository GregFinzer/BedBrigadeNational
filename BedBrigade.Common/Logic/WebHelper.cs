using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace BedBrigade.Common.Logic
{
    public static class WebHelper
    {
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
    }
}
