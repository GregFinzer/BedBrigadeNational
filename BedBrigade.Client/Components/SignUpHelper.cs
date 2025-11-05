using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components;
using System.Text;
using BedBrigade.Common.Enums;
using BedBrigade.Common.Models;
using Serilog;

namespace BedBrigade.Client.Components
{
    public static class SignUpHelper
    {
        public static SignUpDisplayItem PrepareVolunteerDeleteDialog(SignUpDisplayItem selectedGridObject, ref string strMessageText, ref string dialogTitle, ref string displayDeleteButton, ref string closeButtonCaption)
        {
            var signUp = new SignUpDisplayItem();
            dialogTitle = "Remove Signup for Volunteer";
            displayDeleteButton = ""; // show OK button
            closeButtonCaption = "Cancel";
            strMessageText = "Selected Volunteer Signup will be removed from the Event";
            strMessageText += "</br>Do you still want to remove the signup?";
            // copy Volunteer Data to Display                        
            signUp.SignUpId = selectedGridObject.SignUpId;
            signUp.VolunteerId = selectedGridObject.VolunteerId;
            signUp.VolunteerFirstName = selectedGridObject.VolunteerFirstName;
            signUp.VolunteerLastName = selectedGridObject.VolunteerLastName;
            signUp.VolunteerPhone = selectedGridObject.VolunteerPhone;
            signUp.VolunteerEmail = selectedGridObject.VolunteerEmail;
            signUp.VehicleType = selectedGridObject.VehicleType;
            return (signUp);

        } 
    } 
} 
