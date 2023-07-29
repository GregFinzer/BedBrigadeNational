using BedBrigade.Common;
using BedBrigade.Data.Services;
using HtmlAgilityPack;

namespace BedBrigade.Client.Services
{
    public class LoadImagesService : ILoadImagesService
    {
        private const string imageRotatorTag = "ImageRotator";
        const string mediaDirectory = "wwwroot/media";
        private readonly ICachingService _cachingService;

        public string SetImgSourceForImageRotators(string path, string html)
        {
            List<string> imgIds = GetImgIdsWithRotator(html);
            foreach (var imgId in imgIds)
            {
                List<string> images = GetImagesForArea(path, imgId);

                if (images.Count > 0)
                {
                    var image =  images.First().Replace("wwwroot/", "");
                    html = ReplaceImageSrc(html, imgId, image);
                }
            }

            return html;
        }

        public void EnsureDirectoriesExist(string path, string html)
        {
            //Ensure directory exists for the path
            string directory = $"{mediaDirectory}/{path}";
            directory = directory.Replace("//", "/");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            //Ensure directory exists for each image rotator
            List<string> imgIds = GetImgIdsWithRotator(html);
            foreach (var imgId in imgIds)
            {
                directory = $"{mediaDirectory}/{path}/{imgId}";
                directory = directory.Replace("//", "/");

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }

        public string ReplaceImageSrc(string html, string id, string newSrc)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var imgNode = doc.DocumentNode.SelectSingleNode($"//img[@id='{id}']");
            if (imgNode != null)
            {
                imgNode.SetAttributeValue("src", newSrc);
            }

            return doc.DocumentNode.OuterHtml;
        }

        public List<string> GetImgIdsWithRotator(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var imgIds = new List<string>();

            foreach (var img in doc.DocumentNode.Descendants("img"))
            {
                var idAttribute = img.Attributes["id"];
                if (idAttribute != null && idAttribute.Value.Contains(imageRotatorTag))
                {
                    imgIds.Add(idAttribute.Value);
                }
            }

            return imgIds;
        }

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
                    if (attributeValue.Contains(imageRotatorTag))
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
            string directory = $"{mediaDirectory}/{path}/{area}";
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
