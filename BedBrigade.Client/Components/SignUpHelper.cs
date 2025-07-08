using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using System.Text;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;

namespace BedBrigade.Client.Components
{
    //TODO:  This should go in the backend, not on the client side.
    public static class SignUpHelper
    {


        public static async Task<List<Volunteer>> GetVolunteers(IVolunteerDataService svcVolunteer)
        {
            var dataVolunteer = await svcVolunteer.GetAllAsync(); // get Schedules           
            if (dataVolunteer.Success && dataVolunteer.Data != null)
            {
                if (dataVolunteer.Data.Count > 0)
                {
                    return(dataVolunteer.Data.ToList()); 
                } 
            }

            return (null);
        }// Get Volunteers

        public static async Task<List<Schedule>> GetSchedules (IScheduleDataService? svcSchedule, bool isLocationAdmin, int userLocationId)
        {
            var dataSchedules = await svcSchedule.GetAllAsync();
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

        public static async Task<List<SignUp>?> GetSignUps(ISignUpDataService svcSignUp, bool isLocationAdmin,
            int userLocationId)
        {

            var dataEvents = await svcSignUp.GetAllAsync();
            if (dataEvents.Success && dataEvents.Data != null) // 
            {
                var signUps = dataEvents.Data.ToList();

                if (isLocationAdmin)
                {
                    signUps = signUps.FindAll(e => e.LocationId == userLocationId);
                }

                return signUps;
            }

            return null;
        }

        public static async Task<List<string>> GetSignUpDataStatusAsync(IScheduleDataService? svcSchedule, IVolunteerDataService? _svcVolunteer)
        {
            var bTableStatus = false;
            var lstEmptyTables = new List<string>();
            // Schedules
            var dataTable = await svcSchedule.GetAllAsync();            
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

        public static List<Volunteer> CombineAllData(List<SignUp>? signUps = null, List<Schedule>? schedules = null, List<Volunteer>? volunteers = null, List<Location>? locations = null)
        {
            List<Volunteer> eventVolunteers = new List<Volunteer>();
            List<Volunteer> registeredVolunteers = LoadRegisteredVolunteers(signUps, schedules, volunteers, locations);
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

        private static List<Volunteer> LoadRegisteredVolunteers(List<SignUp>? signUps, List<Schedule>? schedules, List<Volunteer>? volunteers, List<Location>? locations)
        {
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
                            OrganizationOrGroup = v.OrganizationOrGroup,
                            Message = ve.SignUpNote,
                            VehicleType = ve.VehicleType,
                            CreateDate = ve.CreateDate,
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

        public static async Task<bool>  UpdateSchedule(IScheduleDataService? svcSchedule, 
            List<Schedule> schedules, string strAction, int intScheduleId, VehicleType carType, int numberOfVolunteers)
        {
            var bScheduleUpdated = false;
            var bUpdate = true;
            var mySchedule = schedules.FirstOrDefault(s => s.ScheduleId == intScheduleId);
            switch (strAction)
            {
                case "Add":
                    if (mySchedule != null) // update 
                    {
                        mySchedule.VolunteersRegistered+= numberOfVolunteers;
                        if (carType != VehicleType.None)
                        {
                            mySchedule.DeliveryVehiclesRegistered++;
                        }
                    }
                    break;
                case "Del":
                    if (mySchedule != null) // update 
                    {
                        if (mySchedule.VolunteersRegistered > 0)
                        {
                            mySchedule.VolunteersRegistered-= numberOfVolunteers;
                        }
                        if (carType != VehicleType.None && mySchedule.DeliveryVehiclesRegistered > 0)
                        {
                            mySchedule.DeliveryVehiclesRegistered--;
                        }
                    }
                    break;
                default:
                    bUpdate = false;
                    // Do Nothing
                    break;
            } // switch action
            // update Schedule Table Record                 
            if (bUpdate && mySchedule!=null && svcSchedule != null)
            {
                var dataUpdate = await svcSchedule.UpdateAsync(mySchedule);

                if (dataUpdate.Success)
                {
                    bScheduleUpdated = true;
                }                  
            }

            return (bScheduleUpdated);

        } // update Schedule

    } // class
} // namespace
