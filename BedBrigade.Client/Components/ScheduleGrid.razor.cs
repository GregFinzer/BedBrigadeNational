using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Notifications;
using System.Security.Claims;
using Action = Syncfusion.Blazor.Grids.Action;
using Syncfusion.Blazor.DropDowns;
using BedBrigade.Data.Services;
using Serilog;
using BedBrigade.Common.Constants;
using BedBrigade.Common.EnumModels;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using BedBrigade.Client.Services;


namespace BedBrigade.Client.Components
{
    public partial class ScheduleGrid : ComponentBase
    {
        // Data Services
        [Inject] private IConfigurationDataService? _svcConfiguration { get; set; }
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IUserDataService? _svcUser { get; set; }
        [Inject] private IScheduleDataService? _svcSchedule { get; set; }
        [Inject] private IAuthService? _svcAuth { get; set; }

        protected SfGrid<Schedule>? Grid { get; set; }
        private ClaimsPrincipal? Identity { get; set; }
        protected List<Schedule>? lstSchedules { get; set; }
        protected List<Location>? lstLocations;

        private string userRole = String.Empty;
        private string userName = String.Empty;
        private string userLocationName = String.Empty;
        private int userLocationId = 0;
        public bool isLocationAdmin = false;
        private string ErrorMessage = String.Empty;
        private MediaHelper.MediaUser MediaUser = new MediaHelper.MediaUser();
        private Dictionary<string, string?> dctConfiguration { get; set; } = new Dictionary<string, string?>();
        public List<EventStatusEnumItem>? lstEventStatuses { get; private set; }
        public List<EventTypeEnumItem>? lstEventTypes { get; private set; }
        public DateTime ScheduleStartDate { get; set; }
        public DateTime ScheduleEndDate { get; set; }
        public bool enabledLocationSelector { get; set; } = true;

        private const string EventDate = "EventDateScheduled";
        // Edit Form

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
        protected string? RecordText { get; set; } = "Loading Schedules ...";
        protected string? Hide { get; private set; } = "true";
        public bool NoPaging { get; private set; }
        public bool OnlyRead { get; private set; } = false;
        public int selectedScheduleId = 0;

        protected DialogSettings DialogParams = new DialogSettings { Width = "800px", MinHeight="200px", EnableResize=true };


        protected override async Task OnInitializedAsync()
        {
            await LoadUserData();         
            await LoadLocations();
            await LoadScheduleData();
            lstEventStatuses = EnumHelper.GetEventStatusItems();
            lstEventTypes = EnumHelper.GetEventTypeItems();
            await SetInitialFilter();

        } // Async Init

        private async Task SetInitialFilter()
        {
            if (Grid != null)
            {
                if (lstSchedules != null && lstSchedules.Count > 0 && Grid != null)
                {
                    Grid.SelectedRowIndex = 0;
                }

                await Grid.FilterByColumnAsync(EventDate, "greaterthanorequal",
                    DateTime.Today); // default grid filter: future events
            }
        }


