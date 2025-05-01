using BedBrigade.Client.Services;
using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.RichTextEditor;
using System.Security.Claims;
using BedBrigade.Client.Components;
using BedBrigade.Common.Enums;
using BedBrigade.Data.Services;
using Syncfusion.Blazor.Popups;
using Microsoft.AspNetCore.Mvc;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using Syncfusion.Blazor.Kanban.Internal;
using Microsoft.AspNetCore.Components.Forms;
using System.Linq;
using System.Net.Http.Headers;
using BedBrigade.Common.Constants;

namespace BedBrigade.Client.Components.Pages.Administration.Edit
{
    public partial class EditContent : ComponentBase
    {
        [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private IContentDataService _svcContent { get; set; }
        [Inject] private NavigationManager _navigationManager { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        [Inject] private ILoadImagesService _loadImagesService { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private ICachingService _svcCaching { get; set; }
        [Inject] private ILocationState _locationState { get; set; }
        [Inject] private ITranslationProcessorDataService _svcTranslationProcessorDataService { get; set; }
        [Inject] private HttpClient Http { get; set; }

        [Parameter] public string LocationId { get; set; }
        [Parameter] public string ContentName { get; set; }
        private SfRichTextEditor RteObj { get; set; }
        private string? WorkTitle { get; set; }
        private string? Body { get; set; }
        private Content? Content { get; set; }
        private Dictionary<string, string>? ImageButtonList { get; set; } = null;
        private ClaimsPrincipal? Identity { get; set; }
        private bool Refreshed { get; set; }
        SfDialog MediaDialog;
        public string FolderPath { get; set; }
        private string LocationName { get; set; } = "";
        private string LocationRoute { get; set; } = "";
        private string SaveUrl { get; set; }
        private string ImagePath { get; set; }
        private List<string> AllowedTypes = new()
        {
            ".jpg",
            ".png",
            ".gif",
            ".jpeg",
            ".webp"
        };
        private string _contentRootPath = string.Empty;
        private int _maxFileSize;

        private string _mediaFolder;
        private List<string> _allowedExtensions = new List<string>();
        private bool _enableFolderOperations = false;

        private List<ToolbarItemModel> Tools = new List<ToolbarItemModel>()
        {
             new ToolbarItemModel() {Command = ToolbarCommand.Bold },
             new ToolbarItemModel() {Command = ToolbarCommand.Italic},
             new ToolbarItemModel() {Command = ToolbarCommand.Underline },
             new ToolbarItemModel() {Command = ToolbarCommand.Alignments },
             new ToolbarItemModel() {Command = ToolbarCommand.Separator },
             new ToolbarItemModel() {Command = ToolbarCommand.OrderedList },
             new ToolbarItemModel() {Command = ToolbarCommand.UnorderedList },
             new ToolbarItemModel() {Command = ToolbarCommand.Separator },
             new ToolbarItemModel() {Command = ToolbarCommand.Indent },
             new ToolbarItemModel() {Command = ToolbarCommand.Outdent },
             new ToolbarItemModel() {Command = ToolbarCommand.Separator },
             new ToolbarItemModel() {Command = ToolbarCommand.ClearFormat },
             new ToolbarItemModel() {Command = ToolbarCommand.RemoveLink },
             new ToolbarItemModel() {Command = ToolbarCommand.SourceCode },
             new ToolbarItemModel() {Command = ToolbarCommand.FullScreen },
             new ToolbarItemModel() {Command = ToolbarCommand.FontName },
             new ToolbarItemModel() {Command = ToolbarCommand.FontColor },
             new ToolbarItemModel() {Command = ToolbarCommand.FontSize },
             new ToolbarItemModel() {Command = ToolbarCommand.Separator },
             new ToolbarItemModel() {Command = ToolbarCommand.BackgroundColor },
             new ToolbarItemModel() {Command = ToolbarCommand.Formats },
             new ToolbarItemModel() {Command = ToolbarCommand.ClearFormat },
             new ToolbarItemModel() { Command = ToolbarCommand.Separator },
             new ToolbarItemModel() { Command = ToolbarCommand.CreateLink },
             new ToolbarItemModel() { Command = ToolbarCommand.Image },
             new ToolbarItemModel() { Command = ToolbarCommand.CreateTable },
             new ToolbarItemModel() {Command = ToolbarCommand.Separator },
             new ToolbarItemModel() {Command = ToolbarCommand.Redo },
             new ToolbarItemModel() {Command = ToolbarCommand.Undo }

        };

        private string _subdirectory;

        protected override async Task OnInitializedAsync()
        {
            Refreshed = false;
            Identity = _svcAuth.CurrentUser;
            

            int locationId;

            if (!int.TryParse(LocationId, out locationId))
            {
                _toastService.Error("Error",
                    $"Could not parse location as integer: {LocationId}");
                return;
            }

            _maxFileSize = await _svcConfiguration.GetConfigValueAsIntAsync(ConfigSection.Media, "MaxVideoSize");
            _mediaFolder = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "MediaFolder");

            string allowedFileExtensions = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "AllowedFileExtensions");
            string allowedVideoExtensions = await _svcConfiguration.GetConfigValueAsync(ConfigSection.Media, "AllowedVideoExtensions");
            _allowedExtensions.AddRange(allowedFileExtensions.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            _allowedExtensions.AddRange(allowedVideoExtensions.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            _enableFolderOperations = await _svcConfiguration.GetConfigValueAsBoolAsync(ConfigSection.Media, "EnableFolderOperations");

            ServiceResponse<Content> contentResult = await _svcContent.GetAsync(ContentName, locationId);

            if (contentResult.Success && contentResult.Data != null)
            {
                Content = contentResult.Data;
                WorkTitle = $"Editing {Content.Title}";

                if (BlogTypes.ValidBlogTypes.Contains(Content.ContentType))
                {
                    _subdirectory = Content.ContentType.ToString();
                }
                else
                {
                    _subdirectory = "pages";
                }
                await SetLocationName(locationId);
                _contentRootPath = FileUtil.GetMediaDirectory(LocationRoute);

                Body = await ProcessHtml(Content.ContentHtml);
                
                Content.UpdateDate = DateTime.UtcNow;
                Content.UpdateUser =  Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;
                ImageButtonList = GetImageButtonList(Body);
            }
            else
            {
                _toastService.Error("Error",
                    $"Could not load Content for location {LocationId} with name of {ContentName}");
            }
        }

        private Dictionary<string, string> GetImageButtonList(string? html)
        {
            if (String.IsNullOrEmpty(html))
                return new Dictionary<string, string>();

            Dictionary<string, string> imageButtonList = new Dictionary<string, string>();
            var rotatorImages=  _loadImagesService.GetImgIdsWithRotator(html);

            foreach (var rotatorImage in rotatorImages)
            {
                string firstLetterCapitalized = rotatorImage.First().ToString().ToUpper() + rotatorImage.Substring(1);
                string imagePath = StringUtil.InsertSpaces(firstLetterCapitalized);
                
                imageButtonList.Add($"Upload and maintain images for {imagePath}", rotatorImage);
            }

            imageButtonList.Add("Upload and maintain all images", string.Empty);
            return imageButtonList;
        }

        private async Task SetLocationName(int locationId)
        {
            var locationResult = await _svcLocation.GetByIdAsync(locationId);
            if (locationResult.Success && locationResult.Data != null)
            {
                LocationName = locationResult.Data.Name;
                LocationRoute = locationResult.Data.Route.TrimStart('/');
                ImagePath = $"media/{LocationRoute}/{_subdirectory}/{ContentName}/"; // VS 8/25/2024
                SaveUrl =  $"api/image/save/{locationId}/{_subdirectory}/{ContentName}";
            }
        }

        private async Task<string?> ProcessHtml(string? html)
        {
            string path = $"{LocationRoute}/{_subdirectory}/{ContentName}"; // VS 8/25/2024
            html = html ?? string.Empty;
            _loadImagesService.EnsureDirectoriesExist(path, html);
            html = _loadImagesService.SetImgSourceForImageRotators(path, html);
            return html;
        }


        private async Task HandleSaveClick()
        {
            Content.ContentHtml = await RteObj.GetXhtmlAsync();

            //Update Content  Record
            var updateResult = await _svcContent.UpdateAsync(Content);
            if (updateResult.Success)
            {
                if (Content.ContentType == ContentType.Header || Content.ContentType == ContentType.Footer)
                {
                    await _locationState.NotifyStateChangedAsync();
                }

                await _svcTranslationProcessorDataService.QueueContentTranslation(updateResult.Data);

                _toastService.Success("Content Saved", 
                    $"Content saved for location {LocationRoute} with name of {ContentName}"); // VS 8/25/2024
                NavigateToManagePages();
            }
            else
            {
                _toastService.Error("Error",
                    $"Could not save Content for location {LocationRoute} with name of {ContentName}");
            }
            
        }

        private void NavigateToManagePages()
        {
            _navigationManager.NavigateTo($"/administration/manage/pages/{Content.ContentType.ToString()}");
        }

        private async Task HandleCancelClick()
        {
            NavigateToManagePages();
        }

        private async Task HandleImageButtonClick(string itemValue)
        {
            FolderPath = _contentRootPath + $"\\{_subdirectory}\\{ContentName}\\{itemValue}";
            FolderPath = FolderPath.TrimEnd('\\');
            await OpenDialog();
        }

        private async Task OpenDialog()
        {
            await this.MediaDialog.ShowAsync();
        }
        private async Task CloseDialog()
        {
            await this.MediaDialog.HideAsync();

            int locationId;

            if (!int.TryParse(LocationId, out locationId))
            {
                return;
            }
            _svcCaching.ClearAll();
            Body = await ProcessHtml(Body);
            await this.RteObj.RefreshUIAsync();
            StateHasChanged();
        }

        private void onOpen(BeforeOpenEventArgs args)
        {
            // setting maximum height to the Dialog
            args.MaxHeight = "90%";
        }

        private async Task OnInputFileChange(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                var ext = Path.GetExtension(file.Name).ToLowerInvariant();

                if (!AllowedTypes.Contains(ext))
                {
                    _toastService.Error("Invalid file type",
                        $"Only: {string.Join(", ", AllowedTypes)}");
                    return;
                }

                if (file.Size > _maxFileSize)
                {
                    _toastService.Error("File too large",
                        $"Max size is {_maxFileSize / (1024 * 1024)} MB");
                    return;
                }

                string path = ImagePath + file.Name;
                using (FileStream fs = System.IO.File.Create(path))
                    await file.OpenReadStream().CopyToAsync(fs);

                Content.MainImageFileName = file.Name;
                _toastService.Success("Upload succeeded", file.Name);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                // always catch *everything* so nothing bubbles out
                Console.Error.WriteLine($"Upload error: {ex}");
                _toastService.Error("Upload error", ex.Message);
            }
        }

    }
}
