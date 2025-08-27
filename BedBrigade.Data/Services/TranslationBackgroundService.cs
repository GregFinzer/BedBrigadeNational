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
        private bool _isStopping;

        public TranslationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<TranslationBackgroundService> logger)
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
            await Task.Delay(TimeSpan.FromSeconds(80), cancellationToken);

            while (!cancellationToken.IsCancellationRequested && !_isStopping)
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
                            await translationService.ProcessQueue(cancellationToken);
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

                await WaitLoop(cancellationToken);
            }
        }

        private async Task WaitLoop(CancellationToken cancellationToken)
        {
            for (int i = 0; i < 60; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                if (_isStopping)
                    return;
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }
}
