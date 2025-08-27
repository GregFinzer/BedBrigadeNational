using System.ComponentModel.DataAnnotations;
using BedBrigade.Common.Logic;
using KellermanSoftware.NetEmailValidation;

namespace BedBrigade.Common.Models
{
    internal class EmailInputValidation : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {            
            string StatusMessage = String.Empty;
            string eMailAddress=String.Empty;

            if (value != null && value.ToString().Trim().Length>0)
            {
                eMailAddress = value.ToString().Trim();
                Result emailvalid = Validation.IsValidEmail(eMailAddress);

                if (!emailvalid.IsValid) // eMail not valid
                {
                    StatusMessage = emailvalid.UserMessage;
                    return new ValidationResult(StatusMessage, new[] { validationContext.MemberName });
                }                
            }

            return null;

        }

    }

    internal class PhoneInputValidation : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            string StatusMessage = String.Empty;
            string phone = String.Empty;

            if (value != null && value.ToString().Trim().Length > 0)
            {
                phone = value.ToString().Trim();
                var phoneValid = Validation.IsValidPhoneNumber(phone);

                if (!phoneValid) 
                {
                    StatusMessage = "Invalid Phone Number";
                    return new ValidationResult(StatusMessage, new[] { validationContext.MemberName });
                }
            }

            return null;

        }

    }

}
