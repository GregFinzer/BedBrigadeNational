namespace BedBrigade.Common.Enums;

public enum FileUse
{
    Unknown = 0,
    Logo = 1, // an image used as a logo
    Image = 2, // an image used as an image
    Download = 3, // a downloadable file (pdf,csv,etc)
    Text = 4, // a text file
    Html = 5, // Raw Html
    Icon = 6
}