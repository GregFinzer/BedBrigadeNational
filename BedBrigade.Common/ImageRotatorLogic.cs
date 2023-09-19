namespace BedBrigade.Common
{
    public class ImageRotatorLogic
    {
        public DateTime? OverrideDateTime { get; set; }

        public string ComputeImageToDisplay(List<string> fileNames)
        {
            if (fileNames.Count > 0)
            {
                var currentDateTime = OverrideDateTime ?? DateTime.UtcNow;
                var unit = currentDateTime.Hour * 2 + (currentDateTime.Minute > 30 ? 1 : 0);
                var imageIndex = unit % fileNames.Count;
                return fileNames[imageIndex].Replace("wwwroot/", "");
            }
            return Constants.ErrorImagePath;
        }
    }
}
