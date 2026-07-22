using BedBrigade.Common.Logic;
using KellermanSoftware.NetCachingLibrary.CacheProviders;

namespace BedBrigade.Tests
{
    [TestFixture]
    public class FactoryTests
    {
        [Test]
        public void CanCreateAddressParser()
        {
            var library = LibraryFactory.AddressParser;

            Assert.That(library, Is.Not.Null);
        }

        [Test]
        public void CanCreateQualityLogic()
        {
            var library = LibraryFactory.QualityLogic;

            Assert.That(library, Is.Not.Null);
        }

        [Test]
        public void CanCreateEmailValidation()
        {
            var library = LibraryFactory.EmailValidation;

            Assert.That(library, Is.Not.Null);
        }

        [Test]
        public void CanCreateEncryption()
        {
            var library = LibraryFactory.Encryption;

            Assert.That(library, Is.Not.Null);
        }

        [Test]
        public void CanCreateNameParser()
        {
            var library = LibraryFactory.NameParser;

            Assert.That(library, Is.Not.Null);
        }

        [Test]
        public void CanCreateStandardizationLogic()
        {
            var library = LibraryFactory.StandardizationLogic;

            Assert.That(library, Is.Not.Null);
        }

        [Test]
        public void CanCreateSmartConfig()
        {
            var library = LibraryFactory.CreateSmartConfig(
                new MemoryCacheProvider());

            Assert.That(library, Is.Not.Null);
        }

        [Test]
        public void AddressParserIsSingleton()
        {
            var first = LibraryFactory.AddressParser;
            var second = LibraryFactory.AddressParser;

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void QualityLogicIsSingleton()
        {
            var first = LibraryFactory.QualityLogic;
            var second = LibraryFactory.QualityLogic;

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void EmailValidationIsSingleton()
        {
            var first = LibraryFactory.EmailValidation;
            var second = LibraryFactory.EmailValidation;

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void EncryptionIsSingleton()
        {
            var first = LibraryFactory.Encryption;
            var second = LibraryFactory.Encryption;

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void NameParserIsSingleton()
        {
            var first = LibraryFactory.NameParser;
            var second = LibraryFactory.NameParser;

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void StandardizationLogicIsSingleton()
        {
            var first = LibraryFactory.StandardizationLogic;
            var second = LibraryFactory.StandardizationLogic;

            Assert.That(second, Is.SameAs(first));
        }
    }
}
