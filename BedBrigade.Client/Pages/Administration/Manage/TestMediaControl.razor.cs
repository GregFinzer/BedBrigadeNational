using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Popups;

namespace BedBrigade.Client.Pages.Administration.Manage
{
    public partial class TestMediaControl : ComponentBase
    {
        SfDialog MediaDialog;
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
            FolderPath = @"national/pages/AboutUs/middleImageRotator/";
            await OpenDialog();
        }


        private async Task ShowRight()
        {
            FolderPath = @"national/pages/AboutUs/rightImageRotator/";
            await OpenDialog();
        }
    }
}
