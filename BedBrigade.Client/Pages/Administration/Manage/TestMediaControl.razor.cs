using BedBrigade.Client.Components;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.Popups;

namespace BedBrigade.Client.Pages.Administration.Manage
{
    public partial class TestMediaControl : ComponentBase
    {
        private string AllowedExtensions = String.Empty;

        public string FolderPath { get; set; }

        SfDialog MediaDialog;
        FileManager FileManagerComponent;

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
            FolderPath = @"/national/pages/AboutUs/middleImageRotator/";
            await OpenDialog();

            if (FileManagerComponent != null)
            {
                FileManagerComponent.SetFileManagerPath("/national/pages/AboutUs/leftImageRotator");
            }
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
