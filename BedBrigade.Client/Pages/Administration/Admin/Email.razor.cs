using BedBrigade.Common;
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Syncfusion.Blazor.DropDowns;

namespace BedBrigade.Client.Pages.Administration.Admin
{
    public partial class Email : ComponentBase
    {
        [Inject] private IUserDataService _svcUserDataService { get; set; }
        [Inject] private ILocationDataService _svcLocationDataService { get; set; }
        [Inject] private IScheduleDataService _svcScheduleDataService { get; set; }
        public BulkEmailModel Model { get; set; } = new();


        protected override async Task OnInitializedAsync()
        {
            Model.Locations = (await _svcLocationDataService.GetAllAsync()).Data;
            var user = (await _svcUserDataService.GetCurrentLoggedInUser()).Data;
            Model.Schedules = (await _svcScheduleDataService.GetFutureSchedulesByLocationId(user.LocationId)).Data;
            Model.CurrentLocationId = user.LocationId;
            Model.IsNationalAdmin = Model.CurrentLocationId == Constants.NationalLocationId;
            Model.EmailRecipientOptions = EnumHelper.GetEnumNameValues<EmailRecipientOption>();
            Model.CurrentEmailRecipientOption = EmailRecipientOption.AllBedBrigadeLeadersForMyLocation;
            Model.ShowEventDropdown = true;
        }
        
        private async Task HandleValidSubmit()
        {
        }

        private async void LocationChangeEvent(ChangeEventArgs<int, Location> args)
        {
            Model.CurrentLocationId = args.Value;
            Model.Schedules = (await _svcScheduleDataService.GetFutureSchedulesByLocationId(Model.CurrentLocationId)).Data;
            Model.CurrentScheduleId = 0;
            StateHasChanged();
        }

        private async void ScheduleChangeEvent(ChangeEventArgs<int, Data.Models.Schedule> args)
        {
            Model.CurrentScheduleId = args.Value;
            StateHasChanged();
        }

        private async void EmailRecipientChangeEvent(ChangeEventArgs<EmailRecipientOption, EmailRecipientOption> args)
        {
            Model.CurrentEmailRecipientOption = args.Value;
            Model.ShowEventDropdown = Model.CurrentEmailRecipientOption == EmailRecipientOption.VolunteersForAnEvent
                                      || Model.CurrentEmailRecipientOption ==
                                      EmailRecipientOption.BedRequestorsForAnEvent;
            StateHasChanged();
        }
    }
}