        private async Task LoadUserData()
        {
            Identity = _svcAuth.CurrentUser;
            userName = Identity.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? Defaults.DefaultUserNameAndEmail;
            Log.Information($"{userName} went to the Manage Schedules Page");

            userLocationId = int.Parse(Identity.Claims.FirstOrDefault(c => c.Type == "LocationId").Value);

            if (Identity.IsInRole(RoleNames.NationalAdmin) || Identity.IsInRole(RoleNames.LocationAdmin) || Identity.IsInRole(RoleNames.LocationScheduler))
            {
                ToolBar = new List<string> { "Add", "Edit", "Delete", "Print", "Pdf Export", "Excel Export", "Csv Export", "Search", "Reset" };
                //ContextMenu = new List<string> { "Edit", "Delete", FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }
            else
            {
                ToolBar = new List<string> { "Search", "Reset" };
               // ContextMenu = new List<string> { FirstPage, NextPage, PrevPage, LastPage, "AutoFit", "AutoFitAll", "SortAscending", "SortDescending" }; //, "Save", "Cancel", "PdfExport", "ExcelExport", "CsvExport", "FirstPage", "PrevPage", "LastPage", "NextPage" };
            }



            if (Identity.IsInRole(RoleNames.NationalAdmin)) // not perfect! for initial testing
            {
                userRole = RoleNames.NationalAdmin;
            }
            else // Location User
            {
                if (Identity.IsInRole(RoleNames.LocationAdmin))
                {
                    userRole = RoleNames.LocationAdmin;
                    isLocationAdmin = true;
                }

                if (Identity.IsInRole(RoleNames.LocationAuthor))
                {
                    userRole = RoleNames.LocationAuthor;
                    isLocationAdmin = true;
                }
                if (Identity.IsInRole(RoleNames.LocationScheduler))
                {
                    userRole = RoleNames.LocationScheduler;
                    isLocationAdmin = true;
                }


            } // Get User Data
        } // User Data

        private async Task LoadLocations()
        {
            var dataLocations = await _svcLocation!.GetAllAsync();

            if (dataLocations.Success) // 
            {
                lstLocations = dataLocations.Data;
                if (lstLocations != null && lstLocations.Count > 0)
                { // select User Location Name 
                    userLocationName = lstLocations.Find(e => e.LocationId == userLocationId).Name;                    
                } // Locations found             

            }
        } // Load Locations

        private async Task LoadScheduleData()
        {
            try 
            {
                ServiceResponse<List<Schedule>> recordResult;
                if (Identity.IsInRole(RoleNames.NationalAdmin))
                {
                    recordResult = await _svcSchedule.GetAllAsync(); 
                }
                else
                {
                    recordResult = await _svcSchedule.GetSchedulesByLocationId(userLocationId);
                }

                if (recordResult.Success)
                {
                    lstSchedules = recordResult!.Data; 
                }
                else
                {
                    ErrorMessage = "Could not retrieve schedule. " + recordResult.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Could not retrieve schedule. " + ex.Message;
            }

        } 


        protected async Task OnLoad()
        {
            //TODO: Load the grid state for this grid
            //It is currently not possible to load the grid state for this grid 
            //It has to do with how we are filtering the grid client side
            //After saving the grid state, when it is loaded there are no records shown.
        }

        protected async Task OnDestroyed()
        {
            //TODO: Save the grid state for this grid
            //It is currently not possible to save the grid state for this grid 
            //It has to do with how we are filtering the grid client side probably
            //After saving the grid state, when it is loaded there are no records shown.
        }


        protected async Task OnToolBarClick(Syncfusion.Blazor.Navigations.ClickEventArgs args)
        {
            if (args.Item.Text == "Reset")
            {
               await Grid.ResetPersistDataAsync();
               //It is not possible to save the grid state for this grid for some reason
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

        public async Task OnActionBegin(Syncfusion.Blazor.Grids.ActionEventArgs<Schedule> args)
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

        private async Task Delete(Syncfusion.Blazor.Grids.ActionEventArgs<Schedule> args)
        {
            List<Schedule> records = await Grid.GetSelectedRecordsAsync();
            foreach (var rec in records)
            {
                var deleteResult = await _svcSchedule.DeleteAsync(rec.ScheduleId);
                ToastTitle = "Delete Schedule";
                if (deleteResult.Success)
                {
                    ToastContent = "Delete Successful!";
                }
                else
                {
                    ToastContent = $"Unable to Delete. Schedule is in use.";
                    args.Cancel = true;
                }
                ToastTimeout = 4000;
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });

            }
        }

        private void Add()
        {
            HeaderTitle = "Add Schedule";
            ButtonTitle = "Add Schedule";
            if (isLocationAdmin)
            {
                enabledLocationSelector = false;
            }
            else
            {
                enabledLocationSelector = true;
            }   

        }

        private async Task Save(Syncfusion.Blazor.Grids.ActionEventArgs<Schedule> args)
        {
            Schedule editSchedule = args.Data;
            
            if (editSchedule.ScheduleId != 0) // Updated schedule
            {
                //Update Schedule Record
                var updateResult = await _svcSchedule.UpdateAsync(editSchedule);
                ToastTitle = "Update Schedule";
                if (updateResult.Success)
                {
                    ToastContent = "Schedule #" + editSchedule.ScheduleId.ToString()+ " updated Successfully!";
                }
                else
                {
                    ToastContent = "Warning! Unable to update Schedule #" + editSchedule.ScheduleId.ToString() +"!!!";
                }
                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
            }
            else // new schedule
            {
                try
                {
                    var addResult = await _svcSchedule.CreateAsync(editSchedule);
                    if (addResult.Success && addResult.Data != null)
                    {
                        editSchedule = addResult.Data; // added Schedule
                    }
                    ToastTitle = "Create Schedule";
                    if (editSchedule!=null && editSchedule.ScheduleId > 0)
                    {
                        ToastContent = "Schedule #" + editSchedule.ScheduleId.ToString()+ " created Successfully!";
                    }
                    else
                    {
                        ToastContent = "Warning! Unable to add new Schedule!";
                    }
                }
                catch(Exception ex) {
                    ToastContent = "Error! "+ex.Message;
                }

                await ToastObj.ShowAsync(new ToastModel { Title = ToastTitle, Content = ToastContent, Timeout = ToastTimeout });
            }

            await Grid.Refresh();
        } // Save Schedule

        private void BeginEdit()
        {          
            HeaderTitle = "Update Schedule #"+ selectedScheduleId.ToString();
            ButtonTitle = "Update";
            enabledLocationSelector = false;          
        }

        protected async Task Save(Schedule schedule)
        {
            await Grid.EndEditAsync();
        }

        protected async Task Cancel()
        {
            await Grid.CloseEditAsync();
        }
        protected void DataBound()
        {
            if (lstSchedules.Count == 0) RecordText = "No Schedule records found";
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
                FileName = "Schedule" + DateTime.Now.ToShortDateString() + ".pdf",
                PageOrientation = Syncfusion.Blazor.Grids.PageOrientation.Landscape
            };
            await Grid.ExportToPdfAsync(ExportProperties);
        }
        protected async Task ExcelExport()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Schedule" + DateTime.Now.ToShortDateString() + ".xlsx",

            };

