using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Data.Services;
using HtmlAgilityPack;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using StringUtil = BedBrigade.Common.Logic.StringUtil;

namespace BedBrigade.Client.Services
{
    public class LoadImagesService : ILoadImagesService
    {
        private readonly IConfigurationDataService _configurationDataService;
        private readonly ICachingService _cachingService;
        private const string imageRotatorTag = "ImageRotator";
        private const string mediaDirectory = "wwwroot/media";

        public LoadImagesService(IConfigurationDataService configurationDataService, 
            ICachingService cachingService)
        {
            _configurationDataService = configurationDataService;
            _cachingService = cachingService;
        }

        public async Task<string> ConvertToWebp(string targetPath)
        {
            string[] convertableImageExtensions =
                (await _configurationDataService.GetConfigValueAsync(ConfigSection.Media,
                    ConfigNames.ConvertableImageExtensions))
                .Split(',');

            int maxWidth =
                await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.Media,
                    ConfigNames.ConvertableMaxWidth);

            if (!convertableImageExtensions.Contains(Path.GetExtension(targetPath)))
                return targetPath;

            // Determine source info
            var folderPath = Path.GetDirectoryName(targetPath) ?? string.Empty;
            var finalFileName = $"{Path.GetFileNameWithoutExtension(targetPath)}.webp";
            var finalPath = Path.Combine(folderPath, finalFileName);

            // Temporary GUID-based file path
            var tempFileName = $"{Guid.NewGuid()}.tmp";
            var tempPath = Path.Combine(folderPath, tempFileName);

            using (var image = await Image.LoadAsync(targetPath))
            {
                // Strip EXIF metadata
                image.Metadata.ExifProfile = null;

                // Resize to maxWidth on the longer edge
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(maxWidth, maxWidth)
                }));

                // Encode as WebP
                var encoder = new WebpEncoder
                {
                    Quality = 80
                };

                // Save into temporary file
                using (var outStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await image.SaveAsync(outStream, encoder);
                }
            }

            // Delete the original file
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            // Move temp file to final .webp path
            File.Move(tempPath, finalPath);

            return finalPath;
        }


        public string SetImgSourceForImageRotators(string path, string html)
        {
            List<string> imgIds = GetImgIdsWithRotator(html);
            foreach (var imgId in imgIds)
            {
                List<string> images = GetImagesForArea(path, imgId);

                if (images.Count > 0)
                {
                    var image = images.First().Replace("wwwroot/", "");
                    html = ReplaceImageSrc(html, imgId, image);
                }

                else
                { // image source file not found - get "No Image Found" -  VS 9/4/2024                {                 

                    html = ReplaceImageSrc(html, imgId, Defaults.ErrorImagePath); // Image Not Found URL
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
            ImageUtil rotatorLogic = new ImageUtil();
            return rotatorLogic.ComputeImageToDisplay(images);
        }

        public string GetRotatedImage(List<string> images)
        {
            ImageUtil rotatorLogic = new ImageUtil();
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

            if (nodes == null)
            {
                return originalHtml;
            }

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
            string directory = GetDirectoryForPathAndArea(path, area);
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

        public List<string> GetImagesForLocationWithDefault(string path, string area)
        {
            List<string> locationImages = GetImagesForArea(path, area);

            if (locationImages.Count > 0)
            {
                return locationImages;
            }

            return GetImagesForArea(Defaults.NationalRoute, area);
        }

        public string GetDirectoryForPathAndArea(string path, string area)
        {
            string directory = $"{mediaDirectory}/{path}/{area}";
            directory = directory.Replace("//", "/");
            return directory;
        }
    }
}
