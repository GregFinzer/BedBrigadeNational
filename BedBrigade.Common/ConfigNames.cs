namespace BedBrigade.Common
{
    public static class ConfigNames
    {
        //Email Section
        public const string FromEmailAddress = "FromEmailAddress";
        public const string FromEmailDisplayName = "FromEmailDisplayName";
        public const string EmailBeginHour = "EmailBeginHour";
        public const string EmailEndHour = "EmailEndHour";
        public const string EmailBeginDayOfWeek = "EmailBeginDayOfWeek";
        public const string EmailEndDayOfWeek = "EmailEndDayOfWeek";
        public const string EmailMaxSendPerMinute = "EmailMaxSendPerMinute";
        public const string EmailMaxSendPerHour = "EmailMaxSendPerHour";
        public const string EmailMaxSendPerDay = "EmailMaxSendPerDay";
        public const string EmailLockWaitMinutes = "EmailLockWaitMinutes";
        public const string EmailKeepDays = "EmailKeepDays";
        public const string EmailMaxPerChunk = "EmailMaxPerChunk";
        public const string EmailUseFileMock = "EmailUseFileMock";

        //Other
        public const string TokenExpiration = "TokenExpiration";
        public const string AllowedFileExtensions = "AllowedFileExtensions";
        public const string AllowedVideoExtensions = "AllowedVideoExtensions";
        public const string MediaFolder = "MediaFolder";
        public const string MainMediaSubFolder = "MainMediaSubFolder";
        public const string MaxFileSize = "MaxFileSize";
        public const string MaxVideoSize = "MaxVideoSize";
        public const string IsCachingEnabled = "IsCachingEnabled";
        public const string BedBrigadeNearMeMaxMiles = "BedBrigadeNearMeMaxMiles";
        public const string ReCaptchaSecret = "ReCaptchaSecret";
        public const string ReCaptchaSiteKey = "ReCaptchaSiteKey";
    }
}
