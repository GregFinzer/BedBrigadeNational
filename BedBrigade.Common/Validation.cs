using KellermanSoftware.NetEmailValidation;
using System.Linq.Expressions;

namespace BedBrigade.Common
{
    public static class Validation
    {
        public static Result IsValidEmail(string email)
        {
            var emailValidation = LibraryFactory.CreateEmailValidation();
            return emailValidation.ValidEmail(email, emailValidation.NoConnectOptions);
        }

        public static bool IsValidCity(string city)
        {
            var addressParser = LibraryFactory.CreateAddressParser();
            return addressParser.IsValidCity(city);
        }

        public static string GetCityForZipCode(string zipCode)
        {
            
            var addressParser = LibraryFactory.CreateAddressParser();
            try
            {
                return addressParser.GetInfoForZipCode(zipCode).PrimaryCity;
            }
            catch(Exception ex)
            {
                return ("Error: " + ex.Message); // VS 8/11/2023
            }
        }
    }
}
