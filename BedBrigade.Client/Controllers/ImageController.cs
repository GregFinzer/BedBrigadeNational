using BedBrigade.Common.Constants;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp.Formats.Webp;
using System.Net.Http.Headers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ImageUpload.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly IWebHostEnvironment hostingEnv;
        private readonly IAuthService _authService;
        private readonly ILocationDataService _svcLocation;
        private readonly ICachingService _cachingService;

        public ImageController(IWebHostEnvironment env, 
            ILocationDataService location, 
            ICachingService cachingService, IAuthService authService)
        {
            hostingEnv = env;
            _svcLocation = location;
            _cachingService = cachingService;
            _authService = authService;
        }

        [Route("Save/{id:int}/{contentType}/{contentName}")]
        [HttpPost]
        public async Task<IActionResult> Save(
            IList<IFormFile> uploadFiles,
            int id,
            string contentType,
            string contentName,
            [FromQuery] bool convertImages = true)
        {
            var result = await _svcLocation.GetByIdAsync(id);
            var locationRoute = result.Success ? result.Data.Route.TrimStart('/') : string.Empty;

            var targetDir = Path.Combine(
                hostingEnv.ContentRootPath, "wwwroot", "media", locationRoute, contentType, contentName);

            if (!Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            // Syncfusion uploader may batch; return the first (or build an array if you want)
            IFormFile file = uploadFiles.FirstOrDefault();
            if (file is null) return BadRequest(new { message = "No file." });

            var rawFileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
            var ext = Path.GetExtension(rawFileName)?.ToLowerInvariant();
            var isConvertible = ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp";

            string finalFileName;
            string webRelativeUrl;

            await using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                var bytes = ms.ToArray();

                if (convertImages && isConvertible)
                {
                    // your existing helper that strips EXIF, resizes, saves as .webp
                    var finalPath = await ProcessAndSaveImageAsync(bytes, rawFileName, targetDir);
                    finalFileName = Path.GetFileName(finalPath);
                }
                else
                {
                    // save as-is, but still produce the final name we actually wrote
                    CreateFile(file, targetDir, rawFileName);
                    finalFileName = Path.GetFileName(rawFileName);
                }
            }

            // Build a URL the browser can use
            webRelativeUrl = $"/media/{locationRoute}/{contentType}/{contentName}/{finalFileName}";

            // After you computed the final file name you actually wrote, e.g. "foo.webp"
            Response.Headers["name"] = finalFileName;

            // (Optional, for cross-origin scenarios)
            Response.Headers.Append("Access-Control-Expose-Headers", "name");

            // ✅ Return the final name/url so the RTE inserts the correct one
            // Works with Syncfusion RTE 'saveUrl' when InsertImageSettings.Path is used (see below).
            return Ok(new
            {
                name = finalFileName,        // Syncfusion reads 'name' when Path is set
                url = webRelativeUrl         // also include 'url' (useful if you set Path = "" and rely on url)
            });
        }


        private async Task<string> ProcessAndSaveImageAsync(byte[] fileBytes, string originalFileName, string targetDir)
        {
            var srcExt = Path.GetExtension(originalFileName) ?? string.Empty;
            var baseName = Path.GetFileNameWithoutExtension(originalFileName);

            // We always re-encode to WebP at the end (requirement #3),
            // but if the source was already .webp we still strip EXIF/resize and re-save as .webp.
            var finalFileName = $"{baseName}.webp";
            var finalPath = Path.Combine(targetDir, finalFileName);

            using var inStream = new MemoryStream(fileBytes);
            using var image = await Image.LoadAsync(inStream);

            // 1) Strip EXIF (including GPS if present)
            image.Metadata.ExifProfile = null; // removes EXIF block entirely

            // Optional (defensive): also clear XMP / IPTC containers if you want to be thorough
            image.Metadata.IccProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;

            // 2) Resize longest side to 1000px, keep aspect ratio
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(1000, 1000)
            }));

            // 3) Save to WebP
            var encoder = new WebpEncoder { Quality = 80 }; // tweak quality as desired

            await using var outFs = new FileStream(finalPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await image.SaveAsync(outFs, encoder);

            return finalPath;
        }



        private string CreateFile(IFormFile file, string targetLocation, string filename)
        {
            // Name which is used to save the image
            filename = Path.Combine(targetLocation, filename);

            if (!System.IO.File.Exists(filename))
            {
                // Upload a image, if the same file name does not exist in the directory
                using (FileStream fs = System.IO.File.Create(filename))
                {
                    file.CopyTo(fs);
                    fs.Flush();
                }
                Response.StatusCode = 200;
            }
            else
            {
                Response.StatusCode = 204;
            }

            _cachingService.ClearByEntityName(Defaults.GetFilesCacheKey);
            return filename;
        }


    }
}