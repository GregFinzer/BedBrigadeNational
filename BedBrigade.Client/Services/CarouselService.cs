using System.Text;
using System.Text.RegularExpressions;
using BedBrigade.Common.Constants;
using BedBrigade.Data.Services;

namespace BedBrigade.Client.Services
{


    public class CarouselService : ICarouselService
    {
        private readonly ICachingService _cachingService;
        private const string _pattern = "<div\\s+data-component=\\\"bbcarousel\\\"\\s+id=\\\"(.*?)\\\"\\s+src=\\\"(.*?)\\\".*?>.*?</div>";
        private static readonly Regex _regex = new Regex(_pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public CarouselService(ICachingService cachingService)
        {
            _cachingService = cachingService;
        }

        public string ReplaceCarousel(string htmlText)
        {
            var tags = GetBbCarouselTags(htmlText);

            if (!tags.Any())
                return htmlText;

            foreach (var tag in tags)
            {
                var images = GetImages(tag.Src);
                htmlText = htmlText.Replace(tag.Tag, GenerateCarouselHtml(tag.Id, images));
            }

            return htmlText;
        }

        private List<string> GetImages(string subDirectory)
        {
            string directory = Path.Combine("wwwroot", subDirectory.Replace('/', Path.DirectorySeparatorChar));
            string cacheKey = _cachingService.BuildCacheKey(Defaults.GetFilesCacheKey, directory);

            List<string>? cachedFiles = _cachingService.Get<List<string>?>(cacheKey);
            if (cachedFiles != null)
            {
                return cachedFiles;
            }

            if (Directory.Exists(directory))
            {
                var fileNames = Directory.GetFiles(directory).ToList();
                _cachingService.Set(cacheKey, fileNames);
                return fileNames;
            }

            _cachingService.Set(cacheKey, new List<string>());
            return new List<string>();
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
                string imagePath = imagePaths[i];
                imagePath = imagePath.Replace("wwwroot\\", "");
                imagePath = imagePath.Replace("\\", "/");

                html.AppendLine($"      <img src=\"{imagePath}\" class=\"d-block w-100\" alt=\"Slide {i + 1}\">");
                html.AppendLine("    </div>");
            }

            html.AppendLine("  </div>");
            html.AppendLine($"  <button class=\"carousel-control-prev\" type=\"button\" data-bs-target=\"#{divId}\" data-bs-slide=\"prev\">");
            html.AppendLine("    <span class=\"carousel-control-prev-icon\" aria-hidden=\"true\"></span>");
            html.AppendLine("    <span class=\"visually-hidden\">Previous</span>");
            html.AppendLine("  </button>");
            html.AppendLine($"  <button class=\"carousel-control-next\" type=\"button\" data-bs-target=\"#{divId}\" data-bs-slide=\"next\">");
            html.AppendLine("    <span class=\"carousel-control-next-icon\" aria-hidden=\"true\"></span>");
            html.AppendLine("    <span class=\"visually-hidden\">Next</span>");
            html.AppendLine("  </button>");
            html.AppendLine("</div>");

            return html.ToString();
        }
    }
}
