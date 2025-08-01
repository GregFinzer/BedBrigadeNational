using BedBrigade.Common.Constants;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Serilog;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;
using System.Security.Claims;
using Action = Syncfusion.Blazor.Grids.Action;

namespace BedBrigade.Client.Components
{

    public partial class UsersGrid : ComponentBase
    {
        [Inject] private IUserDataService _svcUser { get; set; }
        [Inject] private IUserPersistDataService? _svcUserPersist { get; set; }
        [Inject] private IAuthService _svcAuth { get; set; }
        [Inject] private IAuthDataService _svcAuthData { get; set; }
        [Inject] private ILocationDataService _svcLocation { get; set; }
        [Inject] private ILanguageContainerService _lc { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        [Inject] private ToastService _toastService { get; set; }
        private ClaimsPrincipal Identity { get; set; }
        protected SfGrid<User>? Grid { get; set; }
        
        protected List<string>? ToolBar;
        protected List<string>? ContextMenu;
        protected string[] groupColumns = new string[] { "LocationId" };
        protected List<User>? BBUsers { get; set; }
        protected string? HeaderTitle { get; set; }
        protected string? ButtonTitle { get; private set; }
        protected string? _state { get; set; }
        protected bool AddUser { get; private set; }
        protected bool UserPass { get; private set; }

        protected bool NoPaging { get; private set; }
        public List<Location>? Locations { get; private set; }
        protected UserRegister userRegister { get; set; } = new UserRegister();
        protected string Password { get; set; } = string.Empty;
        public User user { get; set; } = new User();

        protected List<Role> Roles { get; private set; }
        public bool PasswordVisible { get; private set; }
        public string displayError { get; private set; } = "none;";

        protected DialogSettings DialogParams = new DialogSettings { Width = "900px", MinHeight = "550px" };
        public required SfMaskedTextBox phoneTextBox;
        public string ManageUsersMessage { get; set; } = "Manage Users";
        private string _userLocationName = string.Empty;
        protected override async Task OnInitializedAsync()
        {
            try
            {

                _lc.InitLocalizedComponent(this);
                PasswordVisible = false;

                Identity = _svcAuth.CurrentUser;

                Log.Information($"{_svcAuth.UserName} went to the Manage Users Page");


                var getRoles = await _svcUser.GetRolesAsync();
                if (getRoles.Success)
                {
                    Roles = getRoles.Data;
                }

                //TODO:  Refactor
                await LoadUsers();


                var getLocations = await _svcLocation.GetActiveLocations();
                if (getLocations.Success && getLocations.Data != null)
                {
                    Locations = getLocations.Data;

                    if (_svcAuth.LocationId == 0)
                    {
                        _userLocationName = "Unknown";
                    }
                    else if (_svcAuth.LocationId == -1)
                    {
                        _userLocationName = Locations.Find(o => o.LocationId == _svcAuth.LocationId).Name;
                    }
                }

                SetupToolbar();

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error initializing UsersGrid component");
                _toastService.Error("Error", "An error occurred while loading the user data.");
            }
        }

        private void SetupToolbar()
        {
            if (_svcAuth.UserHasRole(RoleNames.CanManageUsers))
            {
                ToolBar = new List<string>
                {
                    "Add", "Edit", "Password", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export",
                    "Search", "Reset"
                };
                ContextMenu = new List<string>
                {
                    "Edit", "Password", "Delete", "FirstPage", "NextPage", "PrevPage", "LastPage", "AutoFit",
                    "AutoFitAll", "SortAscending", "SortDescending"
                }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };


                if (_svcAuth.IsNationalAdmin)
                {
                    ManageUsersMessage=  $"Manage Users Nationally";
                }
                else
                {
                    ManageUsersMessage = $"Manage Users for {_userLocationName}";
                }
            }
            else
            {
                ToolBar = new List<string> { "Search" };
                ManageUsersMessage = $"View Users for {_userLocationName}";
            }
        }

        private async Task LoadUsers()
        {
            bool isNationalAdmin = _svcUser.IsUserNationalAdmin();
            if (isNationalAdmin)
            {
                var allResult = await _svcUser.GetAllAsync();

                if (allResult.Success && allResult.Data != null)
                {
                    BBUsers = allResult.Data.ToList();
                }
                else
                {
                    _toastService.Error("Error", "Unable to load users. Please try again later.");
                    Log.Error($"Error loading users: {allResult.Message}");
                }
            }
            else
            {
                int userLocationId = _svcUser.GetUserLocationId();
                var contactUsResult = await _svcUser.GetAllForLocationAsync(userLocationId);
                if (contactUsResult.Success && contactUsResult.Data != null)
                {
                    BBUsers = contactUsResult.Data.ToList();
                }
                else
                {
                    _toastService.Error("Error", "Unable to load users for the current location. Please try again later.");
                    Log.Error($"Error loading users for location {userLocationId}: {contactUsResult.Message}");
                }
            }
        }

