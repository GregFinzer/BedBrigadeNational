﻿using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;

namespace BedBrigade.Data.Data.Seeding
{
    public static class SeedConfigLogic
    {
        public static List<Configuration> AllConfigurationForSeeding()
        {
            List<Configuration> result = new List<Configuration>()
            {
                new()
                {
                    ConfigurationKey = ConfigNames.TokenExpiration,
                    ConfigurationValue = "24",
                    Section = ConfigSection.System,
                    LocationId = Defaults.NationalLocationId
                },

                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = ConfigNames.AllowedFileExtensions,
                    ConfigurationValue = ".jpg, .png, .pdf, .webp, .jpeg, .gif, .svg, .ico",
                    Section = ConfigSection.Media,
                    LocationId = Defaults.NationalLocationId
                },

                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = ConfigNames.AllowedVideoExtensions,
                    ConfigurationValue = ".mp4, .webm, .avi, .av1, .mov",
                    Section = ConfigSection.Media,
                    LocationId = Defaults.NationalLocationId
                },

                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = ConfigNames.MediaFolder,
                    ConfigurationValue = "Media",
                    Section = ConfigSection.Media,
                    LocationId = Defaults.NationalLocationId
                },

                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = ConfigNames.MainMediaSubFolder,
                    ConfigurationValue = "national",
                    Section = ConfigSection.Media,
                    LocationId = Defaults.NationalLocationId
                },

                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = ConfigNames.MaxFileSize,
                    //100 MB
                    ConfigurationValue = "104857600",
                    Section = ConfigSection.Media,
                    LocationId = Defaults.NationalLocationId
                },

                new() // added by VS 2/19/2023
                {
                    ConfigurationKey = ConfigNames.MaxVideoSize,
                    //250 MB
                    ConfigurationValue = "262144000",
                    Section = ConfigSection.Media,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.EnableFolderOperations,
                    ConfigurationValue = "false",
                    Section = ConfigSection.Media,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.IsCachingEnabled,
                    ConfigurationValue = "true",
                    Section = ConfigSection.System,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.BedBrigadeNearMeMaxMiles,
                    ConfigurationValue = "30",
                    Section = ConfigSection.System,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.ReCaptchaSiteKey,
                    ConfigurationValue = "6LeDtS0qAAAAANLi2IY68WW555JXAWIelpoZQIWO",
                    Section = ConfigSection.System,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.ReCaptchaSecret,
                    ConfigurationValue = "6LeDtS0qAAAAANGrgPxMV2vTcgVG1e01KaRGjuqL",
                    Section = ConfigSection.System,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.FromEmailAddress,
                    ConfigurationValue = "devtest@bedbrigadecolumbus.org",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmailBeginHour,
                    ConfigurationValue = "0",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmailEndHour,
                    ConfigurationValue = "23",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmailBeginDayOfWeek,
                    ConfigurationValue = "0",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmailEndDayOfWeek,
                    ConfigurationValue = "6",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmailMaxSendPerMinute,
                    ConfigurationValue = "30",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmailMaxSendPerHour,
                    ConfigurationValue = "60",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmailMaxSendPerDay,
                    ConfigurationValue = "1440",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmailLockWaitMinutes,
                    ConfigurationValue = "10",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmailKeepDays,
                    ConfigurationValue = "30",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmailMaxPerChunk,
                    ConfigurationValue = "30",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmailUseFileMock,
                    ConfigurationValue = "true",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.FromEmailDisplayName,
                    ConfigurationValue = "Bed Brigade NoReply",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmailHost,
                    ConfigurationValue = "mail5019.site4now.net",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmailPort,
                    ConfigurationValue = "8889",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmailUserName,
                    ConfigurationValue = "devtest@bedbrigadecolumbus.org",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmailPassword,
                    ConfigurationValue = "AskGregForPassword",
                    Section = ConfigSection.Email,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.DisplayIdFields,
                    ConfigurationValue = "No",
                    Section = ConfigSection.System,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EmptyGridText,
                    ConfigurationValue = "No matching records found",
                    Section = ConfigSection.System,
                    LocationId = Defaults.NationalLocationId
                },

                new()
                {
                    ConfigurationKey = ConfigNames.EventCutOffTimeDays,
                    ConfigurationValue = "3",
                    Section = ConfigSection.System,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.TranslationApiKey,
                    ConfigurationValue = "",
                    Section = ConfigSection.System,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.PrimaryLanguage,
                    ConfigurationValue = "English;Spanish;Haitian Creole;Portuguese;Other",
                    Section = ConfigSection.System,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.SpeakEnglish,
                    ConfigurationValue = "Yes;No;A little",
                    Section = ConfigSection.System,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.GeoLocationUrl,
                    ConfigurationValue = "https://us1.locationiq.com/v1/search/structured",
                    Section = ConfigSection.GeoLocation,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.GeoLocationApiKey,
                    ConfigurationValue = string.Empty,
                    Section = ConfigSection.GeoLocation,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.GeoLocationMaxRequestsPerDay,
                    ConfigurationValue = "5000",
                    Section = ConfigSection.GeoLocation,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.GeoLocationMaxRequestsPerSecond,
                    ConfigurationValue = "2",
                    Section = ConfigSection.GeoLocation,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.GeoLocationLockWaitMinutes,
                    ConfigurationValue = "10",
                    Section = ConfigSection.GeoLocation,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.GeoLocationKeepDays,
                    ConfigurationValue = "30",
                    Section = ConfigSection.GeoLocation,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.SmsBeginHour,
                    ConfigurationValue = "8",
                    Section = ConfigSection.Sms,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.SmsEndHour,
                    ConfigurationValue = "21",
                    Section = ConfigSection.Sms,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.SmsBeginDayOfWeek,
                    ConfigurationValue = "0",
                    Section = ConfigSection.Sms,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.SmsEndDayOfWeek,
                    ConfigurationValue = "6",
                    Section = ConfigSection.Sms,
                    LocationId = Defaults.NationalLocationId
                },
                //Twillio can send a max of one SMS per second
                new()
                {
                    ConfigurationKey = ConfigNames.SmsMaxSendPerSecond,
                    ConfigurationValue = "1",
                    Section = ConfigSection.Sms,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.SmsLockWaitMinutes,
                    ConfigurationValue = "10",
                    Section = ConfigSection.Sms,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.SmsKeepDays,
                    ConfigurationValue = "30",
                    Section = ConfigSection.Sms,
                    LocationId = Defaults.NationalLocationId
                },
                //Twilio can send a max of one SMS per second. We are on a one-minute timer
                new()
                {
                    ConfigurationKey = ConfigNames.SmsMaxPerChunk,
                    ConfigurationValue = "60",
                    Section = ConfigSection.Sms,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.SmsUseFileMock,
                    ConfigurationValue = "true",
                    Section = ConfigSection.Sms,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.SmsAccountSid,
                    ConfigurationValue = "SmsAccountSid",
                    Section = ConfigSection.Sms,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.SmsAuthToken,
                    ConfigurationValue = "SmsAuthToken",
                    Section = ConfigSection.Sms,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.SmsPhone,
                    ConfigurationValue = "(999) 123-4567",
                    Section = ConfigSection.Sms,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.SmsMissedMessageMinutes,
                    ConfigurationValue = "30",
                    Section = ConfigSection.Sms,
                    LocationId = Defaults.NationalLocationId
                },
                //Payments
                new()
                {
                    ConfigurationKey = ConfigNames.StripePublishableKey,
                    ConfigurationValue = string.Empty,
                    Section = ConfigSection.Payments,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.StripeSecretKey,
                    ConfigurationValue = string.Empty,
                    Section = ConfigSection.Payments,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.SessionEncryptionKey,
                    ConfigurationValue = string.Empty,
                    Section = ConfigSection.Payments,
                    LocationId = Defaults.NationalLocationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.StripeWebhookSecret,
                    ConfigurationValue = string.Empty,
                    Section = ConfigSection.Payments,
                    LocationId = Defaults.NationalLocationId
                }
            };

            result.AddRange(LocationSpecificConfigurations(Defaults.GroveCityLocationId));
            result.AddRange(LocationSpecificConfigurations(Defaults.PolarisLocationId));
            return result;
        }

        public static List<Configuration> LocationSpecificConfigurations(int locationId)
        {
            return new List<Configuration>()
            {
                new ()
                {
                    ConfigurationKey = ConfigNames.SmsPhone,
                    ConfigurationValue = string.Empty,
                    Section = ConfigSection.Sms,
                    LocationId = locationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.StripeAccountId,
                    ConfigurationValue = string.Empty,
                    Section = ConfigSection.Payments,
                    LocationId = locationId,
                },
                new()
                {
                    ConfigurationKey = ConfigNames.StripeDonationAmounts,
                    ConfigurationValue = "25|50|100||200|500",
                    Section = ConfigSection.Payments,
                    LocationId = locationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.StripeSubscriptionAmounts,
                    ConfigurationValue = "25|50|100||200|500",
                    Section = ConfigSection.Payments,
                    LocationId = locationId
                },
                new()
                {
                    ConfigurationKey = ConfigNames.ContactUsEmails,
                    ConfigurationValue = "Location Admin",
                    Section = ConfigSection.Email,
                    LocationId = locationId
                }
            };
        }
    }
}
