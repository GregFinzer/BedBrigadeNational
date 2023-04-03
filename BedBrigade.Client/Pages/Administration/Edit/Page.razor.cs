using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using BedBrigade.Client;
using BedBrigade.Client.Shared;
using BedBrigade.Client.Services;
using BedBrigade.Client.Components;
using BedBrigade.Data;
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.Navigations;
using Syncfusion.Blazor.Buttons;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Layouts;
using Syncfusion.Blazor.Charts;
using Syncfusion.Blazor.Calendars;
using Syncfusion.Blazor.Cards;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Lists;
using Syncfusion.Blazor.Popups;
using Syncfusion.Blazor.ProgressBar;
using Syncfusion.Blazor.Schedule;
using Syncfusion.Blazor.RichTextEditor;
using Syncfusion.Blazor.SplitButtons;
using Syncfusion.Blazor.Sparkline;
using Syncfusion.Blazor.TreeGrid;
using Syncfusion.Blazor.CircularGauge;
using Syncfusion.Blazor.Data;

namespace BedBrigade.Client.Pages.Administration.Edit
{
    public partial class Page : ComponentBase   
    {
        public bool DialogVisible { get; set; } = false;

        protected override Task OnInitializedAsync()
        {
            DialogVisible = true;
            return base.OnInitializedAsync();
        }

    }
}