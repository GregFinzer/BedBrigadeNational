using AKSoftware.Localization.MultiLanguages;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BedBrigade.Common.Logic;
using BedBrigade.Common.Models;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace BedBrigade.SpeakIt
{
    public class TranslateLogic : ITranslateLogic
    {
        private static ParseLogic _parseLogic = new ParseLogic();
        public static string ResourceName = "Resources.en-US.yml"; 
        private static Dictionary<string, string>? _keyValuePairs;
        private static Dictionary<string, string>? _valueKeyPairs;

        private readonly ILanguageContainerService _lc;

        public TranslateLogic(ILanguageContainerService languageContainerService)
        {
            _lc = languageContainerService;
        }

        public string? ParseAndTranslateText(string input, string targetCulture,
            Dictionary<string, List<Translation>> translations)
        {
            var parseResults = _parseLogic.GetLocalizableStringsInText(input);
            parseResults = parseResults.OrderByDescending(o => o.LocalizableString.Length).ToList();

            foreach (var parseResult in parseResults)
            {
                if (parseResult.LocalizableString.Contains("We are compelled"))
                    Console.WriteLine("here");

                string hash = ComputeSHA512Hash(parseResult.LocalizableString);

                if (!translations.ContainsKey(hash))
                    continue;

                var source = translations[hash].FirstOrDefault(o =>
                    o.Culture == "en-US" && StringUtil.CleanUpSpacesAndLineFeeds(o.Content) == StringUtil.CleanUpSpacesAndLineFeeds(parseResult.LocalizableString));

                if (source == null)
                    continue;

                var target = translations[hash].FirstOrDefault(o => o.Culture == targetCulture && o.ParentId == source.TranslationId);

                if (target != null)
                {
                    input = IntelligentReplace(input, parseResult.LocalizableString, target.Content);
                }
            }

            return input;
        }

        public string IntelligentReplace(string input, string search, string replace)
        {
            //Basically we don't want to replace the search string if it is part of a URL, or an HTML attribute
            string pattern = $"(?<leftChar>[^\\/\"'])(?<content>{Regex.Escape(search)})(?<rightChar>[^\\/\"'])";
            Regex regex = new Regex(pattern, RegexOptions.Multiline);

            MatchCollection matches = regex.Matches(input);

            foreach (Match match in matches)
            {
                string leftChar = match.Groups["leftChar"].Value;
                string rightChar = match.Groups["rightChar"].Value;
                string newSearch = leftChar + search + rightChar;
                string newReplace = leftChar + replace + rightChar;
                input = input.Replace(newSearch, newReplace);
            }

            return input;
        }

        public string ComputeSHA512Hash(string input)
        {
            using (SHA512 sha512 = SHA512.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha512.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        public Dictionary<string, List<Translation>> TranslationsToDictionary(List<Translation> translations)
        {
            Dictionary<string, List<Translation>> translationsDictionary = new Dictionary<string, List<Translation>>();

            foreach (var translation in translations)
            {
                if (!translationsDictionary.ContainsKey(translation.Hash))
                {
                    translationsDictionary.Add(translation.Hash, new List<Translation>());
                }

                translationsDictionary[translation.Hash].Add(translation);
            }

            return translationsDictionary;

        }


        public string? GetTranslation(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            Initialize();

            //Try and translate the whole value
            string? key = GetKeyForValue(value);

            if (key != null)
            {
                return _lc.Keys[key];
            }

            //If the whole value is not found, try to translate each word
            string[] words = value.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            StringBuilder sb = new StringBuilder(value.Length);
            foreach (string word in words)
            {
                string? translatedWord = GetKeyForValue(word);
                if (translatedWord != null)
                {
                    sb.Append(_lc.Keys[translatedWord]);
                    sb.Append(" ");
                }
                else
                {
                    sb.Append(word);
                    sb.Append(" ");
                }
            }

            return value;
        }

        public string? GetKeyForValue(string value)
        {
            Initialize();

            if (_valueKeyPairs.ContainsKey(value))
            {
                return _valueKeyPairs[value];
            }

            return null;
        }

        private void Initialize()
        {
            
            if (_keyValuePairs != null && _valueKeyPairs != null)
            {
                return;
            }

            var resource = FindResourceInAssemblies(ResourceName);

            if (resource == null)
            {
                Console.WriteLine("Resource not found.");
                return;
            }

            // Get the resource stream
            using (Stream resourceStream = resource.Value.assembly.GetManifestResourceStream(resource.Value.resourceName))
            {
                if (resourceStream == null)
                {
                    Console.WriteLine("Resource not found.");
                    return;
                }

                // Read the content from the resource stream
                using (StreamReader reader = new StreamReader(resourceStream))
                {
                    var yamlContent = reader.ReadToEnd();

                    // Deserialize the Yaml into a dictionary
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(CamelCaseNamingConvention.Instance) // Adjust naming convention as needed
                        .Build();

                    // Assuming the YML structure is a dictionary (key-value pairs)
                    _keyValuePairs = deserializer.Deserialize<Dictionary<string, string>>(yamlContent);
                    _valueKeyPairs = new Dictionary<string, string>();

                    // Output or use the parsed data
                    foreach (var item in _keyValuePairs)
                    {
                        if (!_valueKeyPairs.ContainsKey(item.Value))
                        {
                            _valueKeyPairs.Add(item.Value, item.Key);
                        }
                    }
                }
            }
        }

        public static (Assembly assembly, string resourceName)? FindResourceInAssemblies(string resourceSuffix)
        {
            // Get all currently loaded assemblies
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            // Loop through each assembly
            foreach (var assembly in loadedAssemblies)
            {
                // Get the resource names in the current assembly
                var resourceNames = assembly.GetManifestResourceNames();

                // Find the first resource that ends with the specified suffix
                var matchingResource = resourceNames.FirstOrDefault(name => name.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));

                if (matchingResource != null)
                {
                    // Return the assembly and the matching resource as a tuple
                    return (assembly, matchingResource);
                }
            }

            // If no matching resource is found, return null
            return null;
        }
    }
}
