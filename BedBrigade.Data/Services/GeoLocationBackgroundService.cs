using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BedBrigade.Data.Services
{
    public class GeoLocationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GeoLocationBackgroundService> _logger;
        private bool _isProcessing = false;
        private bool _isStopping;

        public GeoLocationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<GeoLocationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public void StopService()
        {
            _isStopping = true;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(90), cancellationToken);

            while (!cancellationToken.IsCancellationRequested && !_isStopping)
            {
                if (!_isProcessing)
                {
                    _logger.LogInformation("Geolocation Background Service Executing");
                    try
                    {
                        _isProcessing = true;
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var processorDataService = scope.ServiceProvider.GetRequiredService<IGeoLocationProcessorDataService>();
                            await processorDataService.ProcessQueue(cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while processing the Geolocation queue.");
                    }
                    finally
                    {
                        _isProcessing = false;
                        _logger.LogInformation("Geolocation Background Service Complete");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }
}
