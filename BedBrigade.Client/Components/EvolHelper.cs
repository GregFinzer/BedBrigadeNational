using BedBrigade.Client.Components.Pages.Administration.Manage;
using BedBrigade.Data.Models;
using BedBrigade.Data.Seeding;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Primitives;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Logical;
using Syncfusion.Blazor;
using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Lists.Internal;
using System.Data.Entity;
using System.Diagnostics;
using System.Text;
using static BedBrigade.Common.Logic.Common;
using static System.Net.Mime.MediaTypeNames;

namespace BedBrigade.Client.Components
{
    public static partial class EvolHelper
    {
        public static async Task<bool> GetIdColumnsConfigurations(IConfigurationDataService _svcConfiguration)
        {
            var bDisplayId = false;
            Dictionary<string, string?> dctConfiguration  = new Dictionary<string, string?>();
            var dataConfiguration = await _svcConfiguration.GetAllAsync(ConfigSection.System); // Configuration ============================
            if (dataConfiguration.Success && dataConfiguration != null)
            {
                dctConfiguration = dataConfiguration.Data.ToDictionary(keySelector: x => x.ConfigurationKey, elementSelector: x => x.ConfigurationValue);
                if (dctConfiguration != null)
                {
                    var DisplayIdFields = dctConfiguration["DisplayIdFields"].ToString();
                    if (DisplayIdFields == "Yes")
                    {
                        bDisplayId = true;
                    }
                }
            }
                      
            return(bDisplayId);
        } // id columns Configuration

        public static async Task<List<Volunteer>> GetVolunteers(IVolunteerDataService _svcVolunteer)
        {
            var dataVolunteer = await _svcVolunteer.GetAllAsync(); // get Schedules           
            if (dataVolunteer.Success && dataVolunteer != null)
            {
                if (dataVolunteer.Data.Count > 0)
                {
                    return(dataVolunteer.Data.ToList()); 
                } 
            }

            return (null);
        }// Get Volunteers

        public static async Task<List<Schedule>> GetSchedules (IScheduleDataService? _svcSchedule, bool isLocationAdmin, int userLocationId)
        {
            var dataSchedules = await _svcSchedule.GetAllAsync();
            if (dataSchedules.Success) // 
            {
                var Schedules = dataSchedules.Data.ToList();
                if(Schedules != null && Schedules.Count > 0)
                { // select Location Schedules
                    if (isLocationAdmin)
                    {
                        Schedules = Schedules.FindAll(s => s.LocationId == userLocationId);
                    }
                    // only future && scheduled
                    Schedules = Schedules.FindAll(s => s.EventDateScheduled >= DateTime.Today && s.EventStatus == EventStatus.Scheduled);

                    return (Schedules);
                }
            }

            return null;

        } // Schedules

        public static async Task<List<VolunteerEvent>> GetVolunteerEvents(IVolunteerEventsDataService _svcVolunteerEvents, bool isLocationAdmin, int userLocationId)
        {
           
                var dataEvents = await _svcVolunteerEvents.GetAllAsync();
                if (dataEvents.Success) // 
                {
                    var VolunteerEvents = dataEvents.Data.ToList();
                    if (VolunteerEvents != null && VolunteerEvents.Count > 0)
                    { // select Location Volunteers Events
                        if (isLocationAdmin)
                        {
                            VolunteerEvents = VolunteerEvents.FindAll(e => e.LocationId == userLocationId);
                        }
                        return VolunteerEvents;
                    }
                }

                return null;
        } // Load Volunteer Events


        public static async Task<List<string>> GetEvolDataStatusAsync(IScheduleDataService? _svcSchedule, IVolunteerDataService? _svcVolunteer)
        {
            var bTableStatus = false;
            var lstEmptyTables = new List<string>();
            // Schedules
            var dataTable = await _svcSchedule.GetAllAsync();            
            if (dataTable.Success && dataTable.Data.ToList().Count > 0)  
            {
                bTableStatus = true;
            }
            if(!bTableStatus)
            {
                lstEmptyTables.Add("Schedules");
            }
            // Volunteers            
            bTableStatus = false;
            var dataTableV = await _svcVolunteer.GetAllAsync();
            if (dataTableV.Success && dataTableV.Data.ToList().Count > 0)
            {
                bTableStatus = true;
            }
            if (!bTableStatus)
            {
                lstEmptyTables.Add("Volunteers");
            }

            return (lstEmptyTables);
        } // GetEvolDataStatus

