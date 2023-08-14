using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BedBrigade.Common;
using KellermanSoftware.NetEmailValidation;

namespace BedBrigade.Data.Models
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

    } //    

}
