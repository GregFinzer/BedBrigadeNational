using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services;

public class SmsState : ISmsState
{
    public event Func<SmsQueue, Task> OnChange;
    public async Task NotifyStateChangedAsync(SmsQueue smsQueue)
    {
        if (OnChange != null)
        {
            // Invoke all subscribed handlers
            foreach (Func<SmsQueue, Task> handler in OnChange.GetInvocationList())
            {
                await handler(smsQueue);
            }
        }
    }
}

