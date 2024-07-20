using Microsoft.AspNetCore.Components;

namespace BedBrigade.Client.Components
{
    public partial class WhoWeAre
    {
        [Parameter] public string LocationTitle { get; set; } = "The Bed Brigade";
        [Parameter] public string LocationTagLine { get; set; } = "We Provide a Safe Place to Sleep for All People";
        [Parameter] public string ImagePath { get; set; } = string.Empty;

        protected List<string> FileNames { get; set; }
        protected string FileName { get; set; }

        protected override Task OnParametersSetAsync()
        {
            if(!ImagePathExists(ImagePath) || string.IsNullOrEmpty(ImagePath))
            {
                ImagePath = "National/pages/Error/leftImageRotator";
            }
            FileNames = GetImages(ImagePath);
            FileName = ComputeImageToDisplay(FileNames);
            return base.OnParametersSetAsync();
        }

        private bool ImagePathExists(string imagePath)
        {
            if (Directory.Exists($"wwwroot/media/{imagePath}"))
            {
                return true;
            }
            return false;
        }

        private string ComputeImageToDisplay(List<string> FileNames)
        {
            var unit = DateTime.Now.Hour * 2 + (DateTime.Now.Minute > 30 ? 1 : 0);
            var imageIndex = unit % FileNames.Count;
            return FileNames[imageIndex].Replace("wwwroot/", "");
        }

        private List<string> GetImages(string path)
        {
            var fileNames = Directory.GetFiles($"wwwroot/media/{path}/").ToList();
            return fileNames;
        }

    }

}