            await Grid.ExportToExcelAsync();
        }
        protected async Task CsvExportAsync()
        {
            ExcelExportProperties ExportProperties = new ExcelExportProperties
            {
                FileName = "Schedule" + DateTime.Now.ToShortDateString() + ".csv",

            };

            await Grid.ExportToCsvAsync(ExportProperties);
        }

        public void RowSelectHandler(RowSelectEventArgs<Schedule> args)
        {
            selectedScheduleId = args.Data.ScheduleId;
        }

        public class GridFilterOption
        {
            public string ID { get; set; }
            public string Text { get; set; }
        }

        public string DefaultFilter = "future";

        List<GridFilterOption> GridDefaultFilter = new List<GridFilterOption> {
    new GridFilterOption() { ID= "future", Text= "In the Future" },
    new GridFilterOption() { ID= "past", Text= "In the Past" },
    new GridFilterOption() { ID= "all", Text= "All Schedules" },
  };

        private async Task OnFilterChange(ChangeEventArgs<string, GridFilterOption> args)
        {   // External Grid Filtering by Event Date
            //Debug.WriteLine("The Grid Filter DropDownList Value", args.Value);

            switch (args.Value)
            {
                case "future":
                    await Grid.FilterByColumnAsync(EventDate, "greaterthanorequal", DateTime.Today);
                    break;
                case "past":
                    await Grid.FilterByColumnAsync(EventDate, "lessthan", DateTime.Today);
                    break;
                default:
                    await Grid.ClearFilteringAsync(EventDate);                  
                    break;

            }
        }

        private string cssClass { get; set; } = "e-outline";
        protected Dictionary<string, object> DescriptionHtmlAttribute { get; set; } = new Dictionary<string, object>()
        {
            { "rows", "7" },
        };

        Dictionary<string, object> htmlattributeSize = new Dictionary<string, object>()
        {
           { "maxlength", "50" },
        };


    } // class
    


} // namespace