using BedBrigade.Client.Services;
using Microsoft.AspNetCore.Components;
using static BedBrigade.Common.Extensions;

namespace BedBrigade.Client.Pages.Administration.AdminTasks;

public partial class AddPage : ComponentBase
{
    [Parameter] public string? imageRoute { get; set; }

    private string newPageName { get; set; }

    private const int PageName = 5;
    private const int Location = 4;

    protected override async Task OnInitializedAsync()
    {
        var parameters = imageRoute.Split('/');

        newPageName = parameters[PageName];
        var media = GetAppRoot($"temp-work\\{parameters[Location]}\\pages");
        if(!Directory.Exists(media))
        {
            Directory.CreateDirectory($"{media}\\{newPageName}\\images");
        }
    }
}