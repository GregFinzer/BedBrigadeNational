using BedBrigade.Client.Components;
using BedBrigade.Common;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Popups;

namespace BedBrigade.Client.Pages.Administration.Manage
{
    public partial class TestMediaControl : ComponentBase
    {
        [Inject] private ICustomSessionService _sessionService { get; set; }
        private string AllowedExtensions = String.Empty;

        SfDialog MediaDialog;
        FileManager FileManagerComponent;
        public string FolderPath { get; set; }

        private async Task OpenDialog()
        {
            await this.MediaDialog.ShowAsync();
        }
        private async Task CloseDialog()
        {
            await this.MediaDialog.HideAsync();
        }
        private void onOpen(BeforeOpenEventArgs args)
        {
            // setting maximum height to the Dialog
            args.MaxHeight = "90%";
        }

        private async Task ShowLeft()
        {
            FolderPath = @"national/pages/AboutUs/leftImageRotator/";
            await OpenDialog();
        }


        private async Task ShowMiddle()
        {
            await OpenDialog();
        }


        private async Task ShowRight()
        {
            await OpenDialog();
        }
    }
}
