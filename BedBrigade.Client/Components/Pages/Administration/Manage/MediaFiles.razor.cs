using BedBrigade.Client.Services;
using BedBrigade.Common.Constants;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;

namespace BedBrigade.Client.Components.Pages.Administration.Manage
{
    public partial class MediaFiles : ComponentBase
    {
        [Inject] private ICustomSessionService _sessionService { get; set; }

        protected override void OnInitialized()
        {
            _sessionService.RemoveItemAsync(Defaults.MediaDirectory).GetAwaiter().GetResult();
        }
    }
}
