using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using Action = Syncfusion.Blazor.Grids.Action;
using BedBrigade.Admin.Services;
using BedBrigade.Shared;
using System.Reflection;
using static BedBrigade.Shared.Common;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace BedBrigade.Admin.Components
{
    public partial class UserGrid : ComponentBase
    {
        [Inject] private IUserService _svcUser { get; set; }
        [Inject] private ILocalStorageService _svcLocal { get; set; }
        [Inject] private AuthenticationStateProvider _authState { get; set; }
        [Inject] private ILogger _logger { get; set; }

        private ClaimsPrincipal Identity { get; set; }
        protected SfGrid<User> Grid { get; set; }
        protected List<string> ToolBar;
        protected List<string> ContextMenu;
        protected List<User> Users { get; set; }
        protected string HeaderTitle { get; set; }
        public string ButtonTitle { get; private set; }
        public string _state { get; set; }
        public bool AddUser { get; private set; }
        public string UserPassword { get; set; }
        public bool UserPass { get; private set; }
        protected SfToast ToastObj { get; set; }
        public bool NoPaging { get; private set; }
        public string? ToastTitle { get; private set; } = string.Empty;
        public string? ToastContent { get; private set; } = string.Empty;
        public int ToastTimeout { get; private set; } = 3000;
        public string ToastWidth { get; private set; } = "300";

        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight = "550px" };

        protected override async Task OnInitializedAsync()
        {
            var authState = await _authState.GetAuthenticationStateAsync();
            Identity = authState.User;
            if (Identity.IsInRole("Admin"))
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                ContextMenu = new List<string> { "Edit", "Delete", "FirstPage", "NextPage", "PrevPage", "LastPage", "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }
            else
            {
                ToolBar = new List<string> { "Search" };
            }
            var getUsers = await _svcUser.GetAllAsync();
            if(getUsers.Success)
            {
                Users = getUsers.Data;
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
                    if(result.Success)
                    {
                        Users = result.Data;
                    }
                    Grid.CallStateHasChanged();
                    Grid.Refresh();
                    break;

                case Action.Add:
                    HeaderTitle = "Add User";
                    ButtonTitle = "Add User";
                    AddUser = true;
                    UserPass = true;
                    break;

                case Action.Save:
                    var user = args.Data;
                    if (!string.IsNullOrEmpty(user.UserName))
                    {

                        //Update User
                        var userUpdate = await _svcUser.UpdateAsync(new ServiceResponse<User> { Data = user });
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
                        var registerResult = await _svcUser.RegisterUserAsync(user);
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
                    if(userResult.Success)
                    {
                        Users = userResult.Data;
                    }
                    Grid.CallStateHasChanged();
                    Grid.Refresh();
                    break;
                case Action.BeginEdit:
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
                    break;
            }

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
            await Grid.AutoFitColumns();
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
