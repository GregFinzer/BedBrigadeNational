﻿namespace BedBrigade.Common
{
    internal static class LicenseLogic
    {
        public static string KellermanUserName => "Bed Brigade 10101";
        public static string KellermanLicenseKey
        {
            get
            {
                string? licenseKey = Environment.GetEnvironmentVariable("GOLD");
                if (string.IsNullOrEmpty(licenseKey))
                {
                    throw new Exception("GOLD environment variable not set");
                }

                return licenseKey;
            }
        }
    }
}
