using BedBrigade.Common.Models;
using Microsoft.AspNetCore.Components;

namespace BedBrigade.Client.Components
{
    public partial class UpdateInformation
    {
        [Parameter, EditorRequired]
        public BaseEntity? BaseEntity { get; set; }
    }
}
