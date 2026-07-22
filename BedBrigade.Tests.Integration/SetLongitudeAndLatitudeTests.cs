using AngleSharp.Dom;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data;
using KellermanSoftware.AddressParser;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using BedBrigade.Common.Enums;

namespace BedBrigade.Tests.Integration
{
    [TestFixture]
    public class SetLongitudeAndLatitudeTests
    {
        private const string ConnectionString =
            "";

        
        [Test, Ignore("This test should only run locally.")]
        public async Task SetLongitudeAndLatitude()
        {
            int count = 0;
            AddressParser parser = LibraryFactory.AddressParser;
            if (!TestHelper.IsWindows() || !TestHelper.ThisComputerHasExcelInstalled())
            {
                Assert.Ignore("This test should only run locally.");
            }

            using (var context = CreateDbContext(ConnectionString))
            {
                var dbSet = context.Set<BedRequest>();
                var waitingBedRequests = await context.BedRequests.Where(o => o.Status == BedRequestStatus.Waiting
                                                                              && (o.Longitude == null || o.Latitude == null))
                    .ToListAsync();

                foreach (var bedRequest in waitingBedRequests)
                {
                    if (!string.IsNullOrWhiteSpace(bedRequest.PostalCode) &&
                        parser.IsValidZipCode(bedRequest.PostalCode))
                    {
                        var info = parser.GetInfoForZipCode(bedRequest.PostalCode);
                        if (info != null && info.Latitude != null && info.Longitude != null)
                        {
                            bedRequest.Latitude = info.Latitude;
                            bedRequest.Longitude = info.Longitude;
                            dbSet.Update(bedRequest);
                            count++;
                        }
                    }
                }
                if (count > 0)
                {
                    await context.SaveChangesAsync();
                }
            }

            Console.WriteLine($"Updated {count} bed requests with longitude and latitude.");
        }

        public DataContext CreateDbContext(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            optionsBuilder.UseSqlServer(connectionString);
            return new DataContext(optionsBuilder.Options);
        }
    }
}
