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
    }
}
