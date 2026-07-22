using KellermanSoftware.NetEmailValidation;

namespace BedBrigade.Common.Logic
{
    public static class Validation
    {
        public const string PhoneRegexPattern = "\\(?[2-9]\\d{2}\\)?\\s?-?\\d{3}-?\\d{4}";
        public const string EmailRegexPattern =
            "^(\\D)+(\\w)*((\\.(\\w)+)?)+@(\\D)+(\\w)*((\\.(\\D)+(\\w)*)+)?(\\.)[a-z]{2,}$";

        public const string PasswordRegexPattern =
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$";

        public static bool IsValidUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                   && (uri.Scheme == Uri.UriSchemeHttp ||
                       uri.Scheme == Uri.UriSchemeHttps);
        }

        public static Result IsValidEmail(string? email)
        {
            var emailValidation = LibraryFactory.EmailValidation;
            return emailValidation.ValidEmail(email, emailValidation.NoConnectOptions);
        }

        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            return PhoneNumberValidator.IsValidPhoneNumber(phoneNumber);
        }

        public static bool IsValidZipCode(string? zipCode)
        {
            var addressParser = LibraryFactory.AddressParser;
            return addressParser.IsValidZipCode(zipCode);
        }

        public static List<string> GetCitiesForZipCode(string zipCode)
        {
            if (!IsValidZipCode(zipCode))
                return new List<string>();

            var addressParser = LibraryFactory.AddressParser;
            var info = addressParser.GetInfoForZipCode(zipCode);
            List<string> cities = new List<string>();
            cities.Add(info.PrimaryCity);
            cities.AddRange(info.AcceptableCities);
            return cities;
        }

        public static string GetStateForZipCode(string zipCode)
        {
            if (!IsValidZipCode(zipCode))
                return null;

            var addressParser = LibraryFactory.AddressParser;
            var info = addressParser.GetInfoForZipCode(zipCode);
            return info.State;
        }

        public static string GetCityForZipCode(string zipCode)
        {
            if (!IsValidZipCode(zipCode))
                return null;

            var addressParser = LibraryFactory.AddressParser;
            return addressParser.GetInfoForZipCode(zipCode).PrimaryCity;
        }

        public static List<string> ValidateWithDataAnnotations<T>(T model)
            where T : class
        {
            List<string> errors = new List<string>();
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(model);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            bool isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                model, validationContext, validationResults, true);
            if (!isValid)
            {
                foreach (var validationResult in validationResults)
                {
                    errors.Add(validationResult.ErrorMessage);
                }
            }

            return errors;
        }
    }
}
