using BedBrigade.Common.Logic;
using NUnit.Framework.Legacy;

namespace BedBrigade.Tests
{
    [TestFixture]
    public class ExtensionTests
    {
        [Test]
        public void FormatPhoneNumber_AllDigits()
        {
            string phone = "1234567890";
            string formattedPhone = phone.FormatPhoneNumber();
            ClassicAssert.AreEqual("(123) 456-7890", formattedPhone);
        }

        [Test]
        public void FormatPhoneNumber_WithPlusOne()
        {
            string phone = "+11234567890";
            string formattedPhone = phone.FormatPhoneNumber();
            ClassicAssert.AreEqual("(123) 456-7890", formattedPhone);
        }
    }
}
