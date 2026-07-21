using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using BedBrigade.Data;
using BedBrigade.Data.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Diagnostics;

namespace BedBrigade.Tests.Integration;

[TestFixture]
public class BedRequestDataServiceSortingTests
{
    [Test]
    public void SortBedRequestClosestToAddress_ShouldOrderByBestRouteFromTarget()
    {
        BedRequestDataService service = CreateBedRequestDataService(new Location
        {
            LocationId = 100,
            BuildPostalCode = "43001",
            Latitude = 0,
            Longitude = 0
        });

        List<BedRequest> bedRequests =
        [
            BuildBedRequest(1, 100, BedRequestStatus.Waiting, "Alpha", 0, 0, new DateTime(2025, 1, 1)),
            BuildBedRequest(2, 100, BedRequestStatus.Waiting, "Alpha", 0, 0.9m, new DateTime(2025, 1, 2)),
            BuildBedRequest(3, 100, BedRequestStatus.Waiting, "Alpha", 0, 2.0m, new DateTime(2025, 1, 3)),
            BuildBedRequest(4, 100, BedRequestStatus.Waiting, "Alpha", 1.0m, 0, new DateTime(2025, 1, 4)),
        ];

        List<BedRequest> sorted = service.SortBedRequestClosestToAddress(bedRequests, 1);

        Assert.That(sorted.Select(x => x.BedRequestId).ToList(), Is.EqualTo(new List<int> { 1, 2, 3, 4 }));
    }

    [Test]
    public async Task GetScheduledBedRequestsForLocation_ShouldOrderEachTeamByBestRoute()
    {
        const int locationId = 200;
        Location location = new()
        {
            LocationId = locationId,
            BuildPostalCode = "43001",
            Latitude = 0,
            Longitude = 0
        };

        DbContextOptions<DataContext> options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        IDbContextFactory<DataContext> contextFactory = new TestDataContextFactory(options);
        await SeedScheduledBedRequestsAsync(contextFactory, locationId);
        BedRequestDataService service = CreateBedRequestDataService(location, contextFactory);

        ServiceResponse<List<BedRequest>> result = await service.GetScheduledBedRequestsForLocation(locationId);

        Assert.That(result.Success, Is.True, result.Message);
        Assert.That(result.Data?.Select(x => x.BedRequestId).ToList(), Is.EqualTo(new List<int> { 11, 12, 13, 21, 22 }));
    }

    [Test]
    [Category("Performance")]
    public void SortBedRequestClosestToAddress_With350WaitingRequests_CompletesWithinBudget()
    {
        const int locationId = 300;
        const int totalRequests = 351;
        const int targetBedRequestId = 1;
        const int maxMedianMilliseconds = 6000;
        const int iterations = 3;

        BedRequestDataService service = CreateBedRequestDataService(new Location
        {
            LocationId = locationId,
            BuildPostalCode = "43001",
            Latitude = 0,
            Longitude = 0
        });

        var elapsedMilliseconds = RunBenchmarkIterations(service, locationId, totalRequests, targetBedRequestId, iterations);
        long medianMilliseconds = elapsedMilliseconds.OrderBy(x => x).ElementAt(iterations / 2);

        TestContext.Progress.WriteLine(
            $"OrderByBestRoute benchmark ({totalRequests - 1} waiting requests). Iterations(ms): {string.Join(", ", elapsedMilliseconds)}; Median(ms): {medianMilliseconds}");

        Console.WriteLine($"OrderByBestRoute benchmark ({totalRequests - 1} waiting requests). Iterations(ms): {string.Join(", ", elapsedMilliseconds)}; Median(ms): {medianMilliseconds}");
        
        Assert.That(medianMilliseconds, Is.LessThanOrEqualTo(maxMedianMilliseconds),
            $"Median runtime {medianMilliseconds} ms exceeded budget {maxMedianMilliseconds} ms.");
    }

