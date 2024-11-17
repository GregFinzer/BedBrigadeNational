using System.Net;
using System.Text.Json;
using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using RestSharp;
using Serilog;
using JsonException = Newtonsoft.Json.JsonException;

namespace BedBrigade.Data.Services
{
    public class GeoLocationProcessorDataService : IGeoLocationProcessorDataService
    {
        private readonly IConfigurationDataService _configurationDataService;
        private readonly IGeoLocationQueueDataService _geoLocationQueueDataService;
        private readonly IBedRequestDataService _bedRequestDataService;
        private int _maxRequestsPerDay;
        private int _maxRequestsPerSecond;
        private int _lockWaitMinutes;
        private string _apiKey;
        private string _geoLocationUrl;
        private int _maxKeepDays;

        public GeoLocationProcessorDataService(IConfigurationDataService configurationDataService,
            IGeoLocationQueueDataService geoLocationQueueDataService,
            IBedRequestDataService bedRequestDataService)
        {
            _configurationDataService = configurationDataService;
            _geoLocationQueueDataService = geoLocationQueueDataService;
            _bedRequestDataService = bedRequestDataService;
        }

        public async Task ProcessQueue(CancellationToken cancellationToken)
        {
            if (!await LoadConfiguration())
                return;

            if (cancellationToken.IsCancellationRequested)
                return;

            if (await MaxSent(cancellationToken))
                return;

            if (cancellationToken.IsCancellationRequested)
                return;

            if (await WaitForLock())
                return;

            await ProcessGeoLocationQueue(cancellationToken);
        }

        private async Task ProcessGeoLocationQueue(CancellationToken cancellationToken)
        {
            List<GeoLocationQueue> itemsToProcess = await _geoLocationQueueDataService.GetItemsToProcess();
            Log.Debug("GeoLocationProcessorDataService:  Items to process now: " + itemsToProcess.Count);

            if (itemsToProcess.Count == 0)
            {
                return;
            }

            Log.Debug("GeoLocationProcessorDataService: Locking Items");
            await _geoLocationQueueDataService.LockItemsToProcess(itemsToProcess);

            foreach (GeoLocationQueue item in itemsToProcess)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                if (await MaxSent(cancellationToken))
                    return;

                await ProcessGeoLocation(item, cancellationToken);
            }

            await _geoLocationQueueDataService.DeleteOldGeoLocationQueue(_maxKeepDays);
        }

        private async Task ProcessGeoLocation(GeoLocationQueue item, CancellationToken cancellationToken)
        {
            string url = _geoLocationUrl;
            url += $"?street={WebUtility.UrlEncode(item.Street)}";
            url += $"&city={WebUtility.UrlEncode(item.City)}";
            url += $"&state={WebUtility.UrlEncode(item.State)}";
            url += $"&postalcode={WebUtility.UrlEncode(item.PostalCode)}";
            url += "&country=US";
            url += "&format=json";
            url += $"&key={_apiKey}";

            try
            {
                RestClientOptions options = new RestClientOptions(url);
                RestClient client = new RestClient(options);
                RestRequest request = new RestRequest("");
                request.AddHeader("accept", "application/json");
                RestResponse response = await client.GetAsync(request, cancellationToken);

                if (!response.IsSuccessful)
                {
                    Log.Error("ProcessGeoLocation response error " + response.ErrorMessage);
                    item.FailureMessage = response.ErrorMessage;
                    item.Status = GeoLocationStatus.Failed.ToString();
                }
                else
                {
                    PopulateGeoLocation(response.Content, item);
                    item.Status = GeoLocationStatus.Processed.ToString();
                }
            }
            catch (Exception ex)
            {
                item.FailureMessage = ex.Message;
                item.Status = GeoLocationStatus.Failed.ToString();
            }

            item.LockDate = null;
            item.ProcessedDate = DateTime.UtcNow;
            await _geoLocationQueueDataService.UpdateAsync(item);

            await SaveGeoLocationToTable(item);
        }

        private async Task SaveGeoLocationToTable(GeoLocationQueue item)
        {
            if (item.TableName == TableNames.BedRequests.ToString())
            {
                BedRequest bedRequest = (await _bedRequestDataService.GetByIdAsync(item.TableId)).Data;

                if (item.Status == GeoLocationStatus.Processed.ToString())
                {
                    bedRequest.Latitude = item.Latitude;
                    bedRequest.Longitude = item.Longitude;
                }
                else
                {
                    bedRequest.Notes = ((bedRequest.Notes ?? "") +
                                       " Could not find Geolocation. Verify the address with the recipient.").Trim();
                }

                await _bedRequestDataService.UpdateAsync(bedRequest);
            }
        }


