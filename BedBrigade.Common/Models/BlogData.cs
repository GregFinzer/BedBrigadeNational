using BedBrigade.Common.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Common.Models
{
    public class BlogData: Content
    {      
        public string? LocationRoute { get; set; } // Location Route/Path
        public string? LocationName { get; set; } // Location Name
                                                  // 
        public string? BlogFolder { get; set; } // Blog Folder (image locations)                               
        public string? MainImageUrl { get; set; } // main image url      

        public string? MainImageThumbnail { get; set; } // reserved string with thumbnail

        public List<string>? OptImagesUrl { get; set; } // all images, ecept current main

    }
}
