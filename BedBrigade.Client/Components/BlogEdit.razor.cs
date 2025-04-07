using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using BedBrigade.SpeakIt;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.RichTextEditor;
using System.Diagnostics;

namespace BedBrigade.Client.Components
{
    public partial class BlogEdit : ComponentBase
    {
        [Inject] private IContentDataService? _svcContent { get; set; }
        [Inject] IWebHostEnvironment? WebhostEnvironment { get; set; }
        [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }
        [Inject] private ILanguageContainerService? _lc { get; set; }
        [Inject] private ITranslateLogic? _translateLogic { get; set; }
        [Inject] private ILanguageService? _svcLanguage { get; set; }

        [Parameter]
        public BlogData? BlogItem { get; set; }

        [Parameter]
        public EventCallback<BlogData> OnSave { get; set; }

        [Parameter]
        public EventCallback OnCancel { get; set; }

        private SfRichTextEditor? RteObj { get; set; }
        public BlogData BlogEditItem = new BlogData();
        private const string PageEditMode = "Edit";
        public string PageMode = PageEditMode; // Another mode : view
        public string PageTitle = String.Empty;
        private string? saveUrl { get; set; }
        public string? imagePath { get; private set; }
        private string LastSubmitResult = string.Empty;


        private List<ToolbarItemModel> EditorToolBar = new List<ToolbarItemModel>()
    {
        new ToolbarItemModel() { Command = ToolbarCommand.Bold },
        new ToolbarItemModel() { Command = ToolbarCommand.Italic },
        new ToolbarItemModel() { Command = ToolbarCommand.Underline },
        new ToolbarItemModel() { Command = ToolbarCommand.Separator },
        new ToolbarItemModel() { Command = ToolbarCommand.FontName },
        new ToolbarItemModel() { Command = ToolbarCommand.FontSize },
        new ToolbarItemModel() { Command = ToolbarCommand.FontColor },
        new ToolbarItemModel() { Command = ToolbarCommand.Separator },
        new ToolbarItemModel() { Command = ToolbarCommand.Alignments },
        new ToolbarItemModel() { Command = ToolbarCommand.OrderedList },
        new ToolbarItemModel() { Command = ToolbarCommand.UnorderedList },
        new ToolbarItemModel() { Command = ToolbarCommand.Outdent },
        new ToolbarItemModel() { Command = ToolbarCommand.Indent },
        new ToolbarItemModel() { Command = ToolbarCommand.Separator },
        new ToolbarItemModel() { Command = ToolbarCommand.CreateLink },
        new ToolbarItemModel() { Command = ToolbarCommand.Image },
        new ToolbarItemModel() { Command = ToolbarCommand.SourceCode },
        new ToolbarItemModel() { Command = ToolbarCommand.Undo },
        new ToolbarItemModel() { Command = ToolbarCommand.Redo }
        };

        private string statusMessage = string.Empty;

        // IMAGES VARIABLES
        private SfUploader? UploaderRef;
        private IBrowserFile? UploadedFile;
        private string? UploadFolder { set; get; } = string.Empty;
        private string WebRootPath => WebhostEnvironment.WebRootPath;
        private bool IsImagePreviewMode = false;
        private bool IsBlogPreviewMode = false;
        bool isMainImageInDeleteList = false;
        private string? selectedImageUrl { set; get; } = string.Empty;

        private double BlogModuleFileSize = 0;
        private string? BlogModuleImagesExt { get; set; }
        private string BlogFolderServerPath = string.Empty;
        private string BlogFolderRootPath = string.Empty;
        private List<string> AllowedTypes { get; set; } = new List<string>();

        protected override async Task OnInitializedAsync()
        {

            await LoadConfiguration();

        }//OnInit