        /// <summary>
        /// On loading of the Grid get the user grid persisted data
        /// </summary>
        /// <returns></returns>
        protected async Task OnLoad()
        {
            string userName = _svcUser.GetUserName();
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.User };
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
            UserPersist persist = new UserPersist { UserName = userName, Grid = PersistGrid.User, Data = _state };
            var result = await _svcUserPersist.SaveGridPersistence(persist);
            if (!result.Success)
            {
                Log.Error($"Unable to save grid state for {userName} for grid {PersistGrid.User} : {result.Message}");
            }
        }

        protected async Task OnRowDeselected(RowDeselectEventArgs<User> args)
        {
            var record = await Grid.GetSelectedRecordsAsync();
            if (record != null)
            {
                await Grid.EnableToolbarItemsAsync(new List<string>() { "UserGrid_Password" }, false);
            }
        }

        protected async Task OnRowSelected(RowSelectEventArgs<User> args)
        {
            var record = await Grid.GetSelectedRecordsAsync();
            if (record != null)
            {
                await Grid.EnableToolbarItemsAsync(new List<string>() { "UserGrid_Password" }, true);
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

            if (args.Item.Text == "Password")
            {
                await ChangePasswordAsync();
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

        private async Task ChangePasswordAsync()
        {
            userRegister.ConfirmPassword = userRegister.Password = string.Empty;
            displayError = "none;";
            PasswordVisible = true;
        }
        private async Task NewPassword()
        {
            var records = await Grid.GetSelectedRecords();
            if (records != null)
            {
                userRegister.user = records[0];
            }
            string passwordChanged = string.Empty;
            if (!string.IsNullOrEmpty(userRegister.Password) && userRegister.Password == userRegister.ConfirmPassword)
            {
                UserChangePassword changePassword = new UserChangePassword() { UserId = userRegister.user.UserName, Password = userRegister.Password, ConfirmPassword = userRegister.Password };
                var result = await _svcAuthData.ChangePassword(changePassword.UserId, changePassword.Password);
                if (result.Success)
                {
                    _toastService.Success("Change Password", "Password Changed Successfully!");
                    displayError = "none;";
                    PasswordVisible = false;
                }
                else
                {
                    Log.Error($"Unable to change password for user {changePassword.UserId}: {result.Message}");
                    _toastService.Error("Change Password", $"Unable to change password!<br/>Correct the following errors:<br/>{result.Message}");
                    displayError = "block;";
                }

            }
            else
            {
                displayError = "block;"; 
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
            if (Identity.HasRole(RoleNames.CanManageUsers))
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

            await LoadUsers();


            await Grid.CallStateHasChangedAsync();
            await Grid.Refresh();
        }

        private async Task AddNewUser(User user)
        {
            userRegister.user = user;
            userRegister.ConfirmPassword = userRegister.Password;
            var registerResult = await _svcAuthData.Register(userRegister.user, userRegister.Password);
            
            if (registerResult.Success)
            {
                _toastService.Success("Add User Success", "User Created Successfully!");
            }
            else
            {
                Log.Error($"Unable to add user {user.UserName}: {registerResult.Message}");
                _toastService.Error("Add User Error", $"Unable to add user! {registerResult.Message}");
            }
        }

        private async Task UpdateUser(User user)
        {
            var userUpdate = await _svcUser.UpdateAsync(user);
            if (userUpdate.Success)
            {
                _toastService.Success("Update User Success", "User Updated Successfully!");
            }
            else
            {
                Log.Error($"Unable to update user {user.UserName}: {userUpdate.Message}");
                _toastService.Error("Update User Error", $"Unable to update user! {userUpdate.Message}");
            }
        }

        private async Task<Location> GetUserLocation(User user)
        {
            var locationResult = await _svcLocation.GetByIdAsync(user.LocationId);
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
                var deleted = await _svcUser.DeleteAsync(rec.UserName);
                if (deleted.Success)
                {
                    _toastService.Success("Delete User", $"User {rec.UserName} deleted successfully!");
                }
                else
                {
                    Log.Error($"Unable to delete user {rec.UserName}: {deleted.Message}");
                    _toastService.Error("Delete User", $"Unable to delete user {rec.UserName}! {deleted.Message}");
                }
            }

            await LoadUsers();

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
        protected void DataBound()
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
                FileName = "Users " + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.ExportToPdfAsync(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Users " + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExportToExcelAsync(ExportProperties);
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Users " + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.ExportToCsvAsync(ExportProperties);
        }
        public async Task HandlePhoneMaskFocus()
        {
            await JS.InvokeVoidAsync("BedBrigadeUtil.SelectMaskedText", phoneTextBox.ID, 0);
        }
    }
}