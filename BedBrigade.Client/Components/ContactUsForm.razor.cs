using System.Diagnostics;
using BedBrigade.Data.Models;

using Microsoft.AspNetCore.Components;
using BedBrigade.Data.Services;
using Microsoft.AspNetCore.Components.Forms;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Enums;
using BedBrigade.Common.EnumModels;

namespace BedBrigade.Client.Components
{
    public partial class ContactUsForm : ComponentBase
    {

        #region Declaration
       
        [Inject] private ILocationDataService? _svcLocation { get; set; }
        [Inject] private IContactUsDataService? _svcContactUs { get; set; }
        private BedBrigade.Data.Models.ContactUs? newRequest;
        private List<UsState>? StateList = AddressHelper.GetStateList();
       
        private List<LocationDistance> Locations { get; set; } = new List<LocationDistance>();
        private LocationDistance? selectedLocation { get; set; }
        private SearchLocation? SearchLocation;             

        private const string DisplayNone = "none";
        private const string AlertDanger = "alert alert-danger";
        private const string FormMessage = "Please fill out all the mandatory fields marked with an asterisk (*).";
        private const string FormNotCompleted = "The Contact Us Form is not completed!";

        private string DisplayForm = DisplayNone;
        private string DisplayAddressMessage = DisplayNone;      
        private string DisplaySearch = "";
        public int NumericValue { get; set; } = 1;

        private string ResultMessage = string.Empty;
        private string NotificationMessage = FormMessage;
        private string NotificationStatus = "alert alert-warning";
        private string SubmitAlertMessage = string.Empty;
        private string AlertDisplay = DisplayNone;
        private string ResultDisplay = DisplayNone;
        private string NotificationDisplay = String.Empty;
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
            newRequest = new BedBrigade.Data.Models.ContactUs();
            EC = new EditContext(newRequest);
            //messageStore = new ValidationMessageStore(EC);
            base.OnInitialized();            
        }

        

        #endregion

        #region Validation & Events

        private void RunValidation()
        {
            NotificationMessage = String.Empty;
            DisplayAddressMessage = DisplayNone;
            NotificationDisplay = DisplayNone;
            EditFormStatus =  EC.Validate(); // manually trigger the validation here
        } // Run Validation



        private void SetNotificationMessage(string MessageType, string MessageText = "", string AttachedMessage = "")
        {  // Clear Notification Message First
            NotificationMessage = String.Empty;
            NotificationStatus = "alert alert-" + MessageType;
            if (MessageType == "success")
            {
                NotificationMessage = "Success! The Form is completed. You can continue with reCaptcha.";                
            }
            else
            {
                if (MessageText.Length == 0 )
                {
                    NotificationMessage = FormMessage;
                }
                
                if (MessageText.Length > 0 && MessageText!="skip")
                {
                    if (NotificationMessage.Length > 0)
                    {
                        NotificationMessage = NotificationMessage + "<br/>" + MessageText;
                    }
                    else
                    {
                        NotificationMessage = MessageText;
                    }
                }

                if (AttachedMessage.Length > 0)
                {
                    if (NotificationMessage.Length > 0)
                    {
                        NotificationMessage = NotificationMessage + "<br/>" + AttachedMessage;
                    }
                    else
                    {
                        NotificationMessage = AttachedMessage;
                    }
                }

                Debug.WriteLine(NotificationMessage);
            }

            NotificationDisplay = "";

        } // Notification

        private void CheckChildData(string SearchZipCode)  // from Search Location Component
        { 
            DisplayForm = "";
        } 

        #endregion

        #region reCaptcha


        private void OnSuccess()
        {
            ValidReCAPTCHA = true;
            EditFormStatus = EC.Validate();
            if (EditFormStatus)
            {
                NotificationDisplay = DisplayNone;
            }
            else
            {
                SetNotificationMessage("danger", FormNotCompleted);
            }
        } // reCaptcha success

        private void OnExpired()
        {
            ValidReCAPTCHA = false;
        }

        #endregion
        
        #region SaveRequest

        private async Task SaveRequest()
        {
            string formStatusMessage;
            RunValidation();

            if (EditFormStatus && ValidReCAPTCHA) // data are valid
            {
                SetNotificationMessage("success");

                newRequest.LocationId = SearchLocation.ddlValue; // get value from child component

                newRequest.Phone = newRequest.Phone.FormatPhoneNumber();
                newRequest.Status = ContactUsStatus.ContactRequested;
                await UpdateDatabase();

            } // Edit Form Status
            else // not valid data or/and reCaptcha
            {
                var ReCaptchaStatusMessage = String.Empty;

                if (EditFormStatus)
                {
                    formStatusMessage = "The Contact Us Form is completed!";
                    AlertType = "success";
                    if (!ValidReCAPTCHA)
                    {
                        AlertType = "warning";
                        ReCaptchaStatusMessage = "Please check reCAPTCHA!";
                    }
                }
                else
                {
                    formStatusMessage = FormNotCompleted;
                    AlertType = "danger";
                }

                SetNotificationMessage(AlertType, formStatusMessage, ReCaptchaStatusMessage);
            }
        }

        private async Task UpdateDatabase()
        {
            try
            {  

                var addResult = await _svcContactUs.CreateAsync(newRequest);
                if (addResult.Success && addResult.Data != null)
                {
                    newRequest = addResult.Data; // added Request
                }

                if (newRequest != null && newRequest.ContactUsId > 0)
                {
                    AlertType = "alert alert-success";
                    DisplaySearch = DisplayNone;
                    DisplayForm = DisplayNone;
                    // ResultMessage = "New Bed Request #" + newRequest.BedRequestId.ToString() + " created Successfully!<br />";
                    ResultMessage += "We have received your contact request (contact #" + newRequest.ContactUsId.ToString() + ") and would like to thank you for writing to us.<br />";
                    ResultMessage += "We will look over your request and reply by email as soon as possible.<br />";
                    ResultMessage += "Talk to you soon, Bed Brigade.";
                    ResultDisplay = "";
                }
                else
                {
                    SubmitAlertMessage = "Warning! Unable to add new Contact!";
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
         

    } 
} 
