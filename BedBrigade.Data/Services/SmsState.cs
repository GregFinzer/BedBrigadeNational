using BedBrigade.Common.Models;

namespace BedBrigade.Data.Services;

public class SmsState : ISmsState, IDisposable
{
    public event Func<SmsQueue, Task> OnChange;
    private bool _isStopping;

    public async Task NotifyStateChangedAsync(SmsQueue smsQueue)
    {
        if (!_isStopping && OnChange != null)
        {
            // Invoke all subscribed handlers
            foreach (Func<SmsQueue, Task> handler in OnChange.GetInvocationList())
            {
                await handler(smsQueue);
            }
        }
    }

    public void StopService()
    {
        _isStopping = true;
        Dispose();
    }

    public void Dispose()
    {
        OnChange = null;
    }
}

