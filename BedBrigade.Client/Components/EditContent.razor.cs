using BedBrigade.Client.Services;
using BedBrigade.Common;
using BedBrigade.Data.Models;
using static BedBrigade.Common.RoleNames;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.RichTextEditor;
using System.Drawing.Text;
using System.Security.Claims;

namespace BedBrigade.Client.Components
{
    public partial class EditContent
    {
        const int UseTemplates = 0;
        const string none = "none;";
        const string display = "display;";

        [Inject] public IContentService _svcContent { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }
        [Inject] private ILocationService _svcLocation { get; set; }

        [Parameter] public string PageName { get; set; }
        [Parameter] public bool IsNewPage { get; set; }
        [Parameter] public string saveUrl { get; set; }

        //private string saveUrl { get; set; } = "api/image/save/1/Images";
        private string imagePath { get; set; }
        private List<string> AllowedTypes = new()
        {
            ".jpg",
            ".png",
            ".gif"
        };
        private SfRichTextEditor RteObj { get; set; }

        private string workTitle;
        private string workType;

        public string newPageName { get; private set; }
        private ClaimsPrincipal? Identity { get; set; }
        private string Body { get; set; }
        private Content Content { get; set; }
        private Location Location { get; set; }
        private List<Location> Locations { get; set; }
        private SfToast ToastObj { get; set; }
        private string? ToastTitle { get; set; } = string.Empty;
        private int ToastTimeout { get; set; } = 2000;
        private string ToastContent { get; set; } = string.Empty;
        //private string ButtonCaption { get; set; } = "Save As ...";
        //private string locationRoute { get; set; } = string.Empty;
        //private string locationName { get; set; } = string.Empty;
        private int LocationId { get; set; }
        string[] pageParameters { get; set; }
        public string DisplayBody { get; private set; }
        public string DisplayHeader { get; private set; }
        public string DisplayHome { get; private set; }
        public string DisplayTitle { get; private set; }
        public string ShowMediaId { get; private set; }
        public bool showMedia { get; private set; } = false;
        public string SelectLocation { get; private set; } = "none;";

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
             new ToolbarItemModel() { Command = ToolbarCommand.Video },
             new ToolbarItemModel() { Command = ToolbarCommand.CreateTable },
             new ToolbarItemModel() {Command = ToolbarCommand.Separator },
             new ToolbarItemModel() {Command = ToolbarCommand.Redo },
             new ToolbarItemModel() {Command = ToolbarCommand.Undo }

        };

        protected override async Task OnInitializedAsync()
        {
            
            Identity = (await _authState.GetAuthenticationStateAsync()).User;
            pageParameters = saveUrl.Split('/');
            var pageNameParameters = pageParameters[4].Split("_");
            ServiceResponse<Content> contentResult;
            ServiceResponse<List<Location>> locationResult;
            locationResult = await _svcLocation.GetAllAsync();
            if(locationResult.Success) 
            {
                Locations = locationResult.Data;
            }
            if (IsNewPage)
            {
                workTitle = "Add Page";
                workType = $"Add {PageName}";
                ToastTitle = $"Save Page as {PageName}";
                //contentResult = await _svcContent.GetAsync(pageNameParameters[0], UseTemplates);
                string LocationId = Identity.Claims.FirstOrDefault(c => c.Type == "LocationId").Value;
                if(Identity.HasRole(CanCreateContentForLocations))
                {
                    SelectLocation = "display;";
                };
                Content = new()
                {
                    LocationId = Convert.ToInt32(LocationId)
                };
            }
            else
            {
                workTitle = "Editing";
                workType = $"Updating {PageName}";
                ToastTitle = $"Edit Page {PageName}";
                contentResult = await _svcContent.GetAsync(pageNameParameters[0], Convert.ToInt32(pageParameters[3]));

                var LocationService = await _svcLocation.GetAllAsync();

                var result = await _svcLocation.GetAsync(Convert.ToInt32(pageParameters[3]));
                if (result.Success)
                {
                    Location = result.Data;
                    imagePath = $"media{Location.Route}/Pages/Images/";
                }
                await CheckContent(contentResult);
            }
        }

        private async Task CheckContent(ServiceResponse<Content> contentResult)
        {
            if (contentResult.Success)
            {
                Body = contentResult.Data.ContentHtml;
                Content = contentResult.Data;
                Content.UpdateDate = DateTime.Now;
                Content.CreateDate = DateTime.Now;
                Content.CreateUser = Content.UpdateUser = Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;
                if (IsNewPage)
                {
                    //Content.LocationId = LocationId;
                    Content.Name = newPageName;
                    Content.ContentId = 0;
                    Content.Title = "N/A";
                }
                switch (Content.ContentType)
                {
                    case Common.Common.ContentType.Body:
                        DisplayBody = display;
                        DisplayHeader = none;
                        DisplayHome = none;
                        DisplayTitle = display;
                        ShowMediaId = none;
                        break;
                    case Common.Common.ContentType.Home:
                        DisplayBody = none;
                        DisplayHeader = none;
                        DisplayHome = display;
                        DisplayTitle = display;
                        break;
                    case Common.Common.ContentType.Header:
                        DisplayBody = none;
                        DisplayHeader = none;
                        DisplayHome = none;
                        DisplayTitle = none;
                        break;
                    case Common.Common.ContentType.Footer:
                        DisplayBody = none;
                        DisplayHeader = none;
                        DisplayHome = none;
                        DisplayTitle = none;
                        break;
                }

            }
            else
            {
                ToastContent = $"Unable to load page {PageName}!";
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, ShowCloseButton = true });
            }
            Console.WriteLine($"SaveUrl: {saveUrl} Path: {imagePath} AllowedTypes: {AllowedTypes}");
        }

        protected async Task OnValidSubmit()
        {
            Content.ContentHtml = await RteObj.GetXhtmlAsync();
            if (Content.ContentId != 0)
            {
                //Update Content  Record
                var updateResult = await _svcContent.UpdateAsync(Content);
                ToastTitle = "Update Page";
                if (updateResult.Success)
                {
                    ToastContent = "Page Updated Successfully!";
                }
                else
                {
                    ToastContent = "Unable to update location!";
                }
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
            }
            else
            {

                // new Page
                var result = await _svcContent.CreateAsync(Content);
                if (result.Success)
                {
                    Content Content = result.Data;
                }
                ToastTitle = "Create Page";
                if (Content.ContentId != 0)
                {
                    ToastContent = "Page Created Successfully!";
                }
                else
                {
                    ToastContent = "Unable to save Page!";
                }
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
            }
        }

        protected async Task DisplayMedia()
        {
            if(showMedia)
            {
                ShowMediaId = none;
                showMedia = false;
            }
            else
            {
                ShowMediaId = display;
                showMedia = true;
            }
        }

    }
}