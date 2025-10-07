namespace BedBrigade.Common.Constants
{
    public static class ConfigNames
    {
        //System
        public const string TokenExpiration = "TokenExpiration";
        public const string ReCaptchaSecret = "ReCaptchaSecret";
        public const string ReCaptchaSiteKey = "ReCaptchaSiteKey";
        public const string IsCachingEnabled = "IsCachingEnabled";
        public const string BedBrigadeNearMeMaxMiles = "BedBrigadeNearMeMaxMiles";
        public const string DisplayIdFields = "DisplayIdFields";
        public const string EmptyGridText = "EmptyGridText";
        public const string EventCutOffTimeDays = "EventCutOffTimeDays";
        public const string PrimaryLanguage = "PrimaryLanguage";
        public const string SpeakEnglish = "SpeakEnglish";
        public const string MonthsBetweenRequests = "MonthsBetweenRequests";

        //Translation
        public const string TranslationApiKey = "TranslationApiKey";
        public const string TranslationMaxRequestsPerMinute = "TranslationMaxRequestsPerMinute";
        public const string TranslationMaxRequestsPerDay = "TranslationMaxRequestsPerDay";
        public const string TranslationMaxPerChunk = "TranslationMaxPerChunk";
        public const string TranslationLockWaitMinutes = "TranslationLockWaitMinutes";
        public const string TranslationQueueKeepDays= "TranslationQueueKeepDays";

        //Media
        public const string AllowedFileExtensions = "AllowedFileExtensions";
        public const string AllowedImageExtensions = "AllowedImageExtensions";
        public const string MediaFolder = "MediaFolder";
        public const string MainMediaSubFolder = "MainMediaSubFolder";
        public const string MaxFileSize = "MaxFileSize";
        public const string EnableFolderOperations = "EnableFolderOperations";
        public const string ConvertableImageExtensions = "ConvertableImageExtensions";
        public const string ConvertableMaxWidth = "ConvertableMaxWidth";

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
        public const string EmailHost = "EmailHost";
        public const string EmailPort = "EmailPort";
        public const string EmailUserName = "EmailUserName";
        public const string EmailPassword = "EmailPassword";
        public const string ContactUsEmails = "ContactUsEmails";
        public const string BedRequestEmails = "BedRequestEmails";
        public const string EmailImapPort = "EmailImapPort";
        public const string EmailImapUseSsl = "EmailImapUseSsl";

        //GeoLocation
        public const string GeoLocationUrl = "GeoLocationUrl";
        public const string GeoLocationApiKey = "GeoLocationApiKey";
        public const string GeoLocationMaxRequestsPerDay = "GeoLocationMaxRequestsPerDay";
        public const string GeoLocationMaxRequestsPerSecond = "GeoLocationMaxRequestsPerSecond";
        public const string GeoLocationLockWaitMinutes = "GeoLocationLockWaitMinutes";
        public const string GeoLocationKeepDays = "GeoLocationKeepDays";

        //SMS Section
        public const string SmsBeginHour = "SmsBeginHour";
        public const string SmsEndHour = "SmsEndHour";
        public const string SmsBeginDayOfWeek = "SmsBeginDayOfWeek";
        public const string SmsEndDayOfWeek = "SmsEndDayOfWeek";
        public const string SmsMaxSendPerSecond = "SmsMaxSendPerSecond";
        public const string SmsLockWaitMinutes = "SmsLockWaitMinutes";
        public const string SmsKeepDays = "SmsKeepDays";
        public const string SmsMaxPerChunk = "SmsMaxPerChunk";
        public const string SmsUseFileMock = "SmsUseFileMock";
        public const string SmsPhone = "SmsPhone";
        public const string SmsAccountSid = "SmsAccountSid";
        public const string SmsAuthToken = "SmsAuthToken";
        public const string SmsMissedMessageMinutes = "SmsMissedMessageMinutes";

        //Payments
        public const string StripeLocationSecretKey = "StripeLocationSecretKey";
        public const string SessionLocationEncryptionKey = "SessionLocationEncryptionKey";
        public const string StripeLocationDonationAmounts = "StripeLocationDonationAmounts";
        public const string StripeLocationSubscriptionAmounts = "StripeLocationSubscriptionAmounts";
        public const string StripeLocationWebhookSecret = "StripeLocationWebhookSecret";

        //Custom Strings
        public const string BedRequestNote = "BedRequestNote";
    }
}