        public static MarkupString GetEvolDataStatusMessage(List<string> lstEmptyTables)
        {
            var strHtml = String.Empty;
            var sbHtml = new StringBuilder();
            var AlertStyle = "danger";

            sbHtml.Append("<div class='alert alert-" + AlertStyle + "' >");
            sbHtml.Append("<h2>Page 'Events Volunteers' is not available right now</h2>");
            sbHtml.Append("We noticed that some of the database tables, required for Events Volunteers management, are currently empty:<br />");
            if (lstEmptyTables.Count > 0)
            {
                sbHtml.Append("<ul>");
                foreach (var item in lstEmptyTables)
                {
                    sbHtml.Append("<li>"+item+"</li>");
                }
                sbHtml.Append("</ul>");
            }
            sbHtml.Append("Please make sure to populate these tables with the necessary data.<br />");
            sbHtml.Append("If you have any questions or need assistance with this process, feel free to reach out to our support team.");
            sbHtml.Append("</div");

            strHtml=sbHtml.ToString();
            return (MarkupString)strHtml;

        } // Get Evol Data Sttaus

        public static List<Volunteer> GetGridDataSource(List<VolunteerEvent>? VolunteerEvents = null, List<Schedule>? Schedules = null, List<Volunteer>? Volunteers = null, List<Location>? Locations = null)
        {
            var EventVolunteers = new List<Volunteer>();
            // step 1 - load all registered volunteers (for future events)  to combined class
            if (VolunteerEvents != null && Volunteers != null && VolunteerEvents.Count > 0)
            {

                EventVolunteers = (from ve in VolunteerEvents
                                   join e in Schedules on ve.ScheduleId equals e.ScheduleId
                                   join v in Volunteers on ve.VolunteerId equals v.VolunteerId
                                   join l in Locations on e.LocationId equals l.LocationId
                                   select new Volunteer
                                   {
                                       RegistrationId = ve.RegistrationId,
                                       VolunteerId = ve.VolunteerId,
                                       EventId = e.ScheduleId,
                                       // Event Fields
                                       EventLocationId = ve.LocationId,
                                       EventLocationName = l.Name,
                                       EventName = e.EventName,
                                       EventDate = e.EventDateScheduled,
                                       EventType = e.EventType,
                                       // Volunteer Fields
                                       IHaveVolunteeredBefore = v.IHaveVolunteeredBefore,
                                       FirstName = v.FirstName,
                                       LastName = v.LastName,
                                       Phone = v.Phone,
                                       Email = v.Email,
                                       OrganizationOrGroup = v.OrganizationOrGroup,
                                       Message = v.Message,
                                       VehicleType = v.VehicleType,
                                       CreateDate = ve.CreateDate
                                   }
                            ).ToList();
            }

            // step 2 - add future events without volunteers to combined class
            if (Schedules != null && Schedules.Count > 0)
            {
                EventVolunteers.AddRange(
                    (from e in Schedules
                     where !(from ev in EventVolunteers
                             select ev.EventId).Contains(e.ScheduleId)
                     join l in Locations on e.LocationId equals l.LocationId
                     select new Volunteer
                     {
                         EventId = e.ScheduleId,
                         EventLocationId = e.LocationId,
                         EventLocationName = l.Name,
                         EventName = e.EventName,
                         EventDate = e.EventDateScheduled,
                         EventType = e.EventType
                     }
                        ).ToList()
                    );
            }
            //  strJson = JsonConvert.SerializeObject(EventVolunteers, Formatting.Indented);
            //  strHtml = "<pre>" + strJson + "</pre>";

            return (EventVolunteers);
        } // Create Grid Data Source

