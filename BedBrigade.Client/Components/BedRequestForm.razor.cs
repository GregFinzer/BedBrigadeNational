using Syncfusion.Blazor;
using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.DropDowns;
using System;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Diagnostics;
using ChangeEventArgs = Microsoft.AspNetCore.Components.ChangeEventArgs;
using BedBrigade.Data.Models;
using static BedBrigade.Common.Common;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components.Web;
using BedBrigade.Common;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using Microsoft.AspNetCore.Components.Forms;

namespace BedBrigade.Client.Components
{
    public partial class BedRequestForm : ComponentBase
    {
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IBedRequestDataService? _svcBedRequest { get; set; }
        private BedBrigade.Data.Models.BedRequest? newRequest;
        private List<UsState>? StateList = GetStateList();
        private string PostalCode { get; set; } = string.Empty;
        private string PostalCodeError { get; set; } = string.Empty;
        private string PostalCodeSuccess { get; set; } = string.Empty;
        private List<LocationDistance> Locations { get; set; } = new List<LocationDistance>();
        private LocationDistance? selectedLocation { get; set; }
        private SearchLocation? SearchLocation;             

        private const string DisplayNone = "none";
        private const string AlertDanger = "alert alert-danger";

        private string DisplayForm = DisplayNone;
        private string DisplayReCaptcha = DisplayNone;
        private string DisplaySubmit = DisplayNone;
        private string DisplaySearch = "";
        public int NumericValue { get; set; } = 1;

        private string ResultMessage = string.Empty;
        private string NotificationMessage = "Attention! The most of fields in this form are required!";
        private string NotificationStatus = "alert alert-info";
        private string SubmitAlertMessage = string.Empty;
        private string AlertDisplay = DisplayNone;
        private string ResultDisplay = DisplayNone;
        private string AlertType = AlertDanger;
        public string userZipCode = String.Empty;
        public string userCity = String.Empty;
        public string userState = String.Empty;

        private ReCAPTCHA? reCAPTCHAComponent;
        private bool ValidReCAPTCHA = false;
        private bool ServerVerificatiing = false;
        private bool EditFormStatus = false;

        private bool DisableSubmitButton => !ValidReCAPTCHA || ServerVerificatiing;
        private EditContext? EC { get; set; }


        private string cssClass { get; set; } = "e-outline";
        protected Dictionary<string, object> DescriptionHtmlAttribute { get; set; } = new Dictionary<string, object>()
        {
            { "rows", "4" },
        };

        protected Dictionary<string, object> DropDownHtmlAttribute = new Dictionary<string, object>()
        {
           { "font-weight", "bold" },
        };

        protected Dictionary<string, object> htmlattributeSize = new Dictionary<string, object>()
        {
           { "maxlength", "2" },
        };



        protected override void OnInitialized()
        {
            newRequest = new BedBrigade.Data.Models.BedRequest();
            EC = new EditContext(newRequest);
            base.OnInitialized();
           
        }

        private void RunValidation()
        {
            string DetailMessage = String.Empty;

           bool bFormValid =  EC.Validate(); // manually trigger the validation here
                                             // 
            if (newRequest.City != Validation.GetCityForZipCode(newRequest.PostalCode) || newRequest.State != GetZipState(StateList, Convert.ToInt32(userZipCode)))
            {
                DetailMessage = "It looks like your city, state, and ZIP code didn't match. Check them and try again.";
                bFormValid = false;
            }


            // Custom Validation - Email

            if (newRequest.Email != null && newRequest.Email.Trim().Length > 0)
            {
                KellermanSoftware.NetEmailValidation.Result emailvalid = Validation.IsValidEmail(newRequest.Email);

                if (!emailvalid.IsValid)
                {
                    // test message 
                    if (DetailMessage.Length > 0)
                    {
                        DetailMessage = DetailMessage  + "<br />" + emailvalid.UserMessage;
                    }
                    else
                    {
                        DetailMessage =  emailvalid.UserMessage;
                    }
                    bFormValid = false;
                }
            }


            if (bFormValid)
            {
                 NotificationMessage = "Success! The Form is completed. You can continue with reCaptcha.";
                 NotificationStatus = "alert alert-success";
                 DisplayReCaptcha = "";
                EditFormStatus = true;
            }
            else
            {
                NotificationMessage = "Warning! This Form was not completed. Please fill out all required fields.";
                if(DetailMessage.Length > 0)
                {
                    NotificationMessage = NotificationMessage + "<br />" + DetailMessage;
                }
                NotificationStatus = "alert alert-warning";
                EditFormStatus = false;
            }
        }


