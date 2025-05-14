namespace BedBrigade.Common.Constants
{
    public static class Defaults
    {
        public const int NationalLocationId = 1;
        public const int GroveCityLocationId = 2;
        public const int RockCityPolarisLocationId = 3;
        public const string ErrorImagePath = "media/national/NoImageFound.jpg";
        public const string AuthToken = "AuthToken";
        public const string MediaDirectory = "MediaDirectory";
        public const string NationalRoute = "/national";
        public const string DefaultPageTemplate = "ThreeRotatorPageTemplate";
        public const string DefaultUserNameAndEmail = "Anonymous";
        public const string GetFilesCacheKey = "Directory.GetFiles";
        public const int MetroAreaNoneId = 1;
        public const string PagesDirectory = "pages";
        public const string GroveCityTrimmedRoute = "grove-city";
        public const string DefaultLanguage = "en-US";
        public const int BulkLowPriority = 1;
        public const int BulkMediumPriority = 2;
        public const int BulkHighPriority = 3;
        public const string CountryCode = "US";
        public const string DefaultTimeZoneId = "Eastern Standard Time";

        //The reason why I have chosen 12 is because each row is four bootstrap columns 
        public const int MaxTopBlogItems = 12;
        public const int TruncationLength = 188;
    }
}
