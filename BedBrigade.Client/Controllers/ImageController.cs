using BedBrigade.Common.Constants;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

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
        public async Task Save(IList<IFormFile> UploadFiles, int Id, string contentType, string contentName)
        {
            if (!_authService.UserHasRole(RoleNames.CanManageMedia))
            {
                Response.StatusCode = 403; // Forbidden
                return;
            }

            string locationRoute = string.Empty;
            try
            {
                var result = await _svcLocation.GetByIdAsync(Id);
                if (result.Success)
                {
                    locationRoute = result.Data.Route.TrimStart('/');
                }
                foreach (var file in UploadFiles)
                {
                    string targetLocation = Path.Combine(hostingEnv.ContentRootPath,
                        "wwwroot",
                        "media",
                        locationRoute,
                        contentType,
                        contentName);

                    if (!Directory.Exists(targetLocation))
                    {
                        Directory.CreateDirectory(targetLocation);
                    }
                    string filename = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');

                    CreateFile(file, targetLocation, filename);
                }
            }
            catch (Exception e)
            {
                Response.Clear();
                Response.ContentType = "application/json; charset=utf-8";
                Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = e.Message;
            }
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