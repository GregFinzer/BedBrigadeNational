using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BedBrigade.Data.Services
{
    public class TranslationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TranslationBackgroundService> _logger;
        private bool _isProcessing = false;

        public TranslationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<TranslationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(80), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_isProcessing)
                {
                    _logger.LogInformation("Translation Background Service Executing");
                    try
                    {
                        _isProcessing = true;
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var translationService = scope.ServiceProvider.GetRequiredService<ITranslationProcessorDataService>();
                            await translationService.ProcessQueue();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while processing the translation queue.");
                    }
                    finally
                    {
                        _isProcessing = false;
                        _logger.LogInformation("Translation Background Service Complete");
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
