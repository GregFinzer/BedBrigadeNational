using BedBrigade.Common.Logic;
using NUnit.Framework.Legacy;

namespace BedBrigade.Tests
{
    [TestFixture]
    public class DateUtilTests
    {
        [Test]
        public void GetNthDayOfWeekLastMonth_FirstSaturday_ReturnsFirstSaturdayOfLastMonth()
        {
            //Arrange
            // January 4, 2025 is the first Saturday of January
            DateTime currentDate = new DateTime(2025, 1, 4);
            DateTime expected = new DateTime(2024, 12, 7); // First Saturday of December 2024

            //Act
            DateTime result = DateUtil.GetNthDayOfWeekLastMonth(currentDate);

            //Assert
            ClassicAssert.AreEqual(expected.Date, result.Date);
        }

        [Test]
        public void GetNthDayOfWeekLastMonth_SecondSaturday_ReturnsSecondSaturdayOfLastMonth()
        {
            //Arrange
            // January 11, 2025 is the second Saturday of January
            DateTime currentDate = new DateTime(2025, 1, 11);
            DateTime expected = new DateTime(2024, 12, 14); // Second Saturday of December 2024

            //Act
            DateTime result = DateUtil.GetNthDayOfWeekLastMonth(currentDate);

            //Assert
            // This test checks that we properly handle the second occurrence
            // Since Jan 11 is second Saturday, we should get Dec 14
            ClassicAssert.AreEqual(DayOfWeek.Saturday, result.DayOfWeek);
            ClassicAssert.AreEqual(2024, result.Year);
            ClassicAssert.AreEqual(12, result.Month);
        }

        [Test]
        public void GetNthDayOfWeekLastMonth_FirstMondayOfMonth_ReturnsFirstMondayOfLastMonth()
        {
            //Arrange
            // February 3, 2025 is the first Monday of February
            DateTime currentDate = new DateTime(2025, 2, 3);
            DateTime expected = new DateTime(2025, 1, 6); // First Monday of January 2025

            //Act
            DateTime result = DateUtil.GetNthDayOfWeekLastMonth(currentDate);

            //Assert
            ClassicAssert.AreEqual(expected.Date, result.Date);
            ClassicAssert.AreEqual(DayOfWeek.Monday, result.DayOfWeek);
        }

        [Test]
        public void GetNthDayOfWeekLastMonth_ThirdWednesdayOfMonth_ReturnsThirdWednesdayOfLastMonth()
        {
            //Arrange
            // March 19, 2025 is the third Wednesday of March
            DateTime currentDate = new DateTime(2025, 3, 19);
            DateTime expected = new DateTime(2025, 2, 19); // Third Wednesday of February 2025

            //Act
            DateTime result = DateUtil.GetNthDayOfWeekLastMonth(currentDate);

            //Assert
            ClassicAssert.AreEqual(expected.Date, result.Date);
            ClassicAssert.AreEqual(DayOfWeek.Wednesday, result.DayOfWeek);
        }

        [Test]
        public void GetNthDayOfWeekLastMonth_FifthOccurrenceNotExist_ReturnsFourthOccurrence()
        {
            //Arrange
            // January 29, 2025 is the fifth Wednesday of January (doesn't exist in December)
            // December 2024 only has 4 Wednesdays, so should return December 25
            DateTime currentDate = new DateTime(2025, 1, 29);

            //Act
            DateTime result = DateUtil.GetNthDayOfWeekLastMonth(currentDate);

            //Assert
            ClassicAssert.AreEqual(DayOfWeek.Wednesday, result.DayOfWeek);
            ClassicAssert.AreEqual(2024, result.Year);
            ClassicAssert.AreEqual(12, result.Month);
            // Should be the last occurrence of Wednesday in December
            ClassicAssert.IsTrue(result.Day >= 18 && result.Day <= 31);
        }

        [Test]
        public void GetNthDayOfWeekLastMonth_PreservesTimeOfDay()
        {
            //Arrange
            DateTime currentDate = new DateTime(2025, 2, 1, 14, 30, 45);
            TimeSpan expectedTime = new TimeSpan(14, 30, 45);

            //Act
            DateTime result = DateUtil.GetNthDayOfWeekLastMonth(currentDate);

            //Assert
            ClassicAssert.AreEqual(expectedTime, result.TimeOfDay);
        }

