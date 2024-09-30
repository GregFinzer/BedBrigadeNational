using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using BedBrigade.Common.Logic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BedBrigade.SpeakIt
{
    public class SpeakItLogic
    {
        private const string ReplacementMarker = "~~~";

        private static List<Regex> _removePatterns = new List<Regex>()
        {
            // Begin and end tag:  <i class="fa fa-home"></i>
            new Regex(@"<[A-Za-z]+[^>]*>.*<\/[A-Za-z]+>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
            // No end tag, no forward slashes: <ValidationMessage For="@(() => loginModel.Email)" />
            new Regex(@"<[A-Za-z]+[^\/]*\/>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
            // No end tag with forward slashes: <img src="https://www.example.com/image.jpg" alt="Example Image" />
            new Regex(@"<[A-Za-z]+[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
            //Non-breaking space
            new Regex(@"&nbsp;", RegexOptions.Compiled|RegexOptions.IgnoreCase),
            //Razor variable
            new Regex(@"@[A-Za-z0-9\.]+", RegexOptions.Compiled|RegexOptions.IgnoreCase),
            //Begin tag hanging out there
            new Regex(@"<[A-Za-z]+[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
            //End tag hanging out there
            new Regex(@"<\/[A-Za-z]+[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
        };

        private static List<Regex> _contentPatterns = new List<Regex>()
        {
            new Regex(@"<a[^>]*>(?<content>.*?)<\/a>", RegexOptions.Compiled|RegexOptions.IgnoreCase|RegexOptions.Multiline),
            new Regex(@"<button[^<]+>(?<content>.*?)</button>", RegexOptions.Compiled|RegexOptions.IgnoreCase|RegexOptions.Multiline),
            new Regex(@"<label[^<]+>(?<content>\s*[A-Za-z].*?)</label>", RegexOptions.Compiled|RegexOptions.IgnoreCase|RegexOptions.Multiline),
            new Regex(@"<strong>(?<content>.*?)<\/strong>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
            new Regex(@"<i>(?<content>.*?)<\/i>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
            new Regex(@"<PageTitle>(?<content>.*?)<\/PageTitle>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
            new Regex(@"<span[^>]*>(?<content>.*?)<\/span>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
            new Regex(@"<th[^>]*>(?<content>[A-Za-z].*?)<\/th>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
            new Regex(@"<td[^>]*>(?<content>.*?)<\/td>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
            new Regex(@"<h\d[^>]*>(?<content>.*?)<\/h\d>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
            new Regex(@"<li[^>]*>(?<content>.*?)<\/li>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
            new Regex(@"<pre[^>]*>(?<content>[\s\S]+?)<\/pre>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
            new Regex(@"<p[^>]*>(?<content>[\s\S]+?)<\/p>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
            new Regex(@"<div[^>]*>(?<content>\s*[A-Za-z][\s\S]*?)<\/div>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline),
        };

        public List<ParseResult> GetLocalizableStrings(ParseParms parms)
        {
            ValidateParseParameters(parms);

            string[] files =
                Directory.GetFiles(parms.TargetDirectory, parms.WildcardPattern, SearchOption.AllDirectories);

            List<string> filesToProcess = files
                .Where(o => parms.ExcludeFiles.All(a => a != Path.GetFileName(o)))
                .Where(o => parms.ExcludeDirectories.All(a =>
                    !o.Contains(Path.DirectorySeparatorChar + a + Path.DirectorySeparatorChar)))
                .ToList();

            List<ParseResult> result = new List<ParseResult>();

            foreach (var file in filesToProcess)
            {
                if (Path.GetFileName(file) == "Error.razor")
                    Console.WriteLine("Here");

                string content = File.ReadAllText(file);
                var fileResults = GetLocalizableStringsInHtml(content);

                foreach (var fileResult in fileResults)
                {
                    fileResult.FilePath = file;
                }
                result.AddRange(fileResults);
            }

            return result;
        }

        public List<ParseResult> GetLocalizableStringsInHtml(string html)
        {
            List<ParseResult> result = new List<ParseResult>();

            foreach (Regex pattern in _contentPatterns)
            {
                MatchCollection matches = pattern.Matches(html);
                foreach (Match match in matches)
                {
                    string content = match.Groups["content"].Value;
                    content = RemovePatterns(content);
                    AddParseResult(pattern, match, result, content);
                }

                //We use this to split in the AddParseResult method
                //Example:  Some text <strong>bold text</strong> more text
                if (matches.Count > 0)
                {
                    html = pattern.Replace(html, ReplacementMarker);
                }
            }

            return result;
        }

        public void CreateLocalizationStrings(CreateParms parms)
        {
            ValidateCreateParms(parms);
            List<ParseResult> parseResults = GetLocalizableStrings(parms);
            ModifyRazorFiles(parseResults);
            ModifyCSharpFiles(parms, parseResults);
        }

        private void ModifyRazorFiles(List<ParseResult> parseResults)
        {
            foreach (var parseResult in parseResults)
            {
                string content = File.ReadAllText(parseResult.FilePath);
                string replacement =
                    parseResult.MatchValue.Replace(parseResult.LocalizableString, $"@_lc.Keys[\"{parseResult.Key}\"]");
                content = content.Replace(parseResult.MatchValue, replacement);
                File.WriteAllText(parseResult.FilePath, content);
            }
        }

        private void ModifyCSharpFiles(CreateParms parms, List<ParseResult> localizableStrings)
        {
            var filesToModify = localizableStrings.Select(o => o.FilePath).Distinct().ToList();

            foreach (var file in filesToModify)
            {
                string cSharpFile = file.Replace(".razor", ".cs");
                if (!File.Exists(cSharpFile))
                {
                    continue;
                }

                string cSharpContent = File.ReadAllText(cSharpFile);
                cSharpContent = InjectLanguageContainer(cSharpContent, parms.InjectLanguageContainerCode);
                cSharpContent = InitLanguageComponent(cSharpContent, parms.InitLanguageContainerCode);
                File.WriteAllText(cSharpFile, cSharpContent);
            }
        }

        private void ValidateCreateParms(CreateParms parms)
        {
            if (String.IsNullOrEmpty(parms.ResourceFilePath))
            {
                throw new ArgumentException("ResourceFilePath is required");
            }
        }

        private void AddParseResult(Regex pattern, Match match, List<ParseResult> result, string content)
        {
            const int minStringLength = 2;
            var contentList = content.Split(new[] { ReplacementMarker }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var contentItem in contentList.ToList())
            {
                var trimmed = contentItem.Trim();

                if (!String.IsNullOrEmpty(trimmed)
                    && trimmed.Length >= minStringLength
                    && !trimmed.StartsWith("@(")
                    && AtLeastOneAlphabeticCharacter(trimmed))
                {
                    result.Add(new ParseResult
                    {
                        LocalizableString = trimmed,
                        MatchingExpression = pattern,
                        FilePath = string.Empty,
                        MatchValue = match.Value,
                        Key = BuildKey(trimmed)
                    });
                }
            }
        }

        private string BuildKey(string content)
        {
            string[] words = content.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder(content.Length);

            foreach (var word in words)
            {
                sb.Append(StringUtil.ProperCase(StringUtil.FilterAlphaNumeric(word)));
            }

            return sb.ToString();
        }

        private bool AtLeastOneAlphabeticCharacter(string input)
        {
            return input.Any(char.IsLetter);
        }

        private string RemovePatterns(string html)
        {
            foreach (var pattern in _removePatterns)
            {
                html = pattern.Replace(html, string.Empty);
            }

            return html;
        }

        private static void ValidateParseParameters(ParseParms parms)
        {
            if (String.IsNullOrEmpty(parms.TargetDirectory))
            {
                throw new ArgumentException("TargetDirectory is required");
            }

            if (String.IsNullOrEmpty(parms.WildcardPattern))
            {
                throw new ArgumentException("WildcardPattern is required.  Example:  *.razor");
            }

            if (!Directory.Exists(parms.TargetDirectory))
            {
                throw new DirectoryNotFoundException($"TargetDirectory does not exist: {parms.TargetDirectory}");
            }
        }

        public string InjectLanguageContainer(string input, string languageContainer)
        {
            // Check if the languageContainer already exists in the input
            if (input.Contains(languageContainer))
            {
                return input;
            }

            var lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var injectLines = lines.Where(l => l.TrimStart().StartsWith("[Inject]")).ToList();

            if (injectLines.Any())
            {
                // Find the index of the last [Inject] line
                int lastInjectIndex = Array.LastIndexOf(lines, injectLines.Last());

                // Insert the new line after the last [Inject] line
                return string.Join(Environment.NewLine,
                    lines.Take(lastInjectIndex + 1)
                        .Concat(new[] { languageContainer })
                        .Concat(lines.Skip(lastInjectIndex + 1)));
            }

            // Find the index of the line with the opening curly brace of the class
            int classOpeningBraceIndex = Array.FindIndex(lines, l => l.TrimStart().StartsWith("public") && l.TrimEnd().EndsWith("{"));

            if (classOpeningBraceIndex != -1)
            {
                // Insert the new line after the class opening brace
                return string.Join(Environment.NewLine,
                    lines.Take(classOpeningBraceIndex + 1)
                        .Concat(new[] { "    " + languageContainer })
                        .Concat(lines.Skip(classOpeningBraceIndex + 1)));
            }

            // If no suitable location is found, return the original input
            return input;
        }

        public static string InitLanguageComponent(string input, string initLanguage)
        {
            // Check if the initLanguage already exists in the input
            if (input.Contains(initLanguage))
            {
                return input;
            }

            var lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Look for "protected override void OnInitialized()"
            int onInitializedIndex = Array.FindIndex(lines, l => l.Trim().StartsWith("protected override void OnInitialized()"));
            if (onInitializedIndex != -1)
            {
                // Find the opening brace
                int openingBraceIndex = Array.FindIndex(lines, onInitializedIndex, l => l.Contains("{"));
                if (openingBraceIndex != -1)
                {
                    // Insert initLanguage after the opening brace
                    return string.Join(Environment.NewLine,
                        lines.Take(openingBraceIndex + 1)
                        .Concat(new[] { "        " + initLanguage })
                        .Concat(lines.Skip(openingBraceIndex + 1)));
                }
            }

            // Look for "protected override async Task OnInitializedAsync()"
            int onInitializedAsyncIndex = Array.FindIndex(lines, l => l.Trim().StartsWith("protected override async Task OnInitializedAsync()"));
            if (onInitializedAsyncIndex != -1)
            {
                // Find the opening brace
                int openingBraceIndex = Array.FindIndex(lines, onInitializedAsyncIndex, l => l.Contains("{"));
                if (openingBraceIndex != -1)
                {
                    // Insert initLanguage after the opening brace
                    return string.Join(Environment.NewLine,
                        lines.Take(openingBraceIndex + 1)
                        .Concat(new[] { "        " + initLanguage })
                        .Concat(lines.Skip(openingBraceIndex + 1)));
                }
            }

            // If neither method is found, create a new OnInitialized method
            string newMethod = $@"
    protected override void OnInitialized()
    {{
        {initLanguage}
    }}";

            // Find the class closing brace
            int classClosingBraceIndex = Array.FindLastIndex(lines, l => l.Trim() == "}");
            if (classClosingBraceIndex != -1)
            {
                // Insert the new method before the class closing brace
                return string.Join(Environment.NewLine,
                    lines.Take(classClosingBraceIndex)
                    .Concat(new[] { newMethod })
                    .Concat(lines.Skip(classClosingBraceIndex)));
            }

            // If no suitable location is found, return the original input
            return input;
        }

        public static void AddResourceKeyValue(string filePath, string key, string value)
        {
            string directory = Path.GetDirectoryName(filePath);

            // Create the directory if it does not exist
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create a new YAML file with the key-value pair
            if (!File.Exists(filePath))
            {
                var newSerializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                string newContent = newSerializer.Serialize(new Dictionary<string, string> { { key, value } });
                File.WriteAllText(filePath, newContent);
                return;
            }

            // Read the YAML file
            string yamlContent = File.ReadAllText(filePath);

            // Deserialize YAML to dictionary
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            Dictionary<string, string> yamlObject = deserializer.Deserialize<Dictionary<string, string>>(yamlContent);

            // Add or update the key-value pair
            yamlObject[key] = value;

            // Sort the dictionary by keys
            var sortedYamlObject = yamlObject.OrderBy(pair => pair.Key)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            // Serialize the sorted dictionary back to YAML
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            string updatedYamlContent = serializer.Serialize(sortedYamlObject);

            // Save the updated YAML content back to the file
            File.WriteAllText(filePath, updatedYamlContent);
        }
    }
}
