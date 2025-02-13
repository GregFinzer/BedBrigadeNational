using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

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
    [Inject] private IJSRuntime _js { get; set; }
    [Inject] private ISmsState _smsState { get; set; }


    protected List<SmsQueue>? smsMessages;
    protected string newMessage = string.Empty;
    protected string ContactName { get; set; } = string.Empty;
    protected string ContactInitials { get; set; } = string.Empty;
    protected string ContactType { get; set; } = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadMessages();
        _smsState.OnChange += OnSmsStateChange;
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
        if (response?.Data != null)
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
            if (response != null && response.Success)
            {
                newMessage = string.Empty;
                // Refresh the conversation
                await LoadMessages();
            }
            // Optionally handle an error response here
        }
    }
}
