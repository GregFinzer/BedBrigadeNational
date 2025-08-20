using KellermanSoftware.NetEncryptionLibrary;

namespace BedBrigade.Common.Logic;

public static class EncryptionLogic
{
    private static Encryption _encryption;

    public static Encryption Encryption
    {
        get
        {
            if (_encryption == null)
            {
                _encryption = LibraryFactory.CreateEncryption();
            }

            return _encryption;
        }
    }

    public static string EncryptString(string key, string plainText)
    {
        byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = Encryption.EncryptBytes(EncryptionProvider.FPEKELL1, key, plainTextBytes);
        return Convert.ToBase64String(encryptedBytes);
    }

    public static string DecryptString(string key, string cipherText)
    {
        byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
        byte[] decryptedBytes = Encryption.DecryptBytes(EncryptionProvider.FPEKELL1, key, cipherTextBytes);
        string plainText = System.Text.Encoding.UTF8.GetString(decryptedBytes);
        return plainText;
    }

    public static string GetOneTimePassword(string? email)
    {
        email = (email ?? string.Empty).Trim().ToLowerInvariant();
        const int validSeconds = 60 * 15; // 15 minutes
        string oneTimePasswordKey = email + LicenseLogic.SyncfusionLicenseKey;
        return Encryption.CreateTimedOneTimePassword(oneTimePasswordKey, validSeconds);
    }

    public static string GetEncryptedEncodedEmail(string? email)
    {
        email = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email))
        {
            return string.Empty;
        }
        string key = LicenseLogic.SyncfusionLicenseKey;
        string encrypted= EncryptString(key, email);
        return System.Web.HttpUtility.UrlPathEncode(encrypted);
    }

    public static string GetDecryptedEmail(string? encryptedEmail)
    {
        if (string.IsNullOrEmpty(encryptedEmail))
        {
            return string.Empty;
        }
        string key = LicenseLogic.SyncfusionLicenseKey;
        return DecryptString(key, encryptedEmail);
    }


}
