using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services
{
    public interface ISmsState
    {
        event Func<SmsQueue, Task> OnChange;
        Task NotifyStateChangedAsync(SmsQueue smsQueue);
        void StopService();
    }
}
