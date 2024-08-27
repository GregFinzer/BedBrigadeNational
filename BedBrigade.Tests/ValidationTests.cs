using BedBrigade.Common.Logic;
using NUnit.Framework.Legacy;

namespace BedBrigade.Tests
{
    [TestFixture]
    public class ValidationTests
    {
        [Test]
        public void InvalidEmail()
        {
            //Arrange
            string email = "bademail@something.baddomain";

            //Act
            KellermanSoftware.NetEmailValidation.Result result = Validation.IsValidEmail(email);

            //Assert
            Console.WriteLine(result.UserMessage);
            ClassicAssert.IsFalse(result.IsValid);
        }

        [Test]
        public void ValidEmail()
        {
            //Arrange
            string email = "gfinzer@hotmail.com";

            //Act
            KellermanSoftware.NetEmailValidation.Result result = Validation.IsValidEmail(email);

            //Assert
            Console.WriteLine(result.UserMessage);
            ClassicAssert.IsTrue(result.IsValid);
        }


        [Test]
        public void GetCityForZipCode()
        {
            //Arrange
            string zipCode = "43215";

            //Act
            string result = Validation.GetCityForZipCode(zipCode);

            //Assert
            ClassicAssert.AreEqual("Columbus", result);
        }

        [Test]
        public void IsInvalidPhone()
        {
            //Arrange
            string phoneNumber = "1234567890";

            //Act
            bool result = Validation.IsValidPhoneNumber(phoneNumber);

            //Assert
            ClassicAssert.IsFalse(result);
        }

        [Test]
        public void IsValidPhone()
        {
            //Arrange
            string phoneNumber = "6144567890";

            //Act
            bool result = Validation.IsValidPhoneNumber(phoneNumber);

            //Assert
            ClassicAssert.IsTrue(result);
        }

        [Test]
        public void GetStateForZipCode()
        {
            //Arrange
            string zipCode = "43215";

            //Act
            string result = Validation.GetStateForZipCode(zipCode);

            //Assert
            ClassicAssert.AreEqual("OH", result);
        }

        [Test]
        public void IsValidZipCode()
        {
            //Arrange
            string zipCode = "43215";

            //Act
            bool result = Validation.IsValidZipCode(zipCode);

            //Assert
            ClassicAssert.IsTrue(result);
        }

        [Test]
        public void IsInvalidZipCode()
        {
            //Arrange
            string zipCode = "99999";

            //Act
            bool result = Validation.IsValidZipCode(zipCode);

            //Assert
            ClassicAssert.IsFalse(result);
        }
    }
}
