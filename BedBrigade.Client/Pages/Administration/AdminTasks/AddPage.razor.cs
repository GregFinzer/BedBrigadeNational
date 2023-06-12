using BedBrigade.Client.Services;
using Microsoft.AspNetCore.Components;

namespace BedBrigade.Client.Pages.Administration.AdminTasks;

public partial class AddPage : ComponentBase
{
    [Inject] private ILocationService _svcLocation { get; set; }
    [Parameter] public string? imageRoute { get; set; }

    private string newPageName { get; set; }
    private string LocationName { get; set; }

    private int locationIndex = 3;
    private int pageNameIndex = 4;

    protected override async Task OnInitializedAsync()
    {
        var parameters = imageRoute.Split('/');
        newPageName = parameters[pageNameIndex];
        var locationId = Convert.ToInt32(parameters[locationIndex]);
        try
        {
            var result = await _svcLocation.GetAsync(locationId);
            if (result.Success)
            {
                LocationName = result.Data.Name;
            }
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
        }

    }
}