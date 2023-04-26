using BedBrigade.Client.Services;
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Newtonsoft.Json;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.RichTextEditor;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace BedBrigade.Client.Components
{
    public partial class EditContent
    {
        [Inject] public IContentService _svcContent { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }
        [Inject] private NavigationManager _nav { get; set; }
        [Inject] private ILocationService _svcLocation { get; set; }

        [Parameter] public string PageName { get; set; }
        [Parameter] public bool IsNewPage { get; set; }
        [Parameter] public string saveUrl { get; set; }

        //private string saveUrl { get; set; } = "api/image/save/1/Images";
        private string imagePath { get; set; } = "media/National/Pages/Images/";
        private string validationMessage { get; set; } = "My Message";
        private List<string> AllowedTypes = new()
        {
            ".jpg",
            ".png",
            ".gif"
        };
        private bool DialogVisible { get; set; } = false;
        private SfRichTextEditor RteObj { get; set; }
        private ClaimsPrincipal? Identity { get; set; }
        private string Body { get; set; }
        private Content Content { get; set; }
        private SfToast ToastObj { get; set; }
        private string? ToastTitle { get; set; } = string.Empty;
        private int ToastTimeout { get; set; } = 6000;
        private string ToastContent { get; set; } = string.Empty;
        private string ButtonCaption { get; set; } = "Save As ...";
        private string locationRoute { get; set; } = string.Empty;
        private string locationName { get; set; } = string.Empty;

        private int locationId { get; set; }

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
             new ToolbarItemModel() {Command = ToolbarCommand.LowerCase },
             new ToolbarItemModel() {Command = ToolbarCommand.UpperCase },
             new ToolbarItemModel() {Command = ToolbarCommand.SuperScript },
             new ToolbarItemModel() {Command = ToolbarCommand.FontName },
             new ToolbarItemModel() {Command = ToolbarCommand.FontColor },
             new ToolbarItemModel() {Command = ToolbarCommand.FontSize },
             new ToolbarItemModel() {Command = ToolbarCommand.Separator },
             new ToolbarItemModel() {Command = ToolbarCommand.BackgroundColor },
             new ToolbarItemModel() {Command = ToolbarCommand.Formats },
             new ToolbarItemModel() {Command = ToolbarCommand.ClearFormat },
             new ToolbarItemModel() {Command = ToolbarCommand.FullScreen },
             new ToolbarItemModel() { Command = ToolbarCommand.Separator },
             new ToolbarItemModel() { Command = ToolbarCommand.CreateLink },
             new ToolbarItemModel() { Command = ToolbarCommand.Image },
             new ToolbarItemModel() { Command = ToolbarCommand.CreateTable },
             new ToolbarItemModel() {Command = ToolbarCommand.Separator },
             new ToolbarItemModel() {Command = ToolbarCommand.Redo },
             new ToolbarItemModel() {Command = ToolbarCommand.Undo },
              new ToolbarItemModel() {Command = ToolbarCommand.Separator },
            new ToolbarItemModel() { Name = "Save",TooltipText = "Save File" }
        };

        protected override async Task OnInitializedAsync()
        {
            Identity = (await _authState.GetAuthenticationStateAsync()).User;
            string location = Identity.Claims.FirstOrDefault(c => c.Type == "LocationId").Value;
            int.TryParse(location, out int id);
            if (IsNewPage)
            {
                ToastTitle = $"Save Page as {PageName}";
            }
            else
            {
                ToastTitle = $"Edit Page {PageName}";
            }

            var result = await _svcContent.GetAsync(PageName);
            if (result.Success)
            {
                Body = result.Data.ContentHtml;
                Content = result.Data;
                Content.ContentId = 0;
                Content.UpdateDate = DateTime.Now;
                Content.CreateDate = DateTime.Now;
                Content.CreateUser = Content.UpdateUser = Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;
                Content.LocationId = id;
                Content.Name = string.Empty;
            }
            else
            {
                ToastContent = $"Unable to load page {PageName}!";
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, ShowCloseButton = true });
            }


        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            //if (!firstRender)
            //{
            //    locationId = int.Parse(Identity.Claims.FirstOrDefault(c => c.Type == "LocationId").Value ?? "0");
            //    var locResult = await _svcLocation.GetAsync(locationId);
            //    if (locResult.Success)
            //    {
            //        locationRoute = locResult.Data.Route;
            //        locationName = locResult.Data.Name;
            //    }

//                saveUrl = $"api/image/save/{locationId}/{Content.Name}";
//                imagePath = $"media/{locationRoute}/pages/{Content.Name}/";
//#if DEBUG   
//                Console.WriteLine($"saveUrl: {saveUrl} imagePath: {imagePath} ");
//#endif
            //}
        }

        private async Task HideToast()
        {
            await this.ToastObj.HideAsync();
            _nav.NavigateTo("/administration/dashboard");
        }

        private async Task ClickHandler()
        {
            Content.ContentHtml = await RteObj.GetXhtmlAsync();
            if (Content.ContentId != 0)
            {
                var result = await _svcContent.UpdateAsync(Content);
                if (result.Success)
                {
                    ToastContent = "Saved Successfully!";
                }
                else
                {
                    ToastContent = "Unable to save the content!";
                }

                await ToastObj.ShowAsync();
            }
            else
            {
                DialogVisible = true;
            }
        }


        private async Task DialogOnClickHandler()
        {
            var result = await _svcContent.CreateAsync(Content);
            if (result.Success)
            {
                DialogVisible = false;
                ToastContent = "Saved Successfully!";
            }
            else
            {
                ToastContent = "Unable to save the content!";
            }

            await ToastObj.ShowAsync();
        }
    }
}