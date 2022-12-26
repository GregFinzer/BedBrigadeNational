using Microsoft.Extensions.Logging;

namespace BedBrigade.Admin.Pages
{
    public static class HandGesture
    {
        public static bool ThumbUp(Axis axis, ILogger logger)
        {
            logger.LogInformation("Thumb up gesture. Axis: {Axis}", axis);

            // Code to make robot perform gesture

            return true;
        }
    }

    public enum Axis { Left, Right }
}