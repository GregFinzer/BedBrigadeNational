using KellermanSoftware.NetEncryptionLibrary;

namespace BedBrigade.Common.Logic;

public static class EncryptUtil
{
    private const string EncryptionPrefix = "@KS@";
    private static Encryption? _encryption;
    private static string? _encryptionKey;
    
    public static Encryption Encryption
    {
        get
        {
            if (_encryption == null)
                _encryption = LibraryFactory.Encryption;
            
            return _encryption;
        }
    }

    public static string EncryptionKey
    {
        get
        {
            if (_encryptionKey == null)
                _encryptionKey = LicenseLogic.KellermanLicenseKey;
            
            return _encryptionKey;
        }
    }
    
    public static bool IsEncrypted(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;
        
        return  value.StartsWith(EncryptionPrefix);  
    }

    public static string EncryptString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        
        if (IsEncrypted(value))
            return value;
        
        string encrypted = Encryption.EncryptString(EncryptionProvider.FPEKELL1, EncryptionKey, value);
        return EncryptionPrefix + encrypted;
    }

    public static string DecryptString(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        
        if (!IsEncrypted(value))
            return value;
        
        string decrypted = Encryption.DecryptString(EncryptionProvider.FPEKELL1, EncryptionKey, value.Substring(EncryptionPrefix.Length));
        return decrypted;
    }
}