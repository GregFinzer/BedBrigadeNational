using BedBrigade.Common;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;

namespace BedBrigade.Client.Pages.Administration.Manage
{
    public partial class MediaFiles : ComponentBase
    {
        [Inject] private ICustomSessionService _sessionService { get; set; }

        protected override void OnInitialized()
        {
            _sessionService.RemoveItemAsync(Constants.MediaDirectory).GetAwaiter().GetResult();
        }
    }
}
