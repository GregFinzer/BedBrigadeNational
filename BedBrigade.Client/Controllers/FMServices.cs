using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Syncfusion.Blazor.FileManager;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;
using FileManagerDirectoryContent = Syncfusion.Blazor.FileManager.Base.FileManagerDirectoryContent;
using Microsoft.AspNetCore.Authorization;
using Syncfusion.Blazor.FileManager.Base;
using System.Diagnostics;

namespace BedBrigade.Client.Controllers
{
    public class FileManagerDirectoryContentExtend : FileManagerDirectoryContent
    {      

        public string? customvalue { get; set; }
        public string? SubFolder { get; set; }

        public Dictionary<string, object>? CustomData { get; set; }
    }

     //[Authorize(Roles ="National Admin, Location Admin, Location Author")]
    [Route("[controller]")]
    public class FileManagerController : Controller
    {
        private const string DoubleBackSlash = "\\";
        private const string Slash = "/";
        public PhysicalFileProvider? operation;
        public string? basePath;
        string root = "wwwroot\\Media"; // new
        string mainFolder = "media";
              
        [Obsolete]
        public FileManagerController(IHostingEnvironment hostingEnvironment)
        {                     
            this.basePath = hostingEnvironment.ContentRootPath;
            this.operation = new PhysicalFileProvider();
            this.operation.RootFolder(this.basePath + DoubleBackSlash + this.root); 
        }

        // Processing the File Manager operations
        [Route("FileOperations")]
        public object FileOperations([FromBody] FileManagerDirectoryContent args)
        {
           SetUserRoot(); // new

           switch (args.Action)
            {
                // Add your custom action here
                case "read":
                    // Path - Current path; ShowHiddenItems - Boolean value to show/hide hidden items
                    return operation.ToCamelCase(operation.GetFiles(args.Path, args.ShowHiddenItems));
                case "delete":
                    // Path - Current path where of the folder to be deleted; Names - Name of the files to be deleted
                    return operation.ToCamelCase(operation.Delete(args.Path, args.Names));
                case "copy":
                    //  Path - Path from where the file was copied; TargetPath - Path where the file/folder is to be copied; RenameFiles - Files with same name in the copied location that is confirmed for renaming; TargetData - Data of the copied file
                    return operation.ToCamelCase(operation.Copy(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData));
                case "move":
                    // Path - Path from where the file was cut; TargetPath - Path where the file/folder is to be moved; RenameFiles - Files with same name in the moved location that is confirmed for renaming; TargetData - Data of the moved file
                    return operation.ToCamelCase(operation.Move(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData));
                case "details":
                    // Path - Current path where details of file/folder is requested; Name - Names of the requested folders
                    return operation.ToCamelCase(operation.Details(args.Path, args.Names));
                case "create":
                    // Path - Current path where the folder is to be created; Name - Name of the new folder / block creation on top level
                    return operation.ToCamelCase(operation.Create(args.Path, args.Name));
                    //return Content("Prohibited");
                case "search":
                    // Path - Current path where the search is performed; SearchString - String typed in the searchbox; CaseSensitive - Boolean value which specifies whether the search must be casesensitive
                    return operation.ToCamelCase(operation.Search(args.Path, args.SearchString, args.ShowHiddenItems, args.CaseSensitive));
                case "rename":
                    // Path - Current path of the renamed file; Name - Old file name; NewName - New file name
                    return operation.ToCamelCase(operation.Rename(args.Path, args.Name, args.NewName));
            }
            return null;
        }
        [Route("Download")]
        public IActionResult Download(string downloadInput)
        {               
            Debug.WriteLine(downloadInput);

            string newJson = ModifyDownloadJson(downloadInput);
            FileManagerDirectoryContent? args = JsonConvert.DeserializeObject<FileManagerDirectoryContent>(newJson);            
            return operation.Download(args.Path, args.Names, args.Data);
           
        } // download


        [Route("Upload")]
        public async Task<IActionResult> UploadAsync(string path, IList<IFormFile> uploadFiles, string action)
        {
            string fullPath = this.root;
            //Invoking upload operation with the required paramaters
            // path - Current path where the file is to uploaded; uploadFiles - Files to be uploaded; action - name of the operation(upload)
            try
            {
                string newroot = HttpContext.Request.Headers["rootfolder"].ToString().Split(',')[0];
                operation.RootFolder(this.basePath + DoubleBackSlash + this.root + DoubleBackSlash + newroot);
                fullPath = fullPath + Path.AltDirectorySeparatorChar + newroot;
            }
            catch {}

           
            FileManagerResponse uploadResponse;
            uploadResponse = operation.Upload(path, uploadFiles, action, null);
            if (uploadResponse.Error != null)
            {
                Response.Clear();
                Response.ContentType = "application/json; charset=utf-8";
                Response.StatusCode = Convert.ToInt32(uploadResponse.Error.Code);
                Response.HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>().ReasonPhrase = uploadResponse.Error.Message;
            }
            
            return Content("");
        }

        [Route("GetImage")]
        public IActionResult GetImage(FileManagerDirectoryContentExtend args)
        {
            //Invoking GetImage operation with the required paramaters
            // path - Current path of the image file; Id - Image file id;
           var root = args.SubFolder;
           operation.RootFolder(this.basePath + DoubleBackSlash + this.root + DoubleBackSlash + root);
           return operation.GetImage(args.Path, args.Id, false, null, null);
        }
        public IActionResult Index()
        {

            return View();
        }

        private void SetUserRoot()
        {
            string newroot = String.Empty;
            try
            {
                var requestedroot = HttpContext.Request.Headers["rootfolder"];
                //Debug.WriteLine(requestedroot);
                newroot = requestedroot.ToString();                

                if (newroot != null && newroot.Length > 0)
                {
                    var newFullRoot = this.basePath + DoubleBackSlash + this.root + DoubleBackSlash + newroot;
                    //Debug.WriteLine(newFullRoot);
                    operation.RootFolder(newFullRoot); // new
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        } // set User Root

        private string ModifyDownloadJson(string argJson)
        {
            dynamic? jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(argJson);
            if (jsonObj != null)
            {
                string? path = jsonObj["path"].ToString();
                string filterPath = jsonObj["data"][0]["filterPath"].ToString();
                if (path == Slash) // restore missing path
                {
                    jsonObj["path"] = filterPath;            
                }
                else
                {                    
                    if(filterPath != path && filterPath.Contains(path)) // missing root path
                    {
                        jsonObj["path"] = filterPath;
                    }
                }

                argJson = jsonObj.ToString();
                // Debug.WriteLine(argJson);
            }           
            return argJson;
        } // modify download JSON

    } // class
} // namespace

