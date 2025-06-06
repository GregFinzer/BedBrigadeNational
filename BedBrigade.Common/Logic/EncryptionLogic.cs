using System;
using KellermanSoftware.NetEncryptionLibrary;

namespace BedBrigade.Common.Logic;

    public class EncryptionLogic
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
            return Encryption.EncryptString(EncryptionProvider.FPEKELL1,key,plainText);
        }

        public static string DecryptString(string key, string cipherText)
        {
            return Encryption.DecryptString(EncryptionProvider.FPEKELL1, key, cipherText);
        }

    }
