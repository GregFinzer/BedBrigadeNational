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
using Microsoft.AspNetCore.Components.Forms;

namespace BedBrigade.Client.Components
{
    public partial class BedRequestForm : ComponentBase
    {

        #region Declaration

        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IBedRequestDataService? _svcBedRequest { get; set; }
        private BedBrigade.Data.Models.BedRequest? newRequest;
        private List<UsState>? StateList = GetStateList();
       
        private List<LocationDistance> Locations { get; set; } = new List<LocationDistance>();
        private LocationDistance? selectedLocation { get; set; }
        private SearchLocation? SearchLocation;             

        private const string DisplayNone = "none";
        private const string AlertDanger = "alert alert-danger";
        private const string FormMessage = "Please fill out all the mandatory fields marked with an asterisk (*).<br />We recommend to use <b>[Tab]</b> key to move between fields.";

        private string DisplayForm = DisplayNone;
        private string DisplayReCaptcha = DisplayNone;
        private string DisplaySubmit = DisplayNone;
        private string DisplaySearch = "";
        public int NumericValue { get; set; } = 1;

        private string ResultMessage = string.Empty;
        private string NotificationMessage = FormMessage;
        private string NotificationStatus = "alert alert-warning";
        private string SubmitAlertMessage = string.Empty;
        private string AlertDisplay = DisplayNone;
        private string ResultDisplay = DisplayNone;
        private string AlertType = AlertDanger;
    
        private ReCAPTCHA? reCAPTCHAComponent;
        private bool ValidReCAPTCHA = false;
        private bool ServerVerificatiing = false;
        private bool EditFormStatus = false; // true if not errors
             
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

        #endregion
        #region Initialization

        protected override void OnInitialized()
        {
            newRequest = new BedBrigade.Data.Models.BedRequest();
            EC = new EditContext(newRequest);
            //messageStore = new ValidationMessageStore(EC);
            base.OnInitialized();            
        }

        private void CheckChildData(string SearchZipCode)  // from Search Location Component
        { // usually data is zip code
            if (SearchZipCode != null && SearchZipCode.Trim().Length == 5)
            {
                DisplayForm = "";
                // the following changes run address validations!               
                newRequest.City = Validation.GetCityForZipCode(SearchZipCode);
                newRequest.State = GetZipState(StateList, Convert.ToInt32(SearchZipCode));
                newRequest.PostalCode = SearchZipCode;
                           
            }
        } // check child component data              

        #endregion

        #region Validation & Events

        private void RunValidation()
        {
           string AttachedMessage = String.Empty;
           bool isAddressCorrect = ValidationAddress();
            EditFormStatus =  EC.Validate(); // manually trigger the validation here

            if (!isAddressCorrect)
            {
                EditFormStatus = false;
                AttachedMessage = "It looks like entered city, state, and ZIP code didn't match your requested location. Check them and try again.";
            }                      
          

            if (EditFormStatus)
            {
                SetNotificationMessage("success"); //= "Success! The Form is completed. You can continue with reCaptcha.";
                 //NotificationStatus = "alert alert-success";
                 DisplayReCaptcha = "";              
            }
            else
            {                
                NotificationStatus = "warning";
                SetNotificationMessage("warning", "", AttachedMessage);
                DisplayReCaptcha = DisplayNone;               
            }
        } // Run Validation

        private bool ValidationAddress() 
        {  // Check Combination of State, City, ZipCode - only if all fields are populated                       
          
                if (!string.IsNullOrEmpty(newRequest.PostalCode) && !string.IsNullOrEmpty(newRequest.City))
                {
                    try
                    {
                        // User entered City & Zip
                        string zipCity = Validation.GetCityForZipCode(newRequest.PostalCode);
                        if (string.IsNullOrEmpty(zipCity) || zipCity.Contains("Error")){
                            return false;
                        }

                        string zipState = GetZipState(StateList, Convert.ToInt32(newRequest.PostalCode));
                                                         
                                if (zipState != null && zipCity.ToLower() == newRequest.City.ToLower() && zipState == newRequest.State)
                                {                                  
                                    return true;
                                } 
                                else
                                { return false; }
                        
                    } 
                    catch (Exception ex) 
                    {
                        // possible validation.cs error
                        return false;
                    }
                }  // entered data found                  

            return true;
            
        } // Validation Address

        private void SetNotificationMessage(string MessageType, string MessageText = "", string AttachedMessage = "")
        {  // Clear Notification Message First
            NotificationMessage = "&nbsp;";
            NotificationStatus = "alert alert-" + MessageType;
            if (MessageType == "success")
            {
                NotificationMessage = "Success! The Form is completed. You can continue with reCaptcha.";
                DisplayReCaptcha = "";
            }
            else
            {
                NotificationMessage = FormMessage;
                DisplayReCaptcha = DisplayNone;

                if (MessageText.Length > 0)
                {
                    NotificationMessage = NotificationMessage + "<br/>" + MessageText;
                }
                if (AttachedMessage.Length > 0)
                {
                    NotificationMessage = NotificationMessage + "<br/>" + AttachedMessage;
                }

                Debug.WriteLine(NotificationMessage);
            }

        } // Notification

        #endregion
        #region reCaptcha


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

        #endregion
        #region SaveRequest

        private async Task SaveRequest()
        {
            RunValidation();

            if (EditFormStatus && ValidReCAPTCHA)
            {
                bool bValidData = true;
                SubmitAlertMessage = String.Empty;

                newRequest.LocationId = SearchLocation.ddlValue; // get value from child component
                newRequest.NumberOfBeds = NumericValue;
                newRequest.Phone = newRequest.Phone.FormatPhoneNumber();
                newRequest.SetCreateUser(Constants.GuestUser);
                newRequest.SetUpdateUser(Constants.GuestUser);
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

                await UpdateDatabase();
             

            } // Edit Form Status
            else
            {
                SetNotificationMessage("warning");
                DisplaySubmit = DisplayNone;
                DisplayReCaptcha = DisplayNone;
            }

        } // Save Request
          
        private async Task UpdateDatabase()
        {
            try
            {  // restore the right city & state - to avoid any errors

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
                    ResultMessage += "We have received your request (registration #" + newRequest.BedRequestId.ToString() + ") and would like to thank you for writing to us.<br />";
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
        } // update database

        #endregion        
         

    } // page class
} // namespace
