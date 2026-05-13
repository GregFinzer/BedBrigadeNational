using BedBrigade.Common.Logic;

namespace BedBrigade.Tests
{
    [TestFixture]
    public class EncryptUtilTests
    {
        private const string EncryptionPrefix = "@KS@";
        private const string ShortPassword = "password";
        private const string LongPassword = "Th!sIsAVeryLongPassword1234567890!@#$%^&*()_+-=[]{}|;:,.<>?/Th!sIsAVeryLongPassword1234567890!@#$%^&*()_+-=[]{}|;:,.<>?/";
        private const string ApiKey = "sk_live_1234567890abcdefghijklmnopqrstuvwxyz_ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string PhoneNumber = "(614) 555-1212";
        private const string MailHost = "smtp.office365.com";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            Assume.That(Environment.GetEnvironmentVariable("GOLD"), Is.Not.Null.And.Not.Empty,
                "GOLD environment variable must be set for EncryptUtil tests.");
        }

        [Test]
        public void EncryptString_ShortPassword_ReturnsEncryptedValue()
        {
            AssertEncrypts(ShortPassword);
        }

        [Test]
        public void DecryptString_ShortPassword_ReturnsOriginalValue()
        {
            AssertDecrypts(ShortPassword);
        }

        [Test]
        public void EncryptString_LongPassword_ReturnsEncryptedValue()
        {
            AssertEncrypts(LongPassword);
        }

        [Test]
        public void DecryptString_LongPassword_ReturnsOriginalValue()
        {
            AssertDecrypts(LongPassword);
        }

        [Test]
        public void EncryptString_ApiKey_ReturnsEncryptedValue()
        {
            AssertEncrypts(ApiKey);
        }

        [Test]
        public void DecryptString_ApiKey_ReturnsOriginalValue()
        {
            AssertDecrypts(ApiKey);
        }

        [Test]
        public void EncryptString_PhoneNumber_ReturnsEncryptedValue()
        {
            AssertEncrypts(PhoneNumber);
        }

        [Test]
        public void DecryptString_PhoneNumber_ReturnsOriginalValue()
        {
            AssertDecrypts(PhoneNumber);
        }

        [Test]
        public void EncryptString_MailHost_ReturnsEncryptedValue()
        {
            AssertEncrypts(MailHost);
        }

        [Test]
        public void DecryptString_MailHost_ReturnsOriginalValue()
        {
            AssertDecrypts(MailHost);
        }

        private static void AssertEncrypts(string value)
        {
            string encryptedValue = EncryptUtil.EncryptString(value);

            Assert.Multiple(() =>
            {
                Assert.That(encryptedValue, Is.Not.EqualTo(value));
                Assert.That(encryptedValue, Does.StartWith(EncryptionPrefix));
                Assert.That(EncryptUtil.IsEncrypted(encryptedValue), Is.True);
            });
        }

        private static void AssertDecrypts(string value)
        {
            string encryptedValue = EncryptUtil.EncryptString(value);
            string decryptedValue = EncryptUtil.DecryptString(encryptedValue);

            Assert.That(decryptedValue, Is.EqualTo(value));
        }
    }
}