        private async Task LoadConfiguration()
        {
            var MaxFileSize = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "MaxFileSize");
            BlogModuleFileSize = Convert.ToDouble(MaxFileSize);
            BlogModuleImagesExt = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "BlogModuleImages");
            //AllowedTypes = BlogModuleImagesExt.Split(',').ToList();
            AllowedTypes = BlogModuleImagesExt.Split(',').Select(ext => ext.Trim()).ToList();

        }//LoadConfiguration


        protected override async void OnParametersSet()
        {
            // Create a copy to avoid direct changes on the original object
            if (BlogItem != null)
            {
                BlogEditItem = BlogHelper.CloneBlog(BlogItem);
            }

            string ObjectName = BlogEditItem.ContentType.ToString().Replace("ies", "y");

            switch (ObjectName)
            {
                case "News":
                    ObjectName = _lc.Keys["News"];
                    break;
                case "Story":
                    ObjectName = _lc.Keys["Story"];
                    break;
                case "Stories": // placeholder
                    ObjectName = _lc.Keys["Stories"];
                    break;
            }

            string? currentLocation = BlogEditItem.LocationName;

            currentLocation = _translateLogic.GetTranslation(currentLocation);

            PageTitle = ObjectName + " " + _lc.Keys["For"] + " " + $"{currentLocation}";

            BlogFolderRootPath = BlogHelper.NormalizePath(BlogEditItem.BlogFolder, false);

            BlogFolderServerPath = Path.Combine(WebRootPath, BlogFolderRootPath);

            imagePath = BlogHelper.NormalizePath(BlogEditItem.BlogFolder) + "/";

            var imagePathApi = imagePath.Replace("media/", "");

            saveUrl = $"api/image/saveblog/{imagePathApi}";


            if (BlogEditItem.ContentId > 0 && !BlogEditItem.IsNewItem)
            {
                PageTitle = @_lc.Keys["Edit"] + " " + PageTitle + $" #{BlogEditItem.ContentId}";    
            }
            else
            {
                PageTitle = @_lc.Keys["Add"] + " " + PageTitle; ;
            }


        }



        private async Task SaveChanges()
        {
            // save data
            if (BlogEditItem != null)
            {
                Debug.WriteLine($"Request to save blog item: {BlogEditItem.ContentId} - {BlogEditItem.Title}");


                if (BlogEditItem.IsNewItem)
                {
                    // create new & after immediate Update
                    string oldBlogFolder = $"BlogItemNew_{BlogEditItem.ContentId}";
                    BlogEditItem.ContentId = 0;

                    var addResult = await _svcContent.CreateAsync(BlogEditItem);
                    if (addResult.Success && addResult != null)
                    {
                        BlogEditItem.ContentId = addResult.Data.ContentId;
                        BlogEditItem.IsNewItem = false;
                        string newBlogFolder = $"BlogItem_{BlogEditItem.ContentId}";
                        // replace Folder Name
                        string newFolderPath = BlogFolderServerPath.Replace(oldBlogFolder, newBlogFolder);
                        if (Directory.Exists(BlogFolderServerPath) && !Directory.Exists(newFolderPath))
                        {
                            Directory.Move(BlogFolderServerPath, newFolderPath);
                        }
                        // update Path in BlogEditItem
                        BlogEditItem.BlogFolder = BlogEditItem.BlogFolder.Replace(oldBlogFolder, newBlogFolder);
                        // update src in HTML
                        if (BlogEditItem.ContentHtml != null)
                        {
                            BlogEditItem.ContentHtml = BlogEditItem.ContentHtml.Replace(oldBlogFolder, newBlogFolder);
                        }
                    }
                } // create new

                if (BlogEditItem.ContentId > 0)
                {
                    var updateResult = await _svcContent.UpdateAsync(BlogEditItem);
                    if (updateResult.Success)
                    {
                        Debug.WriteLine($"Blog saved: {BlogEditItem.ContentId} - {BlogEditItem.Title}");
                        // Rename Folder temporary foldfer for new item
                        StateHasChanged();
                        await OnSave.InvokeAsync(BlogEditItem);
                    }
                    else
                    {
                        Debug.WriteLine($"Blog update failed: {BlogEditItem.ContentId} - {BlogEditItem.Title}");
                    }

                } // update





            } // save edited data            
        }

        private async Task CancelEdit()
        {
            // If Edit Canceled, uploaded files should be removed
            if (BlogEditItem.FileUploaded != null && BlogEditItem.FileUploaded.Count > 0)
            {
                BlogHelper.DeleteBlogFiles(WebRootPath, BlogEditItem.FileUploaded);
                BlogEditItem.FileUploaded = [];
            }

            if (BlogEditItem.IsNewItem)
            {
                if (Directory.Exists(BlogFolderServerPath))
                {
                    Directory.Delete(BlogFolderServerPath, true);
                }
            }

            await OnCancel.InvokeAsync();
        }

        private void ViewBlog()
        {
            PageMode = "View";
        }

        private void CloseView(BlogData? closedItem)
        {
            PageMode = PageEditMode;
            //StateHasChanged();
        }



        private void FormSubmitted(EditContext editContext)
        {
            bool formIsValid = editContext.Validate();
            LastSubmitResult = formIsValid ? "Success" : "Failure";
            Debug.WriteLine(LastSubmitResult);
            if (LastSubmitResult.Contains("Success"))
            {
                _ = SaveChanges();
            }
        }


        // IMAGES PROCESSING
        private void OnImageDelete(AfterImageDeleteEventArgs args)

        {
            var delFile = args.Src;
            if (delFile != null)
            {
                var imageFilePath = Path.Combine(WebhostEnvironment.WebRootPath, delFile);
                Debug.WriteLine("Delete File Full Name: " + imageFilePath);
                FileDelete(imageFilePath);
            }

        }//OnImageDelete

        private void FileDelete(string? FilePath)
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                return;
            }

            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }//FileDelete


        public void OnImageUploadFailedHandler(ImageFailedEventArgs args)
        {
            // Debug.WriteLine($"Image upload Failed: {args.File.Name}");
            // Debug.WriteLine(args.StatusText);

        }

        public void ValueChangeHandler(Syncfusion.Blazor.RichTextEditor.ChangeEventArgs args)
        {

            if (!string.IsNullOrEmpty(args.Value))
            {
                Debug.WriteLine("ValueChange triggered. Updating HTML...");

                // Ensure images are responsive
                var modifiedHtml = MakeImagesResponsive(args.Value);

                // Update the content only if modifications were made
                if (modifiedHtml != args.Value)
                {
                    Debug.WriteLine("Modified HTML applied.");
                    BlogEditItem.ContentHtml = modifiedHtml;
                }
            }

        } // RTE Value Changed


        public void OnImageUploadSuccessHandler(ImageSuccessEventArgs args)
        {
            // Reserved
        }//OnImageUploadSuccess    

        // Ensures responsive images in the content
        private string MakeImagesResponsive(string contentHtml)
        {
            contentHtml = contentHtml.Replace("height=\"auto\"", "");
            contentHtml = System.Text.RegularExpressions.Regex.Replace(
                contentHtml,
                @"max-width:\s*\d+px",
                "max-width: 100%"
            );

            // Ensures height:auto is in the style attribute
            contentHtml = System.Text.RegularExpressions.Regex.Replace(
                contentHtml,
                @"(<img\b[^>]*\bstyle=[""'])([^""']*)[""']",
                "$1$2; height: auto\""
            );

            return contentHtml;
        }

        private void ShowImagePreview(string imageUrl)
        {
            PageMode = "Image";
            selectedImageUrl = imageUrl;
        }
        private void ClosePreview()
        {
            PageMode = PageEditMode;
        }

        private async Task OnUploadMainImage(UploadChangeEventArgs args)
        {

            var SavePath = BlogFolderServerPath;
            // Check File Folder
            if (!string.IsNullOrEmpty(SavePath))
            {
                if (!Directory.Exists(SavePath))
                {
                    Directory.CreateDirectory(SavePath);
                }
            }

            var FileBaseUrl = BlogHelper.NormalizePath(BlogEditItem.BlogFolder); // not full path - only URL

            // Define the maximum file size allowed (2 GB)
            long maxFileSize = (long)BlogModuleFileSize;


            if (args.Files != null && args.Files.Count > 0)
            {
                foreach (var file in args.Files)
                {
                    string NormilizedFileName = BlogHelper.SanitizeFileName(file.FileInfo.Name);
                    string newFileName = Path.Combine(SavePath, NormilizedFileName);
                    Debug.WriteLine($"Uploaded Main Image file: {newFileName}");

                    try
                    {
                        using var stream = file.File.OpenReadStream(maxAllowedSize: maxFileSize);
                        using var fileStream = new FileStream(newFileName, FileMode.Create, FileAccess.Write);
                        await stream.CopyToAsync(fileStream);
                        stream.Close();
                        fileStream.Close();

                        // Normalize path
                        string relativePath = FileBaseUrl + "/" + NormilizedFileName;
                        Debug.WriteLine($"Relative Path to Main Image file: {relativePath}");
                        BlogEditItem.MainImageUrl = relativePath;
                        BlogEditItem.Name = NormilizedFileName;

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error saving Main Image file {file.FileInfo.Name}: {ex.Message}");
                    }
                }// File Loop
            }



        } // File Uploads



    } // BlogEdit Class

} // namespace