        public void PopulateGeoLocation(string jsonString, GeoLocationQueue geoLocation)
        {
            if (geoLocation == null) throw new ArgumentNullException(nameof(geoLocation));

            try
            {
                // Parse the JSON string into a dynamic object
                using var document = JsonDocument.Parse(jsonString);

                // Access the first JSON object in the array
                var rootElement = document.RootElement;
                if (rootElement.ValueKind == JsonValueKind.Array && rootElement.GetArrayLength() > 0)
                {
                    var firstElement = rootElement[0];

                    // Extract latitude and longitude
                    if (firstElement.TryGetProperty("lat", out JsonElement latElement) &&
                        firstElement.TryGetProperty("lon", out JsonElement lonElement))
                    {
                        // Populate the GeoLocation object
                        geoLocation.Latitude = decimal.Parse(latElement.GetString() ?? "0");
                        geoLocation.Longitude = decimal.Parse(lonElement.GetString() ?? "0");
                    }
                    else
                    {
                        throw new Exception("JSON does not contain required 'lat' or 'lon' properties.");
                    }
                }
                else
                {
                    throw new Exception("JSON does not contain a valid array.");
                }
            }
            catch (JsonException ex)
            {
                throw new Exception("Error parsing JSON.", ex);
            }
        }

        private async Task<bool> MaxSent(CancellationToken cancellationToken)
        {
            Log.Debug("GeoLocationProcessorDataService:  GetGeoLocationsToday");
            List<GeoLocationQueue> geoLocationsToday = await _geoLocationQueueDataService.GetGeoLocationsToday();

            if (geoLocationsToday.Count >= _maxRequestsPerDay)
            {
                Log.Information("Max requests per day reached");
                return true;
            }

            if (geoLocationsToday.Count(o => o.ProcessedDate >= DateTime.UtcNow.AddSeconds(-1)) >=
                _maxRequestsPerSecond)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                return true;
            }

            return false;
        }

        private async Task<bool> LoadConfiguration()
        {
            _geoLocationUrl =
                await _configurationDataService.GetConfigValueAsync(ConfigSection.GeoLocation,
                    ConfigNames.GeoLocationUrl);
            _apiKey = await _configurationDataService.GetConfigValueAsync(ConfigSection.GeoLocation,
                ConfigNames.GeoLocationApiKey);
            _maxRequestsPerDay = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.GeoLocation,
                ConfigNames.GeoLocationMaxRequestsPerDay);
            _maxRequestsPerSecond = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.GeoLocation,
                ConfigNames.GeoLocationMaxRequestsPerSecond);
            _lockWaitMinutes = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.GeoLocation,
                ConfigNames.GeoLocationLockWaitMinutes);
            _maxKeepDays = await _configurationDataService.GetConfigValueAsIntAsync(ConfigSection.GeoLocation,
                ConfigNames.GeoLocationKeepDays);

            if (string.IsNullOrEmpty(_apiKey))
            {
                Log.Error("GeoLocationProcessorDataService:  GeoLocationApiKey Key is missing.  See:  https://github.com/GregFinzer/BedBrigadeNational/blob/develop/Documentation/Getting%20Started.md");
                return false;
            }

            return true;
        }

        private async Task<bool> WaitForLock()
        {
            Log.Debug("GeoLocationProcessorDataService:  GetLockedGeoLocationQueue");
            List<GeoLocationQueue> lockedGeolocationQueue =
                await _geoLocationQueueDataService.GetLockedGeoLocationQueue();

            GeoLocationQueue firstLocked = lockedGeolocationQueue.FirstOrDefault();

            if (firstLocked != null)
            {
                //If there are Geo Locations locked less than the configured time, skip
                if (firstLocked.LockDate.HasValue
                    && firstLocked.LockDate.Value > DateTime.UtcNow.AddMinutes(_lockWaitMinutes * -1))
                {
                    Log.Debug(
                        $"GeoLocationProcessorDataService:  Queue is currently locked, waiting for {_lockWaitMinutes} minutes");
                    return true;
                }

                //Clear locked because something bad happened and we want to retry
                await _geoLocationQueueDataService.ClearGeoLocationQueueLock();
            }

            return false;
        }
    }
}
