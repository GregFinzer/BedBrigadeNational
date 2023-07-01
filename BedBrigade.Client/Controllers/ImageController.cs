using BedBrigade.Client.Services;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace ImageUpload.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly IWebHostEnvironment hostingEnv;
        private readonly AuthenticationStateProvider? _authState;
        private readonly ILocationDataService _svcLocation;
        private ClaimsPrincipal _identity;

        public ImageController(IWebHostEnvironment env, ILocationDataService location)
        {
            hostingEnv = env;
            _svcLocation = location;
        }
        
        [Route("Save/{id:int}/{page}")]
        [HttpPost]
        public async Task Save( IList<IFormFile> UploadFiles, int Id, string page)
        {
            
            string locationName = string.Empty;
            try
            {
                var result = await _svcLocation.GetAsync(Id);
                if(result.Success)
                {
                    locationName = result.Data.Route;
                }
                foreach (var file in UploadFiles)
                {
                    string targetLocation = hostingEnv.ContentRootPath + $"\\wwwroot\\media{locationName}";
                    string targetPath = $"\\pages\\{page}";
                    string filename = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');

                    FolderExist(targetLocation, targetPath);
                    filename = CreateFile(file, targetLocation, targetPath, filename);
                }
            }
            catch (Exception e)
            {
                Response.Clear();
                Response.ContentType = "application/json; charset=utf-8";
                Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = e.Message;
            }
        }

        private string CreateFile(IFormFile file, string targetLocation, string targetPath, string filename)
        {

            // Name which is used to save the image
            filename = targetLocation + targetPath + $@"\{filename}";

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

            return filename;
        }

        private static void FolderExist(string targetLocation, string targetPath)
        {
            // Create a new directory, if it does not exists
            if (!Directory.Exists(targetLocation))
            {
                Directory.CreateDirectory(targetLocation);

            }
            if (!Directory.Exists(targetLocation + targetPath))
            {
                Directory.CreateDirectory(targetLocation + targetPath);
            }
        }
    }
}