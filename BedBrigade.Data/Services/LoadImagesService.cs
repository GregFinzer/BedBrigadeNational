using BedBrigade.Common;
using HtmlAgilityPack;

namespace BedBrigade.Data.Services
{
    public class LoadImagesService : ILoadImagesService
    {
        private readonly ICachingService _cachingService;

        public LoadImagesService(ICachingService cachingService)
        {
            _cachingService = cachingService;
        }

        /// <summary>
        /// Get a rotated image for the path and area
        /// </summary>
        /// <param name="path">Path for the location</param>
        /// <param name="area">Id of the image rotator</param>
        /// <returns></returns>
        /// <example>
        /// path = "national\pages\home"
        /// area = "headerImageRotator
        /// </example>
        public string GetRotatedImage(string path, string area)
        {
            var images = GetImagesForArea(path, area);
            ImageRotatorLogic rotatorLogic = new ImageRotatorLogic();
            return rotatorLogic.ComputeImageToDisplay(images);
        }

        /// <summary>
        /// Sets the rotated images for the html
        /// </summary>
        /// <param name="path">Path for the location</param>
        /// <param name="originalHtml"></param>
        /// <example>
        /// path = "national\pages\home"
        /// </example>
        /// <returns></returns>
        public string SetImagesForHtml(string path, string originalHtml)
        {
            const string Src = "src";
            const string Id = "id";
            var doc = new HtmlDocument();
            doc.LoadHtml(originalHtml);
            var nodes = doc.DocumentNode.SelectNodes("//img");
            foreach (var node in nodes)
            {
                if (node.Attributes[Id] != null)
                {
                    string attributeValue = node.Attributes[Id].Value;
                    if (attributeValue.Contains("ImageRotator"))
                    {
                        node.Attributes[Src].Value = GetRotatedImage(path, attributeValue);
                    }
                }
            }

            return doc.DocumentNode.OuterHtml;
        }

        /// <summary>
        /// Gets all the images for a given path
        /// </summary>
        /// <param name="path">Path for the location</param>
        /// <param name="area">Id of the image rotator</param>
        /// <returns></returns>
        /// <example>
        /// path = "national\pages\home"
        /// area = "headerImageRotator
        /// </example>
        public List<string> GetImagesForArea(string path, string area)
        {
            string directory = $"wwwroot/media/{path}/{area}";
            directory = directory.Replace("//", "/");
            string cacheKey = _cachingService.BuildCacheKey("Directory.GetFiles", directory);
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
    }
}
