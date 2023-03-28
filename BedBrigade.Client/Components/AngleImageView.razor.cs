using Microsoft.AspNetCore.Components;

namespace BedBrigade.Client.Components
{
    public partial class AngleImageView
    {

        [Parameter] public string Caption { get; set; } = "Bed Brigade";
        [Parameter] public string Path { get; set; } = "National";

    }
}
