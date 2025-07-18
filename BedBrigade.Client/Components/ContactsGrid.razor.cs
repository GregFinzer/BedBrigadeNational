using BedBrigade.Client.Components.Pages.Administration.Manage;
using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;
using Action = Syncfusion.Blazor.Grids.Action;

namespace BedBrigade.Client.Components
{
    public partial class ContactsGrid : ComponentBase
    {
        [Inject] private IContactUsDataService? _svcContactUs { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private IMetroAreaDataService _svcMetroArea { get; set; }
        [Parameter] public string? Id { get; set; }

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        protected List<ContactUs>? Contacts { get; set; }
        protected List<Location>? Locations { get; set; }
        protected SfGrid<ContactUs>? Grid { get; set; }
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        protected ContactUs ContactUs { get; set; } = new ContactUs();
        protected string[] groupColumns = new string[] { "LocationId" };
        protected string? _state { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }
        protected bool OnlyRead { get; set; } = false;

        protected string? RecordText { get; set; } = "Loading Contacts ...";
        public bool NoPaging { get; private set; }
        public List<EnumNameValue<ContactUsStatus>> ContactUsStatuses { get; private set; }

        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "200px" };
        public required SfMaskedTextBox phoneTextBox;
        public string ManageContactsMessage { get; set; } = "Manage Contacts";
        /// <summary>
        /// Setup the configuration Grid component
        /// Establish the Claims Principal
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            try
            {
                _lc.InitLocalizedComponent(this);
                Log.Information($"{_svcAuth.UserName} went to the Manage Contact Page");

                SetupToolbar();

                await LoadContacts();
                await LoadLocations();

                ContactUsStatuses = EnumHelper.GetEnumNameValues<ContactUsStatus>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing ContactsGrid component");
                _toastService.Error("Error",ex.Message);
            }
        }

        private async Task LoadLocations()
        {
            var locationResult = await _svcLocation.GetActiveLocations();
            if (locationResult.Success && locationResult.Data != null)
            {
                Locations = locationResult.Data.ToList();
                var item = Locations.FirstOrDefault(r => r.LocationId == Defaults.NationalLocationId);
                if (item != null)
                {
                    Locations.Remove(item);
                }
            }
        }

        private async Task LoadContacts()
        {
            var locationId = _svcUser.GetUserLocationId();

            var userLocationResult = await _svcLocation.GetByIdAsync(locationId);
            if (userLocationResult.Success && userLocationResult.Data != null)
            {
                //If this is a metro user, get all contacts for the metro area
                if (userLocationResult.Data.IsMetroLocation())
                {
                    var metroAreaResult = await _svcMetroArea.GetByIdAsync(userLocationResult.Data.MetroAreaId.Value);

                    if (metroAreaResult.Success && metroAreaResult.Data != null)
                    {
                        if (_svcAuth.UserHasRole(RoleNames.CanManageContacts))
                        {
                            ManageContactsMessage = $"Manage Contacts for the {metroAreaResult.Data.Name} Metro Area";
                        }
                        else
                        {
                            ManageContactsMessage = $"View Contacts for the {metroAreaResult.Data.Name} Metro Area";
                        }
                    }

                    var metroLocations = await _svcLocation.GetLocationsByMetroAreaId(userLocationResult.Data.MetroAreaId.Value);

                    if (metroLocations.Success && metroLocations.Data != null)
                    {
                        var metroAreaLocationIds = metroLocations.Data.Select(l => l.LocationId).ToList();
                        var metroAreaBedRequestResult = await _svcContactUs.GetAllForLocationList(metroAreaLocationIds);
                        if (metroAreaBedRequestResult.Success && metroAreaBedRequestResult.Data != null)
                        {
                            Contacts = metroAreaBedRequestResult.Data.ToList();
                        }
                    }

                    return;
                }

                //Get By Location
                var locationResult = await _svcContactUs.GetAllForLocationAsync(userLocationResult.Data.LocationId);
                if (locationResult.Success)
                {
                    Contacts = locationResult.Data.ToList();

                    if (_svcAuth.UserHasRole(RoleNames.CanManageContacts))
                    {
                        ManageContactsMessage = $"Manage Contacts for {userLocationResult.Data.Name}";
                    }
                    else
                    {
                        ManageContactsMessage = $"View Contacts for {userLocationResult.Data.Name}";
                    }
                }
            }
        }

        private void SetupToolbar()
        {
            if (_svcAuth.UserHasRole(RoleNames.CanManageContacts))
            {
                ToolBar = new List<string>
                    { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<string>
                {
                    "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending",
                    "SortDescending"
                }; 
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
                ContextMenu = new List<string>
                {
                    FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending"
                }; 
            }
        }

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (_svcAuth.UserHasRole(RoleNames.CanManageContacts))
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
        /// On loading of the Grid get the user grid persisted data
        /// </summary>
        /// <returns></returns>
        protected async Task OnLoad()
        {
            string userName = _svcUser.GetUserName();
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.ContactUs };
            var result = await _svcUserPersist.GetGridPersistence(persist);
            if (result.Success && result.Data != null)
            {
                await Grid.SetPersistDataAsync(result.Data);
            }
        }

