using BedBrigade.Client.Services;
using BedBrigade.Data.Models;
using BedBrigade.Common;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Security.Claims;
using Action = Syncfusion.Blazor.Grids.Action;
using static BedBrigade.Common.Common;

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
        protected UserRegister userRegister { get; set; } = new UserRegister();
        protected string Password { get; set; } = string.Empty;
        public User user { get; set; } = new User();

        protected List<Role> Roles { get; private set; }
        protected DialogSettings DialogParams = new DialogSettings { Width = "900px", MinHeight = "550px" };

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

            var getRoles = await _svcUser.GetRolesAsync();
            if(getRoles.Success)
            {
                Roles = getRoles.Data;
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

        /// <summary>
        /// On loading of the Grid get the user grid persited data
        /// </summary>
        /// <returns></returns>
        protected async Task OnLoad()
        {
            var result = await _svcUser.GetPersistAsync(new Persist { GridId = (int)PersistGrid.User, UserState = await Grid.GetPersistData() });
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
            var result = await _svcUser.SavePersistAsync(new Persist { GridId = (int)PersistGrid.User, UserState = _state });
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
                await _svcUser.SavePersistAsync(new Persist { GridId = (int)Common.Common.PersistGrid.User, UserState = _state });
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
            if (Identity.HasRole("National Admin, Location Admin"))
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
            user.Phone = user.Phone.FormatPhoneNumber();
            user.Role = await GetUserRoleName(user);
            if (user.PasswordHash != null)
            {
                await UpdateUser(user);
            }
            else
            {
                // new 

                await AddNewUser(user);
                //args.Cancel = true;
            }
            var userResult = await _svcUser.GetAllAsync();
            if (userResult.Success)
            {
                BBUsers = userResult.Data;  
            }
            await Grid.CallStateHasChangedAsync();
            await Grid.Refresh();
        }

        private async Task AddNewUser(User user)
        {
            userRegister.user = user;
            userRegister.ConfirmPassword = userRegister.Password;
            var registerResult = await _svcAuth.RegisterAsync(userRegister);
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
            await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
        }

        private async Task UpdateUser(User user)
        {
            string passwordChanged = string.Empty;
            if (!string.IsNullOrEmpty(userRegister.Password))
            {
                UserChangePassword changePassword = new UserChangePassword() { UserId = user.UserName, Password = userRegister.Password, ConfirmPassword = userRegister.Password };
                var result = await _svcAuth.ChangePassword(changePassword);
                if (result.Success)
                {
                    user = result.Data;                   
                    passwordChanged = "and password updated ";
                }
            }

            var userUpdate = await _svcUser.UpdateAsync(user);
            ToastTitle = "Update User";
            if (userUpdate.Success)
            {
                ToastContent = $"User Updated {passwordChanged} Successfully!";

            }
            else
            {
                ToastContent = "Unable to update User!";
            }
            await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
        }

        private async Task<Location> GetUserLocation(User user)
        {
            var locationResult = await _svcLocation.GetAsync(user.LocationId);
            if (locationResult.Success)
            {
               return locationResult.Data;
            }
            return new Location();
        }

        private async Task<string> GetUserRoleName(User user)
        {
            var result = await _svcUser.GetRoleAsync(user.FkRole);
            if (result.Success)
            {
                return result.Data.Name;
            }
            return string.Empty;
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
                    await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
                }

            }
            var result = await _svcUser.GetAllAsync();
            if (result.Success)
            {
                BBUsers = result.Data;
            }
            await Grid.CallStateHasChangedAsync();
            await Grid.Refresh();
        }

        protected async Task Save(User user)
        {
            await Grid.EndEditAsync();
        }
        protected async Task Cancel()
        {
            await Grid.CloseEditAsync();
        }
        protected async Task DataBound()
        {
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