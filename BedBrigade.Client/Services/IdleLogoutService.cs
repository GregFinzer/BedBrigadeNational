using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BedBrigade.Client.Services;

public sealed class IdleLogoutService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    public IdleLogoutService(IJSRuntime js) => _js = js;

    public async Task StartAsync(TimeSpan timeout, string logoutUrl = "/logout")
    {
        try
        {
            // Import the ES module on the client
            _module ??= await _js.InvokeAsync<IJSObjectReference>("import", "/scripts/IdleLogout.js");
            await _module.InvokeVoidAsync("startIdleTimer", (int)timeout.TotalMilliseconds, logoutUrl);
        }
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            // Ignore the exception when the component is disposed before the JS call completes
        }
        catch (Microsoft.JSInterop.JSDisconnectedException)
        {
            // Ignore if the JS runtime is disconnected (e.g., during dispose)
        }
    }

    public async Task StopAsync()
    {
        try
        {
            if (_module is not null)
                await _module.InvokeVoidAsync("stopIdleTimer");
        }
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            // Ignore the exception when the component is disposed before the JS call completes
        }
        catch (Microsoft.JSInterop.JSDisconnectedException)
        {
            // Ignore if the JS runtime is disconnected (e.g., during dispose)
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null)
                await _module.DisposeAsync();
        }
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            // Ignore the exception when the component is disposed before the JS call completes
        }
        catch (Microsoft.JSInterop.JSDisconnectedException)
        {
            // Ignore the exception when the JS runtime is disconnected (e.g., during hot reload)
        }
    }
}
