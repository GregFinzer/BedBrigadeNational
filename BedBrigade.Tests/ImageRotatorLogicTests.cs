using BedBrigade.Common;

namespace BedBrigade.Tests
{
    [TestFixture]
    public class ImageRotatorLogicTests
    {
        [Test]
        public void When_There_Is_One_Image_Returns_First_Image()
        {
            //Arrange
            var imageRotatorLogic = new ImageRotatorLogic();
            var fileNames = new List<string> { "image1.jpg" };
            var expected = "image1.jpg";

            //Act
            var actual = imageRotatorLogic.ComputeImageToDisplay(fileNames);

            //Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_There_Are_Two_Images_On_Hour_Returns_First_Image()
        {
            //Arrange
            var imageRotatorLogic = new ImageRotatorLogic();
            imageRotatorLogic.OverrideDateTime = new DateTime(2023, 1, 1, 0, 0, 0);
            var fileNames = new List<string> { "image1.jpg", "image2.jpg" };
            var expected = "image1.jpg";

            //Act
            var actual = imageRotatorLogic.ComputeImageToDisplay(fileNames);

            //Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_There_Are_Two_Images_On_Half_Hour_Returns_Second_Image()
        {
            //Arrange
            var imageRotatorLogic = new ImageRotatorLogic();
            imageRotatorLogic.OverrideDateTime = new DateTime(2023, 1, 1, 0, 31, 0);
            var fileNames = new List<string> { "image1.jpg", "image2.jpg" };
            var expected = "image2.jpg";

            //Act
            var actual = imageRotatorLogic.ComputeImageToDisplay(fileNames);

            //Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_There_Are_Three_Images_On_Second_Hour_Returns_Third_Image()
        {
            //Arrange
            var imageRotatorLogic = new ImageRotatorLogic();
            imageRotatorLogic.OverrideDateTime = new DateTime(2023, 1, 1, 1, 1, 0);
            var fileNames = new List<string> { "image1.jpg", "image2.jpg", "image3.jpg" };
            var expected = "image3.jpg";

            //Act
            var actual = imageRotatorLogic.ComputeImageToDisplay(fileNames);

            //Assert
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void When_There_Are_Four_Images_On_Half_Hour_Returns_Fourth_Image()
        {
            //Arrange
            var imageRotatorLogic = new ImageRotatorLogic();
            imageRotatorLogic.OverrideDateTime = new DateTime(2023, 1, 1, 1, 31, 0);
            var fileNames = new List<string> { "image1.jpg", "image2.jpg", "image3.jpg", "image4.jpg" };
            var expected = "image4.jpg";

            //Act
            var actual = imageRotatorLogic.ComputeImageToDisplay(fileNames);

            //Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
