﻿using BedBrigade.Client.Services;
using BedBrigade.Data.Models;
using BedBrigade.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Security.Claims;
using Action = Syncfusion.Blazor.Grids.Action;
using static BedBrigade.Common.Common;
using static BedBrigade.Common.Extensions;
using ContentType = BedBrigade.Common.Common.ContentType;
using BedBrigade.Data.Services;

namespace BedBrigade.Client.Components
{
    public partial class LocationGrid : ComponentBase
    {
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IContentDataService? _svcContent { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private AuthenticationStateProvider? _authState { get; set; }

        [Parameter] public string? Id { get; set; }

        List<string> PageNames = new List<string> { "AboutUs", "Assembly", "Contact", "Donate", "History", "Locations", "News", "Partners", "RequestBed", "Stories", "Volunteer" };

        private const string LastPage = "LastPage";
        private const string PrevPage = "PrevPage";
        private const string NextPage = "NextPage";
        private const string FirstPage = "First";
        private ClaimsPrincipal? Identity { get; set; }
        protected List<Location>? Locations { get; set; }
        protected SfGrid<Location>? Grid { get; set; }
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
        protected int ToastTimeout { get; set; } = 1000;

        protected string? RecordText { get; set; } = "Loading Locations ...";
        protected string? Hide { get; private set; } = "true";
        public bool NoPaging { get; private set; }

        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "200px" };

        /// <summary>
        /// Setup the configuration Grid component
        /// Establish the Claims Principal
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            var authState = await _authState.GetAuthenticationStateAsync();
            Identity = authState.User;
            if (Identity.IsInRole(RoleNames.NationalAdmin))
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
                ContextMenu = new List<string> { FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }

            var result = await _svcLocation.GetAllAsync();
            if (result.Success)
            {
                Locations = result.Data.ToList();
                var item = Locations.Single(r => r.LocationId == (int)LocationNumber.National);
                if (item != null)
                {
                    Locations.Remove(item);

                }
            }
        }
        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
            {
                if (Identity.IsInRole(RoleNames.NationalAdmin))
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
            var result = await _svcUser.GetGridPersistance(new Persist { GridId = (int)PersistGrid.Location, UserState = await Grid.GetPersistDataAsync() });
            if (result.Success)
            {
                await Grid.SetPersistData(result.Data);
            }
        }

        /// <summary>
        /// On destoring of the grid save its current state
        /// </summary>
        /// <returns></returns>
        protected async Task OnDestroyed()
        {
            _state = await Grid.GetPersistData();
            var result = await _svcUser.SaveGridPersistance(new Persist { GridId = (int)PersistGrid.Location, UserState = _state });
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
                await _svcUser.SaveGridPersistance(new Persist { GridId = (int)Common.Common.PersistGrid.Location, UserState = _state });
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

        public async Task OnActionBegin(ActionEventArgs<Location> args)
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
                    Add(args);
                    break;

                case Action.Save:
                    await Save(args);
                    break;

                case Action.BeginEdit:
                    BeginEdit();
                    break;
            }

        }

        private async Task Delete(ActionEventArgs<Location> args)
        {
            string reason = string.Empty;
            List<Location> records = await Grid.GetSelectedRecordsAsync();
            ToastTitle = "Delete Location";
            ToastTimeout = 6000;
            ToastContent = $"Unable to Delete. {reason}";
            foreach (var rec in records)
            {
                try
                {
                    rec.Route.DeleteDirectory(true);
                    var deleteResult = await _svcLocation.DeleteAsync(rec.LocationId);
                    if (deleteResult.Success)
                    {
                        ToastContent = "Delete Successful!";
                    }
                    else
                    {
                        args.Cancel = true;
                    }

                }
                catch (Exception ex) 
                {
                    args.Cancel = true;
                    reason = ex.Message;

                }

                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });

            }
        }

        private void Add(ActionEventArgs<Location> args)
        {
            HeaderTitle = "Add Location";
            ButtonTitle = "Add Location";
        }

        private async Task Save(ActionEventArgs<Location> args)
        {
            Location Location = args.Data;
            if (Location.LocationId != 0)
            {
                //Update Location Record
                await UpdateLocationAsync(Location);
            }
            else
            {
                await AddNewLocationAsync(Location);
            }
            await Grid.CallStateHasChangedAsync();
            await Grid.Refresh();
        }

        private async Task AddNewLocationAsync(Location Location)
        {

            // new Location
            var result = await _svcLocation.CreateAsync(Location);
            if (result.Success)
            {
                Location location = result.Data;
                if (!Location.Route.DirectoryExists())
                {
                    Location.Route.CreateDirectory();
                    var locationRoute = GetMediaDirectory(location.Route);
                    if (!Directory.Exists(locationRoute + "/pages"))
                    {
                        locationRoute = locationRoute + "/pages";
                        CreateDirectory(locationRoute);
                        CopyDirectory($"../BedBrigade.Data/Data/Seeding/SeedImages/pages/", locationRoute);
                    }

                    await CreateContentAsync(location.LocationId, location.Name, PageNames, ContentType.Body);
                    PageNames.Clear();
                    PageNames.Add("Header0");
                    await CreateContentAsync(location.LocationId, location.Name, PageNames, ContentType.Header);
                    PageNames.Clear();
                    PageNames.Add("Footer0");
                    await CreateContentAsync(location.LocationId, location.Name, PageNames, ContentType.Footer);
                    PageNames.Clear();
                    PageNames.Add($"Home0");
                    await CreateContentAsync(location.LocationId, location.Name, PageNames, ContentType.Home);
                }
            }
            ToastTitle = "Create Location";
            if (Location.LocationId != 0)
            {
                ToastContent = "Location Created Successfully!";
            }
            else
            {
                ToastContent = "Unable to save Location!";
            }
            await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
        }

        private async Task UpdateLocationAsync(Location Location)
        {
            var updateResult = await _svcLocation.UpdateAsync(Location);
            ToastTitle = "Update Location";
            if (updateResult.Success)
            {
                ToastContent = "Location Updated Successfully!";
            }
            else
            {
                ToastContent = "Unable to update location!";
            }
            await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
        }

        private async Task CreateContentAsync(int locationId, string LocationName, List<string> names, ContentType type)
        {
            foreach (var pageName in names)
            {
                var seedHtml = GetHtml($"{pageName}.html");
                var name = string.Empty;
                switch (type)
                {
                    case ContentType.Home:
                        name = "Home";
                        break;
                    case ContentType.Header:
                        name = "Header";
                        seedHtml = seedHtml.Replace("Template", LocationName);
                        break;
                    case ContentType.Footer:
                        name = "Footer";
                        seedHtml = seedHtml.Replace("Template", LocationName);
                        break;
                    default:
                        name = pageName;
                        break;

                }
                Content content = new Content
                {
                    LocationId = locationId,
                    ContentType = type,
                    Name = name,
                    ContentHtml = seedHtml,
                };

                var result = await _svcContent.CreateAsync(content);
            }
        }

        private void BeginEdit()
        {
            HeaderTitle = "Update Location";
            ButtonTitle = "Update";
        }

        protected async Task Save(Location location)
        {
            location.Route.ToLower();
            await Grid.EndEdit();
        }

        protected async Task Cancel()
        {
            await Grid.CloseEdit();
        }

        protected void DataBound()
        {
            if (Locations.Count == 0) RecordText = "No Location records found";
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
                FileName = "Location" + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.PdfExport(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Location " + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExcelExport();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Location " + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.CsvExport(ExportProperties);
        }


    }
}