        /// <summary>
        /// On destroying of the grid save its current state
        /// </summary>
        /// <returns></returns>
        protected async Task OnDestroyed()
        {
            await SaveGridPersistence();
        }

        private async Task SaveGridPersistence()
        {
            _state = await Grid.GetPersistData();
            string userName = _svcUser.GetUserName();
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.ContactUs, Data = _state };
            var result = await _svcUserPersist.SaveGridPersistence(persist);
            if (!result.Success)
            {
                Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.ContactUs} : {result.Message}");
            }
        }


        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if (args.Item.Text == "Reset")
            {
                await Grid.ResetPersistData();
                await SaveGridPersistence();
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

        public async Task OnActionBegin(ActionEventArgs<ContactUs> args)
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

        private async Task Delete(ActionEventArgs<ContactUs> args)
        {
            try
            {
                List<ContactUs> records = await Grid.GetSelectedRecordsAsync();
                foreach (var rec in records)
                {
                    var deleteResult = await _svcContactUs.DeleteAsync(rec.ContactUsId);
                    if (deleteResult.Success)
                    {
                        _toastService.Success("Delete ContactUs",
                            $"ContactUs {rec.FirstName} + {rec.LastName} deleted successfully.");
                    }
                    else
                    {
                        Log.Error(
                            $"Unable to delete ContactUs {rec.FirstName} + {rec.LastName}. Error: {deleteResult.Message}");
                        _toastService.Error("Delete ContactUs",
                            $"Unable to delete ContactUs {rec.FirstName} + {rec.LastName}. Error: {deleteResult.Message}");
                        args.Cancel = true;
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting ContactUs record");
                _toastService.Error("Delete ContactUs", ex.Message);
                args.Cancel = true;
            }
        }

        private void Add()
        {
            HeaderTitle = "Add ContactUs";
            ButtonTitle = "Add ContactUs";
            ContactUs.LocationId = _svcAuth.LocationId;
        }


        private async Task Save(ActionEventArgs<ContactUs> args)
        {
            try
            {
                ContactUs contactUs = args.Data;
                contactUs.Phone = contactUs.Phone.FormatPhoneNumber();
                if (contactUs.ContactUsId != 0)
                {
                    //Update ContactUs Record
                    var updateResult = await _svcContactUs.UpdateAsync(contactUs);
                    if (updateResult.Success)
                    {
                        _toastService.Success("Update ContactUs",
                            $"ContactUs {contactUs.FirstName} {contactUs.LastName} updated successfully.");
                    }
                    else
                    {
                        Log.Error(
                            $"Unable to update ContactUs {contactUs.FirstName} {contactUs.LastName}. Error: {updateResult.Message}");
                        _toastService.Error("Update ContactUs",
                            $"Unable to update ContactUs {contactUs.FirstName} {contactUs.LastName}. Error: {updateResult.Message}");
                        args.Cancel = true;
                    }
                }
                else
                {
                    // new ContactUs
                    var result = await _svcContactUs.CreateAsync(contactUs);
                    if (result.Success)
                    {
                        _toastService.Success("Add ContactUs Success",
                            $"ContactUs {contactUs.FirstName} {contactUs.LastName} added successfully.");
                    }
                    else
                    {
                        Log.Error(
                            $"Unable to add ContactUs {contactUs.FirstName} {contactUs.LastName}. Error: {result.Message}");
                        _toastService.Error("Add ContactUs Error",
                            $"Unable to add ContactUs {contactUs.FirstName} {contactUs.LastName}. Error: {result.Message}");
                        args.Cancel = true;
                    }
                }

                await Grid.Refresh();

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving ContactUs record");
                _toastService.Error("Error with Save ContactUs", ex.Message);
                args.Cancel = true;
            }
        }

        private void BeginEdit()
        {
            HeaderTitle = "Update Contactus";
            ButtonTitle = "Update";
        }

        protected async Task Save(ContactUs contactUs)
        {
            await Grid.EndEdit();
        }

        protected async Task Cancel()
        {
            await Grid.CloseEdit();
        }

        protected void DataBound()
        {
            if (Contacts.Count == 0) RecordText = "No ContactUs records found";
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
                FileName = "ContactUs" + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.PdfExport(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "ContactUs " + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExcelExport();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "ContactUs " + DateTime.Now.ToShortDateString() + ".csv",

            };
            if (Grid != null)
            {
                await Grid.CsvExport(ExportProperties);
            }
        }
        public async Task HandlePhoneMaskFocus()
        {
            await JS.InvokeVoidAsync("BedBrigadeUtil.SelectMaskedText", phoneTextBox.ID, 0);
        }


    }
}

