using System.ComponentModel.DataAnnotations;
using System.Reflection;
using AKSoftware.Localization.MultiLanguages;
using Microsoft.AspNetCore.Components.Forms;

namespace BedBrigade.SpeakIt
{
    public static class ValidationLocalization
    {
        public static string RequiredPrefix { get; set; } = "Required";
        public static string MaxLengthPrefix { get; set; } = "MaxLength";

        public static bool ValidateModel<T>(T model, ValidationMessageStore validationMessageStore, ILanguageContainerService lc)  where T : class, new()
        {
            bool valid = true;
            var type = model.GetType();
            var properties = type.GetProperties();

            foreach (var property in properties)
            {
                var attributes = property.GetCustomAttributes(true);

                foreach (var attribute in attributes)
                {
                    if (attribute is RequiredAttribute requiredAttribute)
                    {
                        if (!ValidateRequired(model, validationMessageStore, lc, property, requiredAttribute))
                            valid = false;
                    }
                    else if (attribute is MaxLengthAttribute maxLengthAttribute)
                    {
                        if (!ValidateMaxLength(model, validationMessageStore, lc, property, maxLengthAttribute))
                            valid = false;
                    }
                }
            }

            return valid;
        }

        private static bool ValidateMaxLength<T>(T model, ValidationMessageStore validationMessageStore, ILanguageContainerService lc, PropertyInfo property, MaxLengthAttribute maxLengthAttribute) where T : class, new()
        {
            if (property.PropertyType != typeof(string))
                return true;

            if (property.GetValue(model) != null && property.GetValue(model).ToString().Length > maxLengthAttribute.Length)
            {
                string languageKey = $"{MaxLengthPrefix}{property.Name}{maxLengthAttribute.Length}";
                string translation = lc.Keys[languageKey];

                if (!string.IsNullOrEmpty(translation))
                {
                    validationMessageStore.Add(new FieldIdentifier(model, property.Name), lc.Keys[languageKey]);
                }
                else if (maxLengthAttribute.ErrorMessage != null)
                {
                    validationMessageStore.Add(new FieldIdentifier(model, property.Name), maxLengthAttribute.ErrorMessage);
                }
                else
                {
                    validationMessageStore.Add(new FieldIdentifier(model, property.Name), $"{property.Name} should not exceed {maxLengthAttribute.Length} characters");
                }

                return false;
            }

            return true;
        }

        private static bool ValidateRequired<T>(T model, ValidationMessageStore validationMessageStore,
            ILanguageContainerService lc, PropertyInfo property, RequiredAttribute requiredAttribute) where T : class, new()
        {
            if (property.PropertyType != typeof(string))
                return true;

            if (property.GetValue(model) == null 
                ||  string.IsNullOrWhiteSpace(property.GetValue(model).ToString()))
            {
                string languageKey = $"{RequiredPrefix}{property.Name}";
                string translation = lc.Keys[languageKey];

                if (!string.IsNullOrEmpty(translation))
                {
                    validationMessageStore.Add(new FieldIdentifier(model, property.Name), lc.Keys[languageKey]);
                }
                else if (requiredAttribute.ErrorMessage != null)
                {
                    validationMessageStore.Add(new FieldIdentifier(model, property.Name), requiredAttribute.ErrorMessage);
                }
                else
                {
                    validationMessageStore.Add(new FieldIdentifier(model, property.Name), $"{property.Name} is required");
                }

                return false;
            }

            return true;
        }
    }
}