        private void OnSuccess()
        {
            //ResultMessage = "Secret Verification OK";
            //ResultDisplay = "";
            ValidReCAPTCHA = true;
            DisplaySubmit = "";
    }

        private void OnExpired()
        {
            ValidReCAPTCHA = false;
        }


        private void CheckChildData(string SearchZipCode) 
        { // usually data is zip code
            if (SearchZipCode != null && SearchZipCode.Trim().Length == 5)
            {                
                DisplayForm = "";
                
                userZipCode = SearchZipCode;
                userCity= Validation.GetCityForZipCode(SearchZipCode); 
                userState = GetZipState(StateList, Convert.ToInt32(SearchZipCode));
                
                newRequest.PostalCode = userZipCode;
                newRequest.State = userState;
                newRequest.City = userCity; 

            }
        } // check child component data

        private void ZipCodeChangedHandler(String args)
        {
            var newZipCode = args;
            if (newZipCode != null && newZipCode.Length==5)
            { // update city & state
                userZipCode = newZipCode;
                newRequest.City = Validation.GetCityForZipCode(newZipCode);
                newRequest.State = GetZipState(StateList, Convert.ToInt32(newZipCode));
            }
            RunValidation();
        }

        private async Task SaveRequest()
        {
            if (EditFormStatus)
            {
                bool bValidData = true;
                SubmitAlertMessage = String.Empty;

                newRequest.LocationId = SearchLocation.ddlValue; // get value from child component
                newRequest.NumberOfBeds = NumericValue;
                newRequest.PostalCode = userZipCode;
                //strSubmitAlertMessage = newRequest.SpecialInstructions;
                //var jsonString = JsonSerializer.Serialize(newRequest);
                //Debug.Write(jsonString);
                // always override city by Zip Code


                // Custom Validation - Address                    

                if (!bValidData)
                {

                    AlertType = AlertDanger;
                    AlertDisplay = "";
                    return;
                }

                try
                {  // restore the right city & state - to avoid any errors
                    newRequest.PostalCode = userZipCode;
                    newRequest.City = Validation.GetCityForZipCode(newRequest.PostalCode);
                    var addResult = await _svcBedRequest.CreateAsync(newRequest);
                    if (addResult.Success && addResult.Data != null)
                    {
                        newRequest = addResult.Data; // added Request
                    }

                    if (newRequest != null && newRequest.BedRequestId > 0)
                    {
                        AlertType = "alert alert-success";
                        DisplaySearch = DisplayNone;
                        DisplayForm = DisplayNone;
                       // ResultMessage = "New Bed Request #" + newRequest.BedRequestId.ToString() + " created Successfully!<br />";
                        ResultMessage += "We have received your request (registration #"+ newRequest.BedRequestId.ToString()+") and would like to thank you for writing to us.<br />";
                        ResultMessage += "We will look over your request and reply by email as soon as possible.<br />";
                        ResultMessage += "Talk to you soon, Bed Brigade.";
                        ResultDisplay = "";
                    }
                    else
                    {
                        SubmitAlertMessage = "Warning! Unable to add new Bed Request!";
                        AlertType = AlertDanger;
                        AlertDisplay = "";
                    }
                }
                catch (Exception ex)
                {
                    AlertType = AlertDanger;
                    SubmitAlertMessage = "Error! " + ex.Message;
                    AlertDisplay = "";
                }

            } // Edit Form Status

        } // Save Request    

        public static bool ValidEmailDataAnnotations(string input)
        {
            return new EmailAddressAttribute().IsValid(input);
        }




    } // page class
} // namespace
