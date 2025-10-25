using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using System.Text;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Serilog;


namespace BedBrigade.Client.Components
{
    //TODO:  This should go in the backend, not on the client side.
    public static class SignUpHelper
    {


        public static async Task<List<Volunteer>> GetVolunteers(IVolunteerDataService svcVolunteer, int locationId)
        {
            var dataVolunteer = await svcVolunteer.GetAllForLocationAsync(locationId); // get Schedules           
            if (dataVolunteer.Success && dataVolunteer.Data != null)
            {
                return dataVolunteer.Data;
            }
            else
            {
                Log.Error(dataVolunteer.Message);
            }

            return new List<Volunteer>();
        }// Get Volunteers

        public static async Task<List<Schedule>> GetSchedules (IScheduleDataService? svcSchedule, bool isLocationAdmin, int userLocationId)
        {
            var dataSchedules = await svcSchedule.GetFutureSchedulesByLocationId(userLocationId);
            if (dataSchedules.Success && dataSchedules.Data != null) // 
            {
                return dataSchedules.Data;
            }
            else
            {
                Log.Error(dataSchedules.Message);
            }
            return new List<Schedule>();

        } // Schedules

        public static async Task<List<SignUp>?> GetSignUps(ISignUpDataService svcSignUp, bool isLocationAdmin, int userLocationId)
        {

            var dataEvents = await svcSignUp.GetAllForLocationAsync(userLocationId);
            if (dataEvents.Success && dataEvents.Data != null) // 
            {
                return dataEvents.Data;
            }
            else
            {
                Log.Error(dataEvents.Message);
            }
            return new List<SignUp>();
        }


        public static MarkupString GetSignUpDataStatusMessage(List<string> lstEmptyTables)
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

        } 

        public static List<Volunteer> CombineAllData(ITimezoneDataService timezoneDataService, 
            List<SignUp>? signUps = null, 
            List<Schedule>? schedules = null, 
            List<Volunteer>? volunteers = null, 
            List<Location>? locations = null)
        {
            List<Volunteer> eventVolunteers = new List<Volunteer>();
            List<Volunteer> registeredVolunteers = LoadRegisteredVolunteers(timezoneDataService, signUps, schedules, volunteers, locations);
            List<Volunteer> eventsWithoutVolunteers = EventsWithoutVolunteers(signUps, schedules, volunteers, locations, registeredVolunteers);
            eventVolunteers.AddRange(registeredVolunteers);
            eventVolunteers.AddRange(eventsWithoutVolunteers);
            return eventVolunteers;
        }

        private static List<Volunteer> EventsWithoutVolunteers(List<SignUp>? signUps, List<Schedule>? schedules,
            List<Volunteer>? volunteers, List<Location>? locations, List<Volunteer> registeredVolunteers)
        {
            List<Volunteer> result = new List<Volunteer>();
            if (schedules != null && schedules.Count > 0)
            {
                result.AddRange(
                    (from e in schedules
                        where !(from ev in registeredVolunteers
                                select ev.ScheduleId).Contains(e.ScheduleId)
                        join l in locations on e.LocationId equals l.LocationId
                        select new Volunteer
                        {
                            ScheduleId = e.ScheduleId,
                            ScheduleLocationId = e.LocationId,
                            ScheduleLocationName = l.Name,
                            ScheduleEventName = e.EventName,
                            ScheduleEventDate = e.EventDateScheduled,
                            ScheduleEventType = e.EventType,
                            NumberOfVolunteers = 0,
                            VehicleType = VehicleType.None,
                            FirstName = "None",
                            LastName = string.Empty
                        }
                    ).ToList()
                );
            }

            return result;
        }

        private static List<Volunteer> LoadRegisteredVolunteers(ITimezoneDataService timezoneDataService, List<SignUp>? signUps, 
            List<Schedule>? schedules, 
            List<Volunteer>? volunteers, 
            List<Location>? locations)
        {
            string timeZone = timezoneDataService.GetUserTimeZoneId();
            List<Volunteer> result = new List<Volunteer>();

            if (signUps != null && volunteers != null && signUps.Count > 0)
            {

                result = (from ve in signUps
                        join e in schedules on ve.ScheduleId equals e.ScheduleId
                        join v in volunteers on ve.VolunteerId equals v.VolunteerId
                        join l in locations on e.LocationId equals l.LocationId
                        select new Volunteer
                        {
                            SignUpId = ve.SignUpId,
                            VolunteerId = ve.VolunteerId,
                            ScheduleId = e.ScheduleId,
                            // Event Fields
                            ScheduleLocationId = ve.LocationId,
                            ScheduleLocationName = l.Name,
                            ScheduleEventName = e.EventName,
                            ScheduleEventDate = e.EventDateScheduled,
                            ScheduleEventType = e.EventType,
                            // Volunteer Fields
                            IHaveVolunteeredBefore = v.IHaveVolunteeredBefore,
                            FirstName = v.FirstName,
                            LastName = v.LastName,
                            Phone = v.Phone,
                            Email = v.Email,
                            Organization = v.Organization,
                            Message = ve.SignUpNote,
                            VehicleType = ve.VehicleType,
                            CreateDateLocal = timezoneDataService.ConvertUtcToTimeZone(ve.CreateDate, timeZone),
                            NumberOfVolunteers = ve.NumberOfVolunteers,
                        }
                    ).ToList();
            }

            return result;
        }
        // Create Grid Data Source

        public static Volunteer PrepareVolunteerDeleteDialog(Volunteer selectedGridObject, ref string strMessageText, ref string dialogTitle, ref string displayDeleteButton, ref string closeButtonCaption)
        {
            var newVolunteer = new Volunteer();
            dialogTitle = "Delete Volunteer from Event";
            displayDeleteButton = ""; // show OK button
            closeButtonCaption = "Cancel";
            strMessageText = "Selected Volunteer will be deleted from Event";
            strMessageText += "</br>Do you still want to delete this Volunteer?";
            // copy Volunteer Data to Display                        
            newVolunteer.SignUpId = selectedGridObject.SignUpId;
            newVolunteer.VolunteerId = selectedGridObject.VolunteerId;
            newVolunteer.FirstName = selectedGridObject.FirstName;
            newVolunteer.LastName = selectedGridObject.LastName;
            newVolunteer.Phone = selectedGridObject.Phone;
            newVolunteer.Email = selectedGridObject.Email;
            newVolunteer.VehicleType = selectedGridObject.VehicleType;
            return (newVolunteer);

        } // delete dialog

        public static List<Volunteer> GetLocationVolunteersSelector(Volunteer selectedGridObject, List<Volunteer>? lstVolunteerSelector, List<SignUp>? signUps)
        {
            var lstLocationVolunteers = new List<Volunteer>();
            var lstLocVolunteers = lstVolunteerSelector.FindAll(vs => vs.LocationId == selectedGridObject.ScheduleLocationId);
            // Event Linked Volunteers
            if (signUps != null && signUps.Count() > 0)
            {
                var lstEventLinkedVolunteers = signUps.FindAll(ve => ve.ScheduleId == selectedGridObject.ScheduleId);
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



    } // class
} // namespace
