using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;

namespace BedBrigade.Common.Logic;

public static class DriveRoutingLogic
{
    public static List<BedRequest> OrderByBestRoute(List<BedRequest> bedRequests,
        double? startLatitude,
        double? startLongitude)
    {
        var ordered = new List<BedRequest>();
        var remaining = new List<BedRequest>(bedRequests);
        var currentLatitude = startLatitude;
        var currentLongitude = startLongitude;

        while (remaining.Count > 0)
        {
            var nextRequest = remaining
                .Select(request => new
                {
                    Request = request,
                    Distance = GetDistanceFromPointToBedRequest(currentLatitude, currentLongitude, request)
                })
                .OrderBy(item => item.Distance)
                .ThenBy(item => item.Request.CreateDate)
                .First();

            nextRequest.Request.Distance = nextRequest.Distance;
            ordered.Add(nextRequest.Request);
            remaining.Remove(nextRequest.Request);

            currentLatitude = nextRequest.Request.Latitude.HasValue ? (double?)nextRequest.Request.Latitude.Value : null;
            currentLongitude = nextRequest.Request.Longitude.HasValue ? (double?)nextRequest.Request.Longitude.Value : null;
        }

        return ordered;
    }
    
    public static double CalculateDistanceInMiles(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 3956; // miles
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double d = R * c;
        return d;
    }
    
    public static double GetDistanceFromPointToBedRequest(double? startLatitude,
        double? startLongitude,
        BedRequest request)
    {
        if (startLatitude.HasValue && startLongitude.HasValue && request.Latitude.HasValue && request.Longitude.HasValue)
        {
            return CalculateDistanceInMiles(startLatitude.Value, startLongitude.Value, (double)request.Latitude.Value,
                (double)request.Longitude.Value);
        }

        return Defaults.DefaultDistance;
    }
    
    public static List<BedRequest> SortBedRequestClosestToAddress(List<BedRequest> bedRequests, int bedRequestId)
    {
        var targetBedRequest = bedRequests.FirstOrDefault(b => b.BedRequestId == bedRequestId);
        if (targetBedRequest == null)
        {
            return bedRequests;
        }

        targetBedRequest.Distance = -1;

        List<BedRequest> waitingOrScheduled = bedRequests
            .Where(b => b.BedRequestId != targetBedRequest.BedRequestId 
                        && (b.Status == BedRequestStatus.Waiting || b.Status == BedRequestStatus.Scheduled))
            .ToList();

        List<BedRequest> result = new List<BedRequest>();
        List<BedRequest> ordered = OrderByBestRoute(
            waitingOrScheduled,
            targetBedRequest.Latitude.HasValue ? (double?)targetBedRequest.Latitude.Value : null,
            targetBedRequest.Longitude.HasValue ? (double?)targetBedRequest.Longitude.Value : null);
        result.Add(targetBedRequest);
        result.AddRange(ordered);
        return result;
    }
}