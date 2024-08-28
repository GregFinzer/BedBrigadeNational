using System.ComponentModel.DataAnnotations;

namespace BedBrigade.Common.Logic
{
    public class CustomEmailValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var email = value as string;
            var result = Validation.IsValidEmail(email);

            if (!result.IsValid)
            {
                return new ValidationResult(result.UserMessage);
            }

            return ValidationResult.Success;
        }
    }
}
