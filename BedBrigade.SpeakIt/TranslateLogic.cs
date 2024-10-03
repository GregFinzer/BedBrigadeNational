using AKSoftware.Localization.MultiLanguages;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace BedBrigade.SpeakIt
{
    public class TranslateLogic : ITranslateLogic
    {
        public static string ResourceName = "Resources.en-US.yml"; 
        private static Dictionary<string, string>? _keyValuePairs;
        private static Dictionary<string, string>? _valueKeyPairs;

        private readonly ILanguageContainerService _lc;

        public TranslateLogic(ILanguageContainerService languageContainerService)
        {
            _lc = languageContainerService;
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
