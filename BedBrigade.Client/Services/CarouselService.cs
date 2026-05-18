using System.Text;
using System.Text.RegularExpressions;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Logic;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Hosting;
using Serilog;

namespace BedBrigade.Client.Services
{


    public class CarouselService : ICarouselService
    {
        private readonly ICachingService _cachingService;
        private readonly string _webRootPath;
        private const string _pattern = "<div\\s+data-component=\\\"bbcarousel\\\"\\s+id=\\\"(.*?)\\\"\\s+src=\\\"(.*?)\\\".*?>.*?</div>";
        private static readonly Regex _regex = new Regex(_pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public CarouselService(ICachingService cachingService, IWebHostEnvironment hostingEnv)
        {
            _cachingService = cachingService;
            _webRootPath = !string.IsNullOrWhiteSpace(hostingEnv.WebRootPath)
                ? hostingEnv.WebRootPath
                : Path.Combine(hostingEnv.ContentRootPath, "wwwroot");
        }

        public string ReplaceCarousel(string htmlText)
        {
            if (string.IsNullOrWhiteSpace(htmlText))
            {
                return string.Empty;
            }

            var tags = GetBbCarouselTags(htmlText);

            if (!tags.Any())
                return htmlText;

            foreach (var tag in tags)
            {
                var images = GetImages(tag.Src);
                if (images.Count == 0)
                {
                    Log.Warning("Carousel directory not found or empty for {CarouselSource}", tag.Src);
                    continue;
                }

                htmlText = htmlText.Replace(tag.Tag, GenerateCarouselHtml(tag.Id, images));
            }

            return htmlText;
        }

        private List<string> GetImages(string subDirectory)
        {
            string normalizedSubDirectory = NormalizeSubDirectory(subDirectory);
            string contentRootPath = Path.GetDirectoryName(_webRootPath) ?? string.Empty;
            string directory = MediaPathUtil.ResolveExistingMediaPath(contentRootPath, normalizedSubDirectory)
                               ?? Path.Combine(_webRootPath, normalizedSubDirectory.Replace('/', Path.DirectorySeparatorChar));
            string cacheKey = _cachingService.BuildCacheKey(Defaults.GetFilesCacheKey, directory);

            List<string>? cachedFiles = _cachingService.Get<List<string>?>(cacheKey);
            if (cachedFiles != null)
            {
                return cachedFiles;
            }

            if (Directory.Exists(directory))
            {
                var fileNames = Directory.GetFiles(directory)
                    .Select(ToWebRelativePath)
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .ToList();
                _cachingService.Set(cacheKey, fileNames);
                return fileNames;
            }

            _cachingService.Set(cacheKey, new List<string>());
            return new List<string>();
        }

        private string ToWebRelativePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return string.Empty;
            }

            string relativePath = Path.GetRelativePath(_webRootPath, filePath)
                .Replace('\\', '/');

            return relativePath.StartsWith("/", StringComparison.Ordinal)
                ? relativePath
                : "/" + relativePath;
        }

        private static string NormalizeSubDirectory(string subDirectory)
        {
            if (string.IsNullOrWhiteSpace(subDirectory))
            {
                return string.Empty;
            }

            string normalized = subDirectory.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '/', '\\');

            if (normalized.StartsWith("media/", StringComparison.OrdinalIgnoreCase))
            {
                return normalized.Substring("media/".Length);
            }

            return normalized;
        }

        private List<(string Tag, string Id, string Src)> GetBbCarouselTags(string htmlText)
        {
            List<(string Tag, string Id, string Src)> results = new List<(string Tag, string Id, string Src)>();

            
            MatchCollection matches = _regex.Matches(htmlText);

            foreach (Match match in matches)
            {
                if (match.Groups.Count == 3)
                {
                    results.Add((match.Value, match.Groups[1].Value, match.Groups[2].Value));
                }
            }

            return results;
        }


        /// <summary>
        /// Create HTML for a Bootstrap 5 Carousel
        /// See:  https://getbootstrap.com/docs/5.0/components/carousel/
        /// JavaScript is used to run it once the page loads.  See:  BedBrigade.js runCarousel
        /// </summary>
        /// <param name="divId"></param>
        /// <param name="imagePaths"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private string GenerateCarouselHtml(string divId, List<string> imagePaths)
        {
            if (string.IsNullOrEmpty(divId)) throw new ArgumentException("divId cannot be null or empty");
            if (imagePaths == null || imagePaths.Count == 0) throw new ArgumentException("imagePaths must contain at least one image");

            StringBuilder html = new StringBuilder();

            html.AppendLine($"<div id=\"{divId}\" class=\"carousel slide carousel-fade\">");
            html.AppendLine("  <div class=\"carousel-indicators\">");

            for (int i = 0; i < imagePaths.Count; i++)
            {
                html.AppendLine($"    <button type=\"button\" data-bs-target=\"#{divId}\" data-bs-slide-to=\"{i}\" {(i == 0 ? "class=\"active\" aria-current=\"true\"" : "")} aria-label=\"Slide {i + 1}\"></button>");
            }

            html.AppendLine("  </div>");
            html.AppendLine("  <div class=\"carousel-inner\">");

            for (int i = 0; i < imagePaths.Count; i++)
            {
                html.AppendLine($"    <div class=\"carousel-item{(i == 0 ? " active" : "")}\">");
                html.AppendLine($"      <img src=\"{imagePaths[i]}\" class=\"d-block w-100\" alt=\"Slide {i + 1}\">");
                html.AppendLine("    </div>");
            }

            const string spanClass = " <span class=";
            html.AppendLine("  </div>");
            html.AppendLine($"  <button class=\"carousel-control-prev\" type=\"button\" data-bs-target=\"#{divId}\" data-bs-slide=\"prev\">");
            html.AppendLine($"    {spanClass}\"carousel-control-prev-icon\" aria-hidden=\"true\"></span>");
            html.AppendLine($"    {spanClass}\"visually-hidden\">Previous</span>");
            html.AppendLine("  </button>");
            html.AppendLine($"  <button class=\"carousel-control-next\" type=\"button\" data-bs-target=\"#{divId}\" data-bs-slide=\"next\">");
            html.AppendLine($"    {spanClass}\"carousel-control-next-icon\" aria-hidden=\"true\"></span>");
            html.AppendLine($"    {spanClass}\"visually-hidden\">Next</span>");
            html.AppendLine("  </button>");
            html.AppendLine("</div>");

            return html.ToString();
        }
    }
}