    private static List<long> RunBenchmarkIterations(BedRequestDataService service,
        int locationId,
        int totalRequests,
        int targetBedRequestId,
        int iterations)
    {
        var elapsedMilliseconds = new List<long>(iterations);

        for (int i = 0; i < iterations; i++)
        {
            List<BedRequest> requests = BuildLargeWaitingRequestSet(locationId, totalRequests);
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<BedRequest> sorted = service.SortBedRequestClosestToAddress(requests, targetBedRequestId);
            stopwatch.Stop();

            elapsedMilliseconds.Add(stopwatch.ElapsedMilliseconds);
            Assert.That(sorted.Count, Is.EqualTo(totalRequests));
            Assert.That(sorted.First().BedRequestId, Is.EqualTo(targetBedRequestId));
        }

        return elapsedMilliseconds;
    }

    private static List<BedRequest> BuildLargeWaitingRequestSet(int locationId, int totalRequests)
    {
        List<BedRequest> requests = new(totalRequests)
        {
            BuildBedRequest(1, locationId, BedRequestStatus.Waiting, "Alpha", 0, 0, new DateTime(2025, 1, 1))
        };

        for (int i = 2; i <= totalRequests; i++)
        {
            decimal latitude = (i % 25) * 0.04m;
            decimal longitude = (i / 25) * 0.04m;

            requests.Add(BuildBedRequest(i,
                locationId,
                BedRequestStatus.Waiting,
                "Alpha",
                latitude,
                longitude,
                new DateTime(2025, 1, 1).AddMinutes(i)));
        }

        return requests;
    }

    private static async Task SeedScheduledBedRequestsAsync(IDbContextFactory<DataContext> contextFactory, int locationId)
    {
        using DataContext context = contextFactory.CreateDbContext();
        context.BedRequests.AddRange(
            BuildBedRequest(11, locationId, BedRequestStatus.Scheduled, "Alpha", 0, 0.9m, new DateTime(2025, 1, 1)),
            BuildBedRequest(12, locationId, BedRequestStatus.Scheduled, "Alpha", 0, 2.0m, new DateTime(2025, 1, 2)),
            BuildBedRequest(13, locationId, BedRequestStatus.Scheduled, "Alpha", 1.0m, 0, new DateTime(2025, 1, 3)),
            BuildBedRequest(21, locationId, BedRequestStatus.Scheduled, "Beta", 0, 0.5m, new DateTime(2025, 1, 4)),
            BuildBedRequest(22, locationId, BedRequestStatus.Scheduled, "Beta", 1.0m, 1.5m, new DateTime(2025, 1, 5)));
        await context.SaveChangesAsync();
    }

    private static BedRequest BuildBedRequest(int bedRequestId,
        int locationId,
        BedRequestStatus status,
        string team,
        decimal latitude,
        decimal longitude,
        DateTime createDate)
    {
        return new BedRequest
        {
            BedRequestId = bedRequestId,
            LocationId = locationId,
            Status = status,
            Team = team,
            Latitude = latitude,
            Longitude = longitude,
            PostalCode = "43001",
            FirstName = "Test",
            LastName = "User",
            NumberOfBeds = 1,
            PrimaryLanguage = "English",
            CreateDate = createDate
        };
    }

    private static BedRequestDataService CreateBedRequestDataService(Location location, IDbContextFactory<DataContext>? contextFactory = null)
    {
        DbContextOptions<DataContext> options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        contextFactory ??= new TestDataContextFactory(options);

        ICachingService cachingService = new CachingService { IsCachingEnabled = false };
        Mock<IAuthService> authService = new();
        Mock<ICommonService> commonService = new();
        Mock<ILocationDataService> locationDataService = new();
        Mock<IGeoLocationQueueDataService> geoLocationQueueDataService = new();
        Mock<ITimezoneDataService> timezoneDataService = new();
        Mock<IConfigurationDataService> configurationDataService = new();
        Mock<IScheduleDataService> scheduleDataService = new();

        locationDataService
            .Setup(x => x.GetByIdAsync(It.IsAny<object>()))
            .ReturnsAsync(new ServiceResponse<Location>("Location found", true, location));

        return new BedRequestDataService(
            contextFactory,
            cachingService,
            authService.Object,
            commonService.Object,
            locationDataService.Object,
            geoLocationQueueDataService.Object,
            timezoneDataService.Object,
            configurationDataService.Object,
            scheduleDataService.Object);
    }
}
