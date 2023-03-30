using Microsoft.AspNetCore.Components;

namespace BedBrigade.Client.Components
{
    public partial class WhoWeAre
    {
        [Parameter] public string LocationTitle { get; set; } = "The Bed Brigade";
        [Parameter] public string LocationTagLine { get; set; } = "We Provide a Safe Place to Sleep for All People";
        [Parameter] public string ImagePath { get; set; } = string.Empty;
        protected override Task OnParametersSetAsync()
        {
            if(!ImagePathExists(ImagePath))
            {
                ImagePath = "National/pages/Error/";
            }

            return base.OnParametersSetAsync();
        }

        private bool ImagePathExists(string imagePath)
        {
            if (File.Exists($"wwwroot/media/{imagePath}"))
            {
                return true;

            }
            return false;
        }
    }

}