using Microsoft.AspNetCore.Components;

namespace BedBrigade.Client.Components.Pages;
public partial class DonationCancellation : ComponentBase
{
    [Inject] private ILanguageContainerService _lc { get; set; }

    protected override void OnInitialized()
    {
        _lc.InitLocalizedComponent(this);
    }
}