        public static Volunteer PrepareVolunteerDeleteDialog(Volunteer selectedGridObject, ref string strMessageText, ref string DialogTitle, ref string DisplayDeleteButton, ref string CloseButtonCaption)
        {
            var newVolunteer = new Volunteer();
            DialogTitle = "Delete Volunteer from Event";
            DisplayDeleteButton = ""; // show OK button
            CloseButtonCaption = "Cancel";
            strMessageText = "Selected Volunteer will be deleted from Event";
            strMessageText += "</br>Do you still want to delete this Volunteer?";
            // copy Volunteer Data to Display                        
            newVolunteer.RegistrationId = selectedGridObject.RegistrationId;
            newVolunteer.VolunteerId = selectedGridObject.VolunteerId;
            newVolunteer.FirstName = selectedGridObject.FirstName;
            newVolunteer.LastName = selectedGridObject.LastName;
            newVolunteer.Phone = selectedGridObject.Phone;
            newVolunteer.Email = selectedGridObject.Email;
            newVolunteer.VehicleType = selectedGridObject.VehicleType;
            return (newVolunteer);

        } // delete dialog

        public static List<Volunteer> GetLocationVolunteersSelector(Volunteer selectedGridObject, List<Volunteer>? lstVolunteerSelector, List<VolunteerEvent>? VolunteerEvents)
        {
            var lstLocationVolunteers = new List<Volunteer>();
            var lstLocVolunteers = lstVolunteerSelector.FindAll(vs => vs.LocationId == selectedGridObject.EventLocationId);
            // Event Linked Volunteers
            if (VolunteerEvents != null && VolunteerEvents.Count() > 0)
            {
                var lstEventLinkedVolunteers = VolunteerEvents.FindAll(ve => ve.ScheduleId == selectedGridObject.EventId);
                // Available Volunteers, not linked to current Event
                lstLocationVolunteers = (
                         (from v in lstLocVolunteers // location volunteers                                                                                                 
                          where !(from lv in lstEventLinkedVolunteers
                                  select lv.VolunteerId).Contains(v.VolunteerId)
                          select new Volunteer
                          {
                              VolunteerId = v.VolunteerId,
                              FirstName = v.FirstName,
                              LastName = v.LastName,
                              Email = v.Email,
                              Phone = v.Phone,
                              IHaveVolunteeredBefore = v.IHaveVolunteeredBefore,
                              VehicleType = v.VehicleType
                          }
                          ).OrderBy(loc => loc.SearchName).ToList()
                         );
            }
            else
            {
                lstLocationVolunteers = (
                        (from v in lstLocVolunteers // location volunteers                                                                                                                         
                         select new Volunteer
                         {
                             VolunteerId = v.VolunteerId,
                             FirstName = v.FirstName,
                             LastName = v.LastName,
                             Email = v.Email,
                             Phone = v.Phone,
                             IHaveVolunteeredBefore = v.IHaveVolunteeredBefore,
                             VehicleType = v.VehicleType
                         }
                         ).OrderBy(loc => loc.SearchName).ToList()
                        );
            }

            return (lstLocationVolunteers);

        } // Get Location Volunteers

        public static async Task<bool>  UpdateSchedule(IScheduleDataService? _svcSchedule, List<Schedule> Schedules, string strAction, int intScheduleId, VehicleType CarType)
        {
            var bScheduleUpdated = false;
            var bUpdate = true;
            var mySchedule = Schedules.FirstOrDefault(s => s.ScheduleId == intScheduleId);
            switch (strAction)
            {
                case "Add":
                    if (mySchedule != null) // update 
                    {
                        mySchedule.VolunteersRegistered++;
                        if (CarType != VehicleType.NoCar)
                        {
                            mySchedule.VehiclesDeliveryRegistered++;
                        }
                    }
                    break;
                case "Del":
                    if (mySchedule != null) // update 
                    {
                        if (mySchedule.VolunteersRegistered > 0)
                        {
                            mySchedule.VolunteersRegistered--;
                        }
                        if (CarType != VehicleType.NoCar && mySchedule.VehiclesDeliveryRegistered > 0)
                        {
                            mySchedule.VehiclesDeliveryRegistered--;
                        }
                    }
                    break;
                default:
                    bUpdate = false;
                    // Do Nothing
                    break;
            } // switch action
            // update Schedule Table Record                 
            if (bUpdate && mySchedule!=null && _svcSchedule != null)
            {
                var dataUpdate = await _svcSchedule.UpdateAsync(mySchedule);

                if (dataUpdate.Success)
                {
                    bScheduleUpdated = true;
                }                  
            }

            return (bScheduleUpdated);

        } // update Schedule

    } // class
} // namespace
