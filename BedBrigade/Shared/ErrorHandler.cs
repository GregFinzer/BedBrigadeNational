using Microsoft.Extensions.Logging;

namespace BedBrigade.Shared
{
    public class ErrorHandler
    {
        private readonly ILogger _log;

        public ErrorHandler(ILogger logger)
        {
           _log = logger;
        }
        /// <summary>
        /// Just handle the error message and return
        /// </summary>
        /// <param name="Message">Error message to be logged</param>
        public  async Task ErrorHandlerAsync(string name, string message)
        {
            _log.LogInformation(name, message);
        }

        public async Task ErrorHandlerAsync(Exception ex,string name, string message)
        {
            _log.LogInformation(ex, name, message);
        }
    }
}
