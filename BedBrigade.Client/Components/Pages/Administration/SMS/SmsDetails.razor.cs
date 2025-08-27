using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;

namespace BedBrigade.Client.Components.Pages.Administration.SMS;

public partial class SmsDetails : ComponentBase, IDisposable
{
    [Parameter]
    public int locationId { get; set; }

    [Parameter]
    public string phone { get; set; }

    [Inject]
    public ISmsQueueDataService SmsQueueDataService { get; set; }

    [Inject]
    public ISendSmsLogic SendSmsLogic { get; set; }

    [Inject]
    public NavigationManager NavigationManager { get; set; }
    [Inject]
    public SmsQueueBackgroundService SmsQueueBackgroundService { get; set; }
    [Inject] private IJSRuntime _js { get; set; }
    [Inject] private ISmsState _smsState { get; set; }
    [Inject] private IAuthService? _svcAuth { get; set; }
    [Inject] private ToastService _toastService { get; set; }

    protected List<SmsQueue>? smsMessages;
    protected string newMessage = string.Empty;
    protected string ContactName { get; set; } = string.Empty;
    protected string ContactInitials { get; set; } = string.Empty;
    protected string ContactType { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            Log.Information($"{_svcAuth.UserName} went to the SMS Details Page");
            await LoadMessages();
            _smsState.OnChange += OnSmsStateChange;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "SmsDetails OnInitializedAsync");
            _toastService.Error("Error", $"An error occurred while loading messages: {ex.Message}");
        }
    }

    private async Task OnSmsStateChange(SmsQueue smsQueue)
    {
        if (locationId == smsQueue.LocationId && phone == smsQueue.ToPhoneNumber)
        {
            await LoadMessages();
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        _smsState.OnChange -= OnSmsStateChange;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await ScrollToBottom();  // Scroll to the bottom after loading messages 
    }

    private async Task LoadMessages()
    {
        // Load messages for the specified location and phone
        var response = await SmsQueueDataService.GetMessagesForLocationAndToPhoneNumber(locationId, phone);
        if (response.Success && response.Data != null)
        {
            smsMessages = response.Data;

            // Default the contact name from the first message or just use the phone
            if (smsMessages.Any())
            {
                ContactName = smsMessages.First().ContactName ?? phone;
                ContactType = smsMessages.First().ContactType;

                // Generate initials (e.g. "JK" from "John Keating")
                if (!string.IsNullOrEmpty(ContactName))
                {
                    ContactInitials = string.Join("", ContactName
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s[0]))
                        .ToUpper();
                }
                else
                {
                    ContactInitials = "#";
                }
            }
        }
        else
        {
            Log.Error("Failed to load messages for location {LocationId} and phone {Phone}: {Message}",
                locationId, phone, response.Message);
            _toastService.Error("Error", $"An error occurred while loading messages: {response.Message}");
        }

        if (smsMessages != null && smsMessages.Any(o => !o.IsRead))
        {
            await SmsQueueDataService.MarkMessagesAsRead(locationId, phone);
        }
    }

    private async Task ScrollToBottom()
    {
        await _js.InvokeVoidAsync("BedBrigadeUtil.ScrollToBottom", "messagesContainer");
    }

    protected void GoBack()
    {
        // Navigate back to the SmsSummary page
        NavigationManager.NavigateTo("/administration/SMS/SmsSummary");
    }

    protected async Task SendMessage()
    {
        if (!string.IsNullOrWhiteSpace(newMessage))
        {
            // Send the message
            var response = await SendSmsLogic.SendTextMessage(locationId, phone, newMessage);
            if (response.Success)
            {
                SmsQueueBackgroundService.SendNow();
                newMessage = string.Empty;
                // Refresh the conversation
                await LoadMessages();
            }
            else
            {
                Log.Error("Failed to send message: {Message}", response.Message);
                _toastService.Error("Error", $"An error occurred while sending the message: {response.Message}");
            }
        }
    }
}
