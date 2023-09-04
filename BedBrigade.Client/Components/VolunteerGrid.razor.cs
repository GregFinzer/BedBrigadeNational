using BedBrigade.Client.Services;
using BedBrigade.Common;
using BedBrigade.Data.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Syncfusion.Blazor.Notifications.Internal;
using Syncfusion.Blazor.RichTextEditor;
using System.Security.Claims;
using static BedBrigade.Common.Common;
using Action = Syncfusion.Blazor.Grids.Action;

namespace BedBrigade.Client.Components
{
    public partial class VolunteerGrid : ComponentBase
    {
        [Inject] private IVolunteerDataService? _svcVolunteer { get; set; }
        [Inject] private IVolunteerForDataService? _svcVolunteerFor { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }

        [Parameter] public string? Id { get; set; }

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        private ClaimsPrincipal? Identity { get; set; }
        protected List<Volunteer>? Volunteers { get; set; }
        protected Volunteer Volunteer { get; set; } = new Volunteer();
        protected List<VolunteerFor> VolunteersFor { get; private set; }
        protected List<Location> Locations { get; private set; }
        protected string[] groupColumns = new string[] { "LocationId" };
        protected SfGrid<Volunteer>? Grid { get; set; }
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        protected string? _state { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }
        protected string? addNeedDisplay { get; private set; }
        protected string? editNeedDisplay { get; private set; }
        protected SfToast? ToastObj { get; set; }
        protected string? ToastTitle { get; set; }
        protected string? ToastContent { get; set; }
        protected int ToastTimeout { get; set; } = 3000;
        protected SfRichTextEditor RteObject { get; set; }
        protected List<ToolbarItemModel> Tools = new List<ToolbarItemModel>()
        {
            new ToolbarItemModel() { Command = ToolbarCommand.Bold },
            new ToolbarItemModel() { Command = ToolbarCommand.Italic },
            new ToolbarItemModel() { Command = ToolbarCommand.Underline },
            new ToolbarItemModel() { Command = ToolbarCommand.StrikeThrough },
            new ToolbarItemModel() { Command = ToolbarCommand.FontName },
            new ToolbarItemModel() { Command = ToolbarCommand.FontSize },
            new ToolbarItemModel() { Command = ToolbarCommand.FontColor },
            new ToolbarItemModel() { Command = ToolbarCommand.BackgroundColor },
            new ToolbarItemModel() { Command = ToolbarCommand.LowerCase },
            new ToolbarItemModel() { Command = ToolbarCommand.UpperCase },
            new ToolbarItemModel() { Command = ToolbarCommand.SuperScript },
            new ToolbarItemModel() { Command = ToolbarCommand.SubScript },
            new ToolbarItemModel() { Command = ToolbarCommand.Separator },
            new ToolbarItemModel() { Command = ToolbarCommand.Formats },
            new ToolbarItemModel() { Command = ToolbarCommand.Alignments },
            new ToolbarItemModel() { Command = ToolbarCommand.OrderedList },
            new ToolbarItemModel() { Command = ToolbarCommand.UnorderedList },
            new ToolbarItemModel() { Command = ToolbarCommand.Outdent },
            new ToolbarItemModel() { Command = ToolbarCommand.Indent },
            new ToolbarItemModel() { Command = ToolbarCommand.Separator },
            new ToolbarItemModel() { Command = ToolbarCommand.CreateLink },
            new ToolbarItemModel() { Command = ToolbarCommand.Image },
            new ToolbarItemModel() { Command = ToolbarCommand.CreateTable },
            new ToolbarItemModel() { Command = ToolbarCommand.Separator },
            new ToolbarItemModel() { Command = ToolbarCommand.ClearFormat },
            new ToolbarItemModel() { Command = ToolbarCommand.Print },
            new ToolbarItemModel() { Command = ToolbarCommand.SourceCode },
            new ToolbarItemModel() { Command = ToolbarCommand.Separator },
            new ToolbarItemModel() { Command = ToolbarCommand.Undo },
            new ToolbarItemModel() { Command = ToolbarCommand.Redo }
        };


        protected string? RecordText { get; set; } = "Loading Volunteers ...";
        protected string? Hide { get; private set; } = "true";
        public bool NoPaging { get; private set; }
        public bool OnlyRead { get; private set; } = false;

        protected DialogSettings DialogParams = new DialogSettings { Width = "900px", MinHeight = "80%" };

        /// <summary>
        /// Setup the configuration Grid component
        /// Establish the Claims Principal
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            var authState = await _authState.GetAuthenticationStateAsync();
            Identity = authState.User;
            if (Identity.IsInRole(RoleNames.NationalAdmin) || Identity.IsInRole(RoleNames.LocationAdmin))
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
                ContextMenu = new List<string> { FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }

            if (Identity.IsInRole(RoleNames.LocationAdmin) )
            {
                OnlyRead = true;
            }

            var result = await _svcVolunteer.GetAllForLocationAsync();
            if (result.Success)
            {
                Volunteers = result.Data.ToList();
            }

            var volunteerForRresult = await _svcVolunteerFor.GetAllAsync();
            if (volunteerForRresult.Success)
            {
                VolunteersFor = volunteerForRresult.Data.ToList();
            }
            var locationResult = await _svcLocation.GetAllAsync();
            if (locationResult.Success)
            {
                Locations = locationResult.Data.ToList();
            }
        }

