namespace BedBrigade.Common.Models
{
    public class BlogData : Content
    {
        public string? LocationRoute { get; set; } // Location Route/Path
        public string? LocationName { get; set; } // Location Name
                                                  // 
        public string? BlogFolder { get; set; } // Blog Folder (image locations)                               
        public string? MainImageUrl { get; set; } // main image url      

        public string? MainImageThumbnail { get; set; } // reserved string with thumbnail

        public List<string>? OptImagesUrl { get; set; } = []; // all images

        public List<string>? FileUploaded { get; set; } = [];  // the list of files, uploaded duiring session

        public List<string>? FileDelete { get; set; } = []; // the list of files, selected to delete duiring session

        public string? CreatedDateMonth { get; set; }
        public string? CreatedDatePeriod { get; set; } // Date & Year
        public string? UpdatedDateMonth { get; set; }
        public string? UpdatedDatePeriod { get; set; } // Date & Year
        public bool IsNewItem { get; set; } = false;

    }
}
