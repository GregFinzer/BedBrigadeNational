using Microsoft.AspNetCore.Components;
using BedBrigade.Common.Models;
using System.Diagnostics;
using BedBrigade.Common.Logic;
using Microsoft.AspNetCore.Components.Forms;
using Syncfusion.Blazor.RichTextEditor;
using Microsoft.JSInterop;
using BedBrigade.Data.Services;
using BedBrigade.Client.Services;
using Microsoft.AspNetCore.Hosting;
using Syncfusion.Blazor.Inputs;
using static BedBrigade.Common.Logic.BlogHelper;
using BedBrigade.Common.Enums;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using Syncfusion.Blazor.DataVizCommon;
using System.Collections.Generic;
using BedBrigade.SpeakIt;

namespace BedBrigade.Client.Components
{
    public partial class BlogEdit: ComponentBase
    {
        [Inject] private IContentDataService? _svcContent { get; set; }
        [Inject] IWebHostEnvironment? WebhostEnvironment { get; set; }
        [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }
        [Inject] private ILanguageContainerService? _lc { get; set; }
        [Inject] private ITranslateLogic? _translateLogic { get; set; }
             
        
        [Parameter]
        public BlogData? BlogItem { get; set; }

        [Parameter]
        public EventCallback<BlogData> OnSave { get; set; }

        [Parameter]
        public EventCallback OnCancel { get; set; }

        public BlogData BlogEditItem = new BlogData();
        private const string PageEditMode = "Edit";
        public string PageMode = PageEditMode; // Another mode : view
        public string PageTitle = String.Empty;
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
        public string? SelectedMainImage { set; get; }
        private double BlogModuleFileSize = 0;
        private string? BlogModuleImagesExt { get; set; }
        private string BlogFolderServerPath = string.Empty;
        private string BlogFolderRootPath = string.Empty;


        protected override async Task OnInitializedAsync()
        {
           
            await LoadConfiguration();
           
        }//OnInit
              

