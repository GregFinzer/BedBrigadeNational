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
        public void HelloWorldExcel()
        {
            //Arrange
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
                    Notes = "No Bed Bugs. Delivery scheduled for Saturday 8/31/2024 at 10am"
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
                    Notes = "No Bed Bugs. Delivery scheduled for Saturday 8/31/2024 at 10am"
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
                    Notes = "No Bed Bugs. Delivery scheduled for Saturday 8/31/2024 at 10am"
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
                    Notes = "No Bed Bugs. Delivery scheduled for Saturday 8/31/2024 at 10am"
                }
            };
                
            //Act
            deliverySheetService.CreateDeliverySheet(location, bedRequests);

            //Assert
            ClassicAssert.IsTrue(File.Exists("Sample.xlsx"));
        }
    }
}
