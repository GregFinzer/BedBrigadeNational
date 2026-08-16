using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Data.Services;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Hosting;
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
        private readonly IWebHostEnvironment _hostingEnv;
        private readonly string _webRootPath;
        private const string imageRotatorTag = "ImageRotator";

        public LoadImagesService(IConfigurationDataService configurationDataService, 
            ICachingService cachingService,
            IWebHostEnvironment hostingEnv)
        {
            _configurationDataService = configurationDataService;
            _cachingService = cachingService;
            _hostingEnv = hostingEnv;
            _webRootPath = !string.IsNullOrWhiteSpace(_hostingEnv.WebRootPath)
                ? _hostingEnv.WebRootPath
                : Path.Combine(_hostingEnv.ContentRootPath, "wwwroot");
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

        /// <summary>
        /// This is used when editing the content of a page to set to the first image found for the image rotator
        /// </summary>
        /// <param name="path"></param>
        /// <param name="html"></param>
        /// <returns></returns>
        public string SetImgSourceForImageRotators(string path, string html)
        {
            //This will return example: leftImageRotator, middleImageRotator, rightImageRotator
            List<string> imgIds = GetImgIdsWithRotator(html);

            foreach (var imgId in imgIds)
            {
                //The image rotator is in the same path as the page.
                //Example: media/grove-city/pages/Donate/leftImageRotator/Bedding.jpg
                string? replaced = HandleImagePath(path, html, imgId);
                if (replaced != null)
                {
                    html = replaced;
                    continue;
                }

                //The image rotator is on another page.
                //Example: media/grove-city/pages/someOtherPage/leftImageRotator/Bedding.jpg
                replaced = HandleSharedImageRotator(imgId, html);
                if (replaced != null)
                {
                    html = replaced;
                    continue;
                }

                // image source file not found - get "No Image Found" -  VS 9/4/2024                                
                html = ReplaceImageSrc(html, imgId, Defaults.ErrorImagePath); // Image Not Found URL
            }

            return html;
        }

        private string? HandleImagePath(string path, string html, string imgId)
        {
            List<string> images = GetImagesForArea(path, imgId);

            if (images.Count > 0)
            {
                var image = images.First().Replace("wwwroot/", "");
                return ReplaceImageSrc(html, imgId, image);
            }
            return null;
        }

        private string? GetPathForSharedImageRotator(string imageId, string html)
        {
            string? imageSource = GetImageSrcById(html, imageId);
            if (imageSource == null)
                return null;
            
            int imageIdIndex = imageSource.LastIndexOf(imageId, StringComparison.OrdinalIgnoreCase);
            if (imageIdIndex == -1)
            {
                return null; 
            }

            string path = imageSource.Substring(0, imageIdIndex);
            path = path.TrimEnd('/').TrimStart('/');
            if (path.StartsWith("media") || path.StartsWith("Media"))
            {
                path = path.Substring("media".Length).TrimStart('/');
            }

            return path;
        }
        
        private string? HandleSharedImageRotator(string imageId, string html)
        {
            string? path = GetPathForSharedImageRotator(imageId, html);
            
            if (!string.IsNullOrWhiteSpace(path))
            {
                return HandleImagePath(path, html, imageId);
            }
            
            return null;
        }

        public void EnsureDirectoriesExist(string path, string html)
        {
            string normalizedPath = NormalizeMediaPath(path);
            MediaPathUtil.GetMediaDirectory(_hostingEnv.ContentRootPath, normalizedPath);

            //Ensure directory exists for each image rotator
            List<string> imgIds = GetImgIdsWithRotator(html);
            foreach (var imgId in imgIds)
            {
                MediaPathUtil.GetMediaDirectory(_hostingEnv.ContentRootPath, normalizedPath, imgId);
            }
        }

        public string? GetImageSrcById(string html, string id)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var imgNode = doc.DocumentNode.SelectSingleNode($"//img[@id='{id}']");
            if (imgNode != null)
            {
                return imgNode.GetAttributeValue("src", null);
            }

            return null;
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
                    SetImageNode(path, originalHtml, node, Id, Src);
                }
            }

            return doc.DocumentNode.OuterHtml;
        }

        private void SetImageNode(string path, string originalHtml, HtmlNode node, string Id, string Src)
        {
            string attributeValue = node.Attributes[Id].Value;
            if (attributeValue.Contains(imageRotatorTag))
            {
                string currentSrc = node.Attributes[Src].Value;

                //Normal images in the path of the page
                if (currentSrc.ToLower().Contains(path.ToLower()))
                {
                    node.Attributes[Src].Value = GetRotatedImage(path, attributeValue);
                }
                else
                {
                    //Shared images from another page
                    string? sharedPath = GetPathForSharedImageRotator(attributeValue, originalHtml);
                    if (!string.IsNullOrWhiteSpace(sharedPath))
                    {
                        node.Attributes[Src].Value = GetRotatedImage(sharedPath, attributeValue);
                    }
                    else
                    {
                        node.Attributes[Src].Value = GetRotatedImage(path, attributeValue);
                    }
                }
            }
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
            string? resolvedDirectory = FileUtil.ResolveCaseInsensitivePath(directory);
            string cacheKey = _cachingService.BuildCacheKey(Defaults.GetFilesCacheKey, resolvedDirectory ?? directory);
            List<string>? cachedFiles = _cachingService.Get<List<string>?>(cacheKey);
            if (cachedFiles != null)
            {
                return cachedFiles;
            }

            if (!string.IsNullOrWhiteSpace(resolvedDirectory) && Directory.Exists(resolvedDirectory))
            {
                var fileNames = Directory.GetFiles(resolvedDirectory)
                    .Select(ToWebRelativePath)
                    .Where(pathValue => !string.IsNullOrWhiteSpace(pathValue))
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


            return relativePath;
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
            string normalizedPath = NormalizeMediaPath(path);
            return MediaPathUtil.ResolveExistingMediaPath(_hostingEnv.ContentRootPath, normalizedPath, area)
                   ?? Path.Combine(MediaPathUtil.GetPreferredMediaRoot(_hostingEnv.ContentRootPath), normalizedPath, area);
        }

        private static string NormalizeMediaPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '/', '\\');
        }
    }
}