        private async Task LoadConfiguration()
        {
            var MaxFileSize = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "MaxFileSize");
            BlogModuleFileSize = Convert.ToDouble(MaxFileSize);
            BlogModuleImagesExt = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "BlogModuleImages");

        }//LoadConfiguration


        protected override void OnParametersSet()
        {
            // Create a copy to avoid direct changes on the original object
            if (BlogItem != null)
            {               
                BlogEditItem = BlogHelper.CloneBlog(BlogItem);
            }

            string ObjectName = BlogEditItem.ContentType.ToString().Replace("ies", "y");
            PageTitle = ObjectName + " "  + $"{BlogEditItem.LocationName}";

            // Temp holder for some translation items
            var strImage1 = @_lc.Keys["Image"];
            var strNews1 = @_lc.Keys["News"];
            var strStory1 = @_lc.Keys["Story"];
            var strStories1 = @_lc.Keys["Stories"];

            if (BlogEditItem.ContentId > 0)
            {
                PageTitle = @_lc.Keys["Edit"] + " " + PageTitle + $" #{BlogEditItem.ContentId}";
            }
            else
            {
                PageTitle = @_lc.Keys["Add"] + " " + PageTitle; ;                
            }

            BlogFolderRootPath = BlogHelper.NormalizePath(BlogEditItem.BlogFolder, false);
            BlogFolderServerPath  = Path.Combine(WebRootPath,BlogFolderRootPath);
            // Audit Image Files
            BlogEditItem.OptImagesUrl = BlogHelper.AuditBlogFiles(BlogFolderServerPath, BlogEditItem.BlogFolder);

            if(BlogEditItem.MainImageUrl!=null && BlogEditItem.MainImageUrl.Length > 0)
            {
                SelectedMainImage = BlogEditItem.MainImageUrl;
            }


        }

        private void CheckMainImageToDelete()
        {
            isMainImageInDeleteList = false;

            if (BlogEditItem.FileDelete != null && BlogEditItem.FileDelete.Count > 0)
            {
                string selectedMainImageFileName = Path.GetFileName(SelectedMainImage);
                // Check if the main image name exists in the FileDelete list
                isMainImageInDeleteList = BlogEditItem.FileDelete
                .Any(url => Path.GetFileName(url).Equals(selectedMainImageFileName, StringComparison.OrdinalIgnoreCase));
            } // check main image in delete list

        }

        private async Task SaveChanges()
        {
            // save data
            if (BlogEditItem != null)
            {
                Debug.WriteLine($"Request to save blog item: {BlogEditItem.ContentId} - {BlogEditItem.Title}");
                if (SelectedMainImage != null && SelectedMainImage.Length>0) // Update Main Image
                {                                  
                    
                    if (isMainImageInDeleteList) // main image file should be delete
                    {
                        // The main image is in the delete list, handle it accordingly
                        //Debug.WriteLine("The selected main image is marked for deletion!");
                        BlogEditItem.MainImageUrl = String.Empty;
                        BlogEditItem.Name = "NA";
                    }
                    else
                    {
                        BlogEditItem.MainImageUrl = BlogHelper.NormalizePath(SelectedMainImage);
                        BlogEditItem.Name = Path.GetFileName(SelectedMainImage);
                    }
                }

                // Delete Files
                if(BlogEditItem.FileDelete != null && BlogEditItem.FileDelete.Count > 0)
                {
                    BlogHelper.DeleteBlogFiles(WebRootPath, BlogEditItem.FileDelete);
                    // Audit Image Files
                    BlogEditItem.OptImagesUrl = BlogHelper.AuditBlogFiles(BlogFolderServerPath, BlogEditItem.BlogFolder);
                    if (isMainImageInDeleteList && BlogEditItem.OptImagesUrl.Any())
                    {
                        string newMainImage = BlogEditItem.OptImagesUrl.First();
                        BlogEditItem.MainImageUrl = newMainImage;
                        BlogEditItem.Name = Path.GetFileName(newMainImage);
                    }
                }

                BlogEditItem.FileDelete.Clear();
                BlogEditItem.FileUploaded.Clear();

                var updateResult = await _svcContent.UpdateAsync(BlogEditItem);
                if (updateResult.Success)
                {
                    Debug.WriteLine($"Blog saved: {BlogEditItem.ContentId} - {BlogEditItem.Title}");
                    StateHasChanged();
                    await OnSave.InvokeAsync(BlogEditItem);
                }
                else
                {
                    Debug.WriteLine($"Blog save failed: {BlogEditItem.ContentId} - {BlogEditItem.Title}");
                }                 

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

        [JSInvokable]
        public async Task OnBrowserClose()
        {
            // Handle browser close event (e.g., save to draft, rollback, etc.)
            statusMessage = "Edit canceled: Browser closed!";
            await HandleEditCancellation("Browser Closed");
        }


        [JSInvokable]
        public async Task OnNavigationBack()
        {
            // Handle navigation back event
            statusMessage = "Edit canceled: Back button used!";
            await HandleEditCancellation("Back Button Used");
        }

        public async Task HandleEditCancellation(string reason)
        {
            // Your logic to handle unsaved changes
            Debug.WriteLine($"Edit canceled due to: {reason}");
            // Example: Save changes or rollback
            await Task.Delay(200); // Simulate async operation
        }

      

        // IMAGES PROCESSING

        private void SelectMainImage(string? fileName)
        {
            SelectedMainImage = fileName; // Update selected image
            BlogEditItem.MainImageUrl = BlogHelper.NormalizePath(SelectedMainImage);
            BlogEditItem.Name = Path.GetFileName(SelectedMainImage);
            CheckMainImageToDelete();
            Debug.WriteLine($"Select Image as New Main {SelectedMainImage}");      
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

        private void ToggleImageDeletion(Microsoft.AspNetCore.Components.ChangeEventArgs e, string imageUrl)
        {
            if ((bool)e.Value) // image selected for deletion
            {
                BlogEditItem.FileDelete.Add(imageUrl);                
            }
            else
            {
                BlogEditItem.FileDelete.Remove(imageUrl);
            }

            CheckMainImageToDelete();

        }//ToggleImageDeletion
        public bool GetImageDelStatus(string imageUrl)
        {
            if (BlogEditItem.FileDelete.Count > 0)
            {
                // Compare file name and check if it's in the deletion list
                string fileName = Path.GetFileName(imageUrl);
                return BlogEditItem.FileDelete.Any(file => Path.GetFileName(file) == fileName) ? true : false;
            }
            else
            {
                return false;
            }
        }//GetImageDelStatus

        private async Task OnUploadChange(UploadChangeEventArgs args)
        {

            var SavePath = BlogFolderServerPath;
            var FileBaseUrl = BlogHelper.NormalizePath(BlogEditItem.BlogFolder); // not full path - only URL
            
            // Define the maximum file size allowed (2 GB)
            long maxFileSize = (long)BlogModuleFileSize;


            if (BlogEditItem.OptImagesUrl == null)
            {
                BlogEditItem.OptImagesUrl = new List<string>();
            }


            if (args.Files != null && args.Files.Count > 0)
            {
                foreach (var file in args.Files)
                {
                    string NormilizedFileName = BlogHelper.SanitizeFileName(file.FileInfo.Name);
                    string newFileName = Path.Combine(SavePath, NormilizedFileName);
                    Debug.WriteLine($"Uploaded file: {newFileName}");

                    try
                    {
                        using var stream = file.File.OpenReadStream(maxAllowedSize: maxFileSize);
                        using var fileStream = new FileStream(newFileName, FileMode.Create, FileAccess.Write);
                        await stream.CopyToAsync(fileStream);
                        stream.Close();
                        fileStream.Close();

                        // Normalize path
                        string relativePath = FileBaseUrl += "/" + NormilizedFileName;
                        Debug.WriteLine($"Relative file: {relativePath}");

                        // Store temporarily only new files
                        if (!BlogEditItem.FileUploaded.Contains(relativePath) && !BlogEditItem.OptImagesUrl.Contains(relativePath))
                        {
                            BlogEditItem.FileUploaded.Add(relativePath); // current User session new uploaded files (without overriden)
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error saving file {file.FileInfo.Name}: {ex.Message}");
                    }
                }// File Loop
            }

            UpdateBlogFiles();
                        
        } // File Uploads

        private void UpdateBlogFiles()
        {
            var intAddCount = 0;
            // Update the Blog Item File list only once after all files are processed
            if (BlogEditItem.FileUploaded.Count > 0)
            {

                foreach (var newFile in BlogEditItem.FileUploaded)
                {
                    Debug.WriteLine($"The added uploaded file: {newFile}");
                    if (!BlogEditItem.OptImagesUrl.Contains(newFile))
                    {
                        intAddCount++;
                        BlogEditItem.OptImagesUrl.Add(newFile);
                    }
                }

                Debug.WriteLine("Final Image List:");
                foreach (var img in BlogEditItem.OptImagesUrl)
                {
                    Debug.WriteLine(img);
                }

                if (intAddCount > 0) // new files were added to Blog Item file list
                {
                    StateHasChanged(); // Refresh UI
                }

            }
        } //  UpdateBlogFiles()

    } // BlogEdit Class

} // namespace
