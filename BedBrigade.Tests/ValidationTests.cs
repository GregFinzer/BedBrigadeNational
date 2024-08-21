using BedBrigade.Common.Logic;

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
            Assert.IsFalse(result.IsValid);
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
            Assert.IsTrue(result.IsValid);
        }


        [Test]
        public void GetCityForZipCode()
        {
            //Arrange
            string zipCode = "43215";

            //Act
            string result = Validation.GetCityForZipCode(zipCode);

            //Assert
            Assert.AreEqual("Columbus", result);
        }

        [Test]
        public void IsInvalidPhone()
        {
            //Arrange
            string phoneNumber = "1234567890";

            //Act
            bool result = Validation.IsValidPhoneNumber(phoneNumber);

            //Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void IsValidPhone()
        {
            //Arrange
            string phoneNumber = "6144567890";

            //Act
            bool result = Validation.IsValidPhoneNumber(phoneNumber);

            //Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void GetStateForZipCode()
        {
            //Arrange
            string zipCode = "43215";

            //Act
            string result = Validation.GetStateForZipCode(zipCode);

            //Assert
            Assert.AreEqual("OH", result);
        }

        [Test]
        public void IsValidZipCode()
        {
            //Arrange
            string zipCode = "43215";

            //Act
            bool result = Validation.IsValidZipCode(zipCode);

            //Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void IsInvalidZipCode()
        {
            //Arrange
            string zipCode = "99999";

            //Act
            bool result = Validation.IsValidZipCode(zipCode);

            //Assert
            Assert.IsFalse(result);
        }
    }
}
