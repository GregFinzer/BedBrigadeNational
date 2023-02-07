using BedBrigade.Client.Services;
using BedBrigade.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Security.Claims;
using static BedBrigade.Data.Shared.Common;
using Action = Syncfusion.Blazor.Grids.Action;

namespace BedBrigade.Client.Components
{

    public partial class UsersGrid : ComponentBase
    {
        [Inject] private IUserService _svcUser { get; set; }
        [Inject] private IAuthService _svcAuth { get; set; }
        [Inject] private ILocationService _svcLocation { get; set; }
        [Inject] private AuthenticationStateProvider _authState { get; set; }
        [Inject] private ILogger<User> _logger { get; set; }

        private ClaimsPrincipal Identity { get; set; }
        protected SfGrid<User>? Grid { get; set; }
        protected SfDropDownList<string, Role>? RoleDD { get; set; }
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        protected List<User>? BBUsers { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }
        protected string? _state { get; set; }
        protected bool AddUser { get; private set; }
        protected string? UserPassword { get; set; }
        protected bool UserPass { get; private set; }
        protected SfToast? ToastObj { get; set; }
        protected bool NoPaging { get; private set; }
        protected string? ToastTitle { get; private set; } = string.Empty;
        protected string? ToastContent { get; private set; } = string.Empty;
        protected int ToastTimeout { get; private set; } = 3000;
        protected string ToastWidth { get; private set; } = "300";
        public List<Location>? Locations { get; private set; }

        protected List<Role> Roles = new List<Role>()
    {
        new Role { Id = "National Admin", Name = "National Admin" },
        new Role { Id = "National Editor", Name = "National Editor" },
        new Role { Id = "Location Admin", Name = "Location Admin" },
        new Role { Id = "Location Contributor", Name = "Location Contributor" },
        new Role { Id = "Location Author", Name = "Location Author" },
        new Role { Id = "Location Editor", Name = "Location Editor" },
        new Role { Id = "Location Scheduler", Name = "Location Scheduler" },
        new Role { Id = "Location Communications", Name = "Location Communications" },
    };
        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "550px" };

        protected override async Task OnInitializedAsync()
        {
            _logger.LogInformation("Starting User Grid");
            var authState = await _authState.GetAuthenticationStateAsync();
            
            Identity = authState.User;
            if (Identity.IsInRole("National Admin") || Identity.IsInRole("Location Admin"))
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<string> { "Edit", "Delete", "FirstPage", "NextPage", "PrevPage", "LastPage", "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }
            else
            {
                ToolBar = new List<string> { "Search" };
            }
            var getUsers = await _svcUser.GetAllAsync();
            if (getUsers.Success)
            {
                BBUsers = getUsers.Data;
            }
            var getLocations = await _svcLocation.GetAllAsync();
            if(getLocations.Success)
            {
                Locations = getLocations.Data;
            }

            //Users = result.Success ? result.Data : new ErrorHandler(_logger).ErrorHandlerAsync(this.GetType().Module.Name,result.Message);

        }


        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if (args.Item.Text == "Reset")
            {
                await Grid.ResetPersistData();
                _state = await Grid.GetPersistData();
                await _svcUser.SavePersistAsync(new Persist { GridId = (int)PersistGrid.User, UserState = _state });
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
        public async Task OnActionBegin(ActionEventArgs<User> args)
        {

            var requestType = args.RequestType;
            switch (requestType)
            {
                case Action.Delete:
                    await Delete();
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

        private void BeginEdit()
        {
            HeaderTitle = "Update User";
            ButtonTitle = "Update User";
            AddUser = false;
            if (Identity.IsInRole("Admin"))
            {
                UserPass = true;
            }
            else
            {
                UserPass = false;
            }
        }

        private async Task Save(ActionEventArgs<User> args)
        {
            var user = args.Data;
            if (!string.IsNullOrEmpty(user.UserName))
            {

                //Update User
                var userUpdate = await _svcUser.UpdateAsync(user);
                ToastTitle = "Update User";
                if (userUpdate.Success)
                {
                    ToastContent = "User Updated Successfully!";
                }
                else
                {
                    ToastContent = "Unable to update User!";
                }
                ToastTimeout = 3000;
                await ToastObj.Show();
            }
            else
            {
                // new 
                var newUser = new UserRegister { user = user, Password = string.Empty };
                var registerResult = await _svcAuth.RegisterAsync(newUser);
                ToastTitle = "Create User";
                if (registerResult.Success)
                {
                    ToastContent = "User Created Successfully!";
                }
                else
                {
                    ToastContent = $"Unable to create User!<br/>Correct the following errors:<br/>";
                    ToastContent += registerResult.Message + "<br/>";
                    ToastTimeout = 20000;
                    ToastWidth = "400";

                }
                await ToastObj.Show();
                args.Cancel = true;
            }
            var userResult = await _svcUser.GetAllAsync();
            if (userResult.Success)
            {
                BBUsers = userResult.Data;
            }
            Grid.CallStateHasChanged();
            Grid.Refresh();
        }

        private void Add()
        {
            HeaderTitle = "Add User";
            ButtonTitle = "Add User";
            AddUser = true;
            UserPass = true;
        }

        private async Task Delete()
        {
            List<User> records = await Grid.GetSelectedRecords();
            foreach (var rec in records)
            {
                var deleted = await _svcUser.DeleteUserAsync(rec.UserName);
                if (deleted.Success)
                {
                    ToastTitle = "Delete User";
                    ToastContent = "Deleted Successful!";
                    await ToastObj.Show();
                }

            }
            var result = await _svcUser.GetAllAsync();
            if (result.Success)
            {
                BBUsers = result.Data;
            }
            Grid.CallStateHasChanged();
            Grid.Refresh();
        }

        protected async Task Save(User status)
        {
            await Grid.EndEdit();
        }
        protected async Task Cancel()
        {
            await Grid.CloseEdit();
        }
        protected async Task DataBound()
        {
            //await Grid.AutoFitColumns();
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
                FileName = "Status " + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.PdfExport(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Facilities " + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExcelExport();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Facilities " + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.CsvExport(ExportProperties);
        }

    }
}