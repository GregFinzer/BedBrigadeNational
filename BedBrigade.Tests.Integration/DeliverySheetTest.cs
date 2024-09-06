using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using NUnit.Framework.Legacy;

namespace BedBrigade.Tests
{
    [TestFixture]
    public class DeliverySheetTest
    {
        [Test]
        public void OutputDeliverySheet()
        {
            //Skip if not running locally
            if (!TestHelper.IsWindows() || !TestHelper.ThisComputerHasExcelInstalled())
            {
                Assert.Ignore("Ignore Excel Test When Running in Pipeline");
            }

            //Arrange
            // Calculate the first Saturday
            int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)DateTime.Today.DayOfWeek + 7) % 7;
            if (daysUntilSaturday == 0)
            {
                daysUntilSaturday = 7; // If today is Saturday, schedule for the next Saturday
            }

            DateTime nextSaturday = DateTime.Today.AddDays(daysUntilSaturday);
            DeliverySheetService deliverySheetService = new DeliverySheetService();

            Location location = new Location
            {
                Name = "Grove City",
            };

            List<BedRequest> bedRequests = new List<BedRequest>
            {
                new BedRequest
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Phone = "123-456-7890",
                    Street = "123 Elm St",
                    PostalCode = "43215",
                    CreateDate = DateTime.Today,
                    NumberOfBeds = 2,
                    AgesGender = "G/5 B/7",
                    TeamNumber = 1,
                    Notes = $"No Bed Bugs. Delivery scheduled for Saturday {nextSaturday.ToShortDateString()} at 10am",
                    DeliveryDate = nextSaturday
                },
                new BedRequest
                {
                    FirstName = "Mr.",
                    LastName = "Smith",
                    Phone = "614-555-7777",
                    Street = "123 Main St",
                    PostalCode = "43215",
                    CreateDate = DateTime.Today,
                    NumberOfBeds = 3,
                    AgesGender = "Self G/5 B/7",
                    TeamNumber = 1,
                    Notes = $"No Bed Bugs. Delivery scheduled for Saturday {nextSaturday.ToShortDateString()} at 10am",
                    DeliveryDate = nextSaturday
                },
                new BedRequest
                {
                    FirstName = "Francis",
                    LastName = "Seven",
                    Phone = "330-888-9999",
                    Street = "123 Lane Ave",
                    PostalCode = "43215",
                    CreateDate = DateTime.Today,
                    NumberOfBeds = 2,
                    AgesGender = "G/5 B/7",
                    TeamNumber = 2,
                    Notes = $"No Bed Bugs. Delivery scheduled for Saturday {nextSaturday.ToShortDateString()} at 10am",
                    DeliveryDate = nextSaturday
                },
                new BedRequest
                {
                    FirstName = "Logan",
                    LastName = "Five",
                    Phone = "614-444-4444",
                    Street = "123 Nationwide Blvd",
                    PostalCode = "43215",
                    CreateDate = DateTime.Today,
                    NumberOfBeds = 3,
                    AgesGender = "Self G/5 B/7",
                    TeamNumber = 2,
                    Notes = $"No Bed Bugs. Delivery scheduled for Saturday {nextSaturday.ToShortDateString()} at 10am",
                    DeliveryDate = nextSaturday
                }
            };
                
            //Act
            Stream stream = deliverySheetService.CreateDeliverySheet(location, bedRequests);
            using (FileStream fileStream = File.Create("Sample.xlsx"))
            {
                stream.CopyTo(fileStream);
            }
            

            //Assert
            ClassicAssert.IsTrue(File.Exists("Sample.xlsx"));
        }
    }
}
