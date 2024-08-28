using System.ComponentModel.DataAnnotations;


namespace BedBrigade.Common.Logic
{
    public class CustomPhoneValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var phone = value as string;
            var result = Validation.IsValidPhoneNumber(phone);

            if (!result)
            {
                return new ValidationResult("A phone number must be exactly 10 digits and have a valid area code and prefix.");
            }

            return ValidationResult.Success;
        }
    }
}