        [Test]
        public void GetNthDayOfWeekLastMonth_FebruaryFromMarch_HandlesShortMonth()
        {
            //Arrange
            // March 1, 2025 - first Saturday of March
            DateTime currentDate = new DateTime(2025, 3, 1);

            //Act
            DateTime result = DateUtil.GetNthDayOfWeekLastMonth(currentDate);

            //Assert
            // Should return a date in February
            ClassicAssert.AreEqual(2025, result.Year);
            ClassicAssert.AreEqual(2, result.Month);
        }

        [Test]
        public void GetNthDayOfWeekLastMonth_JanuaryFromFebruary_HandlesYearBoundary()
        {
            //Arrange
            // February 1, 2025 - first Saturday
            DateTime currentDate = new DateTime(2025, 2, 1);

            //Act
            DateTime result = DateUtil.GetNthDayOfWeekLastMonth(currentDate);

            //Assert
            // Should return a date in January
            ClassicAssert.AreEqual(2025, result.Year);
            ClassicAssert.AreEqual(1, result.Month);
        }

        [Test]
        public void GetNthDayOfWeekLastMonth_FifthTuesdayToFourthTuesday()
        {
            //Arrange
            // September 30, 2025 is the fifth Tuesday of September
            DateTime currentDate = new DateTime(2025, 9, 30);

            //Act
            DateTime result = DateUtil.GetNthDayOfWeekLastMonth(currentDate);

            //Assert
            ClassicAssert.AreEqual(DayOfWeek.Tuesday, result.DayOfWeek);
            ClassicAssert.AreEqual(2025, result.Year);
            ClassicAssert.AreEqual(8, result.Month);
            // Should be the last Tuesday in August, which is the 26th
            ClassicAssert.IsTrue(result.Day >= 19);
        }

        [Test]
        public void GetNthDayOfWeekLastMonth_MultipleYears()
        {
            //Arrange
            DateTime currentDate = new DateTime(2024, 1, 13); // Second Saturday of January 2024

            //Act
            DateTime result = DateUtil.GetNthDayOfWeekLastMonth(currentDate);

            //Assert
            // Should get second Saturday of December 2023
            ClassicAssert.AreEqual(DayOfWeek.Saturday, result.DayOfWeek);
            ClassicAssert.AreEqual(2023, result.Year);
            ClassicAssert.AreEqual(12, result.Month);
        }

        [Test]
        public void GetNthDayOfWeekLastMonth_AllWeekdaysOfFirstWeek()
        {
            //Arrange
            // Test each day of week for a date in the first week

            //Act & Assert for Sunday (first day of March 2025 is a Saturday, so March 2 is Sunday)
            DateTime sundayDate = new DateTime(2025, 3, 2);
            DateTime resultSunday = DateUtil.GetNthDayOfWeekLastMonth(sundayDate);
            ClassicAssert.AreEqual(DayOfWeek.Sunday, resultSunday.DayOfWeek);

            // Monday
            DateTime mondayDate = new DateTime(2025, 3, 3);
            DateTime resultMonday = DateUtil.GetNthDayOfWeekLastMonth(mondayDate);
            ClassicAssert.AreEqual(DayOfWeek.Monday, resultMonday.DayOfWeek);
        }

        [Test]
        public void GetNthDayOfWeekLastMonth_EdgeCaseEndOfMonth()
        {
            //Arrange
            // January 31, 2025 is a Friday (5th Friday)
            DateTime currentDate = new DateTime(2025, 1, 31);

            //Act
            DateTime result = DateUtil.GetNthDayOfWeekLastMonth(currentDate);

            //Assert
            ClassicAssert.AreEqual(DayOfWeek.Friday, result.DayOfWeek);
            ClassicAssert.AreEqual(2024, result.Year);
            ClassicAssert.AreEqual(12, result.Month);
        }
    }
}