        protected void CreatedRte()
        {
            RteObject.RefreshUI();
        }


        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (Identity.IsInRole(RoleNames.NationalAdmin) || Identity.IsInRole(RoleNames.LocationAdmin))
                {
                    Grid.EditSettings.AllowEditOnDblClick = true;
                    Grid.EditSettings.AllowDeleting = true;
                    Grid.EditSettings.AllowAdding = true;
                    Grid.EditSettings.AllowEditing = true;
                    StateHasChanged();
                }
            }
            return base.OnAfterRenderAsync(firstRender);
        }

        /// <summary>
        /// On loading of the Grid get the user grid persited data
        /// </summary>
        /// <returns></returns>
        protected async Task OnLoad()
        {
            var result = await _svcUser.GetGridPersistance(new Persist { GridId = (int)PersistGrid.BedRequest, UserState = await Grid.GetPersistData() });
            if (result.Success)
            {
                await Grid.SetPersistData(result.Data);
            }
            if (!Identity.IsInRole(RoleNames.NationalAdmin))
            {
                await Grid.ExpandAllGroupAsync();
            }

        }

        /// <summary>
        /// On destoring of the grid save its current state
        /// </summary>
        /// <returns></returns>
        protected async Task OnDestroyed()
        {
            _state = await Grid.GetPersistData();
            var result = await _svcUser.SaveGridPersistance(new Persist { GridId = (int)PersistGrid.BedRequest, UserState = _state });
            if (!result.Success)
            {
                //Log the results
            }

        }


        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if (args.Item.Text == "Reset")
            {
                await Grid.ResetPersistData();
                _state = await Grid.GetPersistData();
                await _svcUser.SaveGridPersistance(new Persist { GridId = (int)Common.Common.PersistGrid.Volunteer, UserState = _state });
                return;
            }

            if (args.Item.Text == "Pdf Export")
            {
                await PdfExport();
            }
            if (args.Item.Text == "Excel Export")
            {
                await ExcelExport();
                return;
            }
            if (args.Item.Text == "Csv Export")
            {
                await CsvExportAsync();
                return;
            }

        }

        public async Task OnActionBegin(ActionEventArgs<Volunteer> args)
        {
            var requestType = args.RequestType;
            switch (requestType)
            {
                case Action.Searching:
                    RecordText = "Searching ... Record Not Found.";
                    break;

                case Action.Delete:
                    await Delete(args);
                    break;

                case Action.Add:
                    Add();
                    break;

                case Action.Save:
                    await Save(args);
                    break;
                case Action.BeginEdit:
                    BeginEdit();
                    break;
            }

        }

        private async Task Delete(ActionEventArgs<Volunteer> args)
        {
            List<Volunteer> records = await Grid.GetSelectedRecordsAsync();
            foreach (var rec in records)
            {
                var deleteResult = await _svcVolunteer.DeleteAsync(rec.VolunteerId);
                ToastTitle = "Delete Volunteer";
                if (deleteResult.Success)
                {
                    ToastContent = "Delete Successful!";
                }
                else
                {
                    ToastContent = $"Unable to Delete. Volunteer is in use.";
                    args.Cancel = true;
                }
                ToastTimeout = 4000;
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout});

            }
        }

        private void Add()
        {
            HeaderTitle = "Add Volunteer";
            ButtonTitle = "Add Volunteer";
            Volunteer.LocationId = int.Parse(Identity.Claims.FirstOrDefault(c => c.Type == "LocationId").Value);
        }

        private async Task Save(ActionEventArgs<Volunteer> args)
        {
            Volunteer Volunteer = args.Data;
            Volunteer.Phone = Volunteer.Phone.FormatPhoneNumber();
            if (Volunteer.VolunteerId != 0)
            {
                //Update Volunteer Record
                var updateResult = await _svcVolunteer.UpdateAsync(Volunteer);
                ToastTitle = "Update Volunteer";
                if (updateResult.Success)
                {
                    ToastContent = "Volunteer Updated Successfully!";
                }
                else
                {
                    ToastContent = "Unable to update Volunteer!";
                }
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
            }
            else
            {
                // new Volunteer
                var result = await _svcVolunteer.CreateAsync(Volunteer);
                if (result.Success)
                {
                    Volunteer location = result.Data;
                }
                ToastTitle = "Create Volunteer";
                if (Volunteer.VolunteerId != 0)
                {
                    ToastContent = "Volunteer Created Successfully!";
                }
                else
                {
                    ToastContent = "Unable to save Volunteer!";
                }
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
            }

            await Grid.Refresh();
        }

        private void BeginEdit()
        {
            HeaderTitle = "Update Volunteer";
            ButtonTitle = "Update";
        }

        protected async Task Save(Volunteer location)
        {
            await Grid.EndEdit();
        }

        protected async Task Cancel()
        {
            await Grid.CloseEdit();
        }

        protected void DataBound()
        {
            if (Volunteers.Count == 0) RecordText = "No Volunteer records found";
            if (Grid.TotalItemCount <= Grid.PageSettings.PageSize)  //compare total grid data count with pagesize value 
            {
                NoPaging = true;
            }
            else
                NoPaging = false;

        }

        protected async Task PdfExport()
        {
            PdfExportProperties ExportProperties = new PdfExportProperties
            {
                FileName = "Volunteer" + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.PdfExport(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Volunteer " + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExcelExport();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Volunteer " + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.CsvExport(ExportProperties);
        }


    }
}

