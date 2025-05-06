using BedBrigade.Common.Constants;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace BedBrigade.Common.Logic
{
    public class ImageUtil
    {
        public DateTime? OverrideDateTime { get; set; }

        public string ComputeImageToDisplay(List<string> fileNames)
        {
            if (fileNames.Count > 0)
            {
                var currentDateTime = OverrideDateTime ?? DateTime.UtcNow;
                var unit = currentDateTime.Hour * 2 + (currentDateTime.Minute > 30 ? 1 : 0);
                var imageIndex = unit % fileNames.Count;
                return fileNames[imageIndex].Replace("wwwroot/", "");
            }
            return Defaults.ErrorImagePath;
        }

        /// <summary>
        /// Loads an image from disk, resizes it to the given max width (preserving aspect ratio),
        /// and saves it as a WebP to the destination path.
        /// </summary>
        /// <param name="sourcePath">Path to the source image (jpg, png, gif or webp).</param>
        /// <param name="destinationPath">Path where the WebP thumbnail will be saved.</param>
        /// <param name="maxWidth">Maximum width of the thumbnail in pixels.</param>
        /// <param name="quality">Optional WebP quality (0–100, defaults to 75).</param>
        public static void CreateThumbnail(string sourcePath, string destinationPath, int maxWidth, int quality = 75)
        {
            // Validate parameters
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new ArgumentException("Source path must be provided.", nameof(sourcePath));
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Source image not found.", sourcePath);
            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException("Destination path must be provided.", nameof(destinationPath));
            if (maxWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxWidth), "Max width must be greater than zero.");

            // Load the source image (ImageSharp auto-detects the format)
            using (Image image = Image.Load(sourcePath))
            {
                // Calculate resize dimensions
                if (image.Width > maxWidth)
                {
                    double ratio = (double)maxWidth / image.Width;
                    int newHeight = (int)Math.Round(image.Height * ratio);

                    image.Mutate(x => x.Resize(maxWidth, newHeight));
                }

                // Prepare WebP encoder with given quality
                var webpEncoder = new WebpEncoder
                {
                    Quality = quality
                };

                // Save as WebP
                image.Save(destinationPath, webpEncoder);
            }
        }

        public static string GetThumbnailFileName(string path)
        {
            return Path.GetFileNameWithoutExtension(path) + "_thumb.webp";
        }
    }
}
