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

namespace BedBrigade.Client.Components.Pages.Administration.Edit
{
    public partial class EditContent : ComponentBase
    {
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private IContentDataService _svcContent { get; set; }
        [Inject] private NavigationManager _navigationManager { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        [Inject] private ILoadImagesService _loadImagesService { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private ICachingService _svcCaching { get; set; }
        [Inject] private ILocationState _locationState { get; set; }
        [Inject] private ITranslationProcessorDataService _svcTranslationProcessorDataService { get; set; }
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
        private string saveUrl { get; set; }
        private string imagePath { get; set; }
        private List<string> AllowedTypes = new()
        {
            ".jpg",
            ".png",
            ".gif"
        };
        private string contentRootPath = string.Empty;

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


        protected override async Task OnInitializedAsync()
        {
            Refreshed = false;
            Identity = _svcAuth.CurrentUser;
            WorkTitle = $"Editing {ContentName}";

            int locationId;

            if (!int.TryParse(LocationId, out locationId))
            {
                _toastService.Error("Error",
                    $"Could not parse location as integer: {LocationId}");
                return;
            }

            await SetLocationName(locationId);
            contentRootPath = FileUtil.GetMediaDirectory(LocationRoute);

            ServiceResponse<Content> contentResult = await _svcContent.GetAsync(ContentName, locationId);

            if (contentResult.Success && contentResult.Data != null)
            {
                Body = await ProcessHtml(contentResult.Data.ContentHtml, locationId);
                Content = contentResult.Data;
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
                LocationRoute = locationResult.Data.Route;
                imagePath = $"media/{LocationRoute}/pages/{ContentName}/"; // VS 8/25/2024
                saveUrl = $"api/image/save/{locationId}/pages/{ContentName}";
            }
        }

        private async Task<string?> ProcessHtml(string? html, int locationId)
        {
            string path = $"{LocationRoute}/pages/{ContentName}"; // VS 8/25/2024
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
                _navigationManager.NavigateTo("/administration/manage/pages");
            }
            else
            {
                _toastService.Error("Error",
                    $"Could not save Content for location {LocationRoute} with name of {ContentName}");
            }
            
        }

        private async Task HandleCancelClick()
        {
            _navigationManager.NavigateTo("/administration/manage/pages");
        }

        private async Task HandleImageButtonClick(string itemValue)
        {
            //FolderPath = $"{LocationRoute}/pages/{ContentName}/{itemValue}"; // old
            FolderPath = contentRootPath + $"/pages/{ContentName}/{itemValue}"; // VS 9/3/2024
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
            Body = await ProcessHtml(Body, locationId);
            await this.RteObj.RefreshUIAsync();
            StateHasChanged();
        }

        private void onOpen(BeforeOpenEventArgs args)
        {
            // setting maximum height to the Dialog
            args.MaxHeight = "90%";
        }
    }
}
