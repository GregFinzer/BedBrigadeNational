﻿using System.Collections.Generic;
using System.Data.Entity.Core.Common.EntitySql;
using System.Text;
using System.Text.RegularExpressions;
using BedBrigade.Common.Logic;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BedBrigade.SpeakIt
{
    public class SpeakItLogic
    {
        private const string ReplacementMarker = "~~~";
        private static Regex KeyReference = new Regex("@_lc.Keys\\[\\\\?\"(?<content>[^\\\\\"]+)\\\\?\"\\]", RegexOptions.Compiled | RegexOptions.Multiline);

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
            new Regex(@"Placeholder=""(?<content>[^""]+)""", RegexOptions.Compiled | RegexOptions.Multiline),
            new Regex(@"Label=""(?<content>[^""]+)""", RegexOptions.Compiled | RegexOptions.Multiline),
            new Regex(@"<button[^<]+>(?<content>.*?)</button>", RegexOptions.Compiled|RegexOptions.IgnoreCase|RegexOptions.Multiline),
            new Regex(@"<SfButton[^<]+>(?<content>.*?)</SfButton>", RegexOptions.Compiled|RegexOptions.IgnoreCase|RegexOptions.Multiline),
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

        public List<ParseResult> GetExistingLocalizedStrings(CreateParms parms)
        {
            ValidateParseParameters(parms);
            var existingKeyValues = ReadYamlFile(parms.ResourceFilePath);
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
                string content = File.ReadAllText(file);
                var fileResults = GetExistingLocalizedStringsInHtml(content, existingKeyValues);

                foreach (var fileResult in fileResults)
                {
                    fileResult.FilePath = file;
                }
                result.AddRange(fileResults);
            }

            return result;
        }

        public List<ParseResult> GetExistingLocalizedStringsInHtml(string html,
            Dictionary<string, string> existingKeyValues)
        {
            List<ParseResult> result = new List<ParseResult>();

            MatchCollection matches = KeyReference.Matches(html);
            foreach (Match match in matches)
            {
                string existingKey = match.Groups["content"].Value;
                string existingValue =
                    existingKeyValues.ContainsKey(existingKey) ? existingKeyValues[existingKey] : null;

                result.Add(new ParseResult
                {
                    LocalizableString = existingValue,
                    MatchingExpression = KeyReference,
                    FilePath = string.Empty,
                    MatchValue = match.Value,
                    Key = existingKey
                });
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
            ModifyResourceFile(parms, parseResults);
            ModifyRazorFiles(parseResults);
            ModifyCSharpFiles(parms, parseResults);
        }

        private void ModifyResourceFile(CreateParms parms, List<ParseResult> parseResults)
        {
            foreach (var parseResult in parseResults)
            {
                AddResourceKeyValue(parms.ResourceFilePath, parseResult.Key, parseResult.LocalizableString);
            }
        }


        private void ModifyRazorFiles(List<ParseResult> parseResults)
        {
            // Order the parseResults by FilePath
            var orderedResults = parseResults.OrderBy(pr => pr.FilePath).ToList();

            string currentFilePath = null;
            string content = null;

            foreach (var parseResult in orderedResults)
            {
                // If the file path has changed, read the new file
                if (currentFilePath != parseResult.FilePath)
                {
                    // Write the modified content of the previous file if it exists
                    if (currentFilePath != null && content != null)
                    {
                        File.WriteAllText(currentFilePath, content);
                    }

                    // Read the new file
                    currentFilePath = parseResult.FilePath;
                    content = File.ReadAllText(currentFilePath);
                }

                string replacement;
                if (parseResult.MatchValue.Contains("button")
                    || parseResult.MatchValue.Contains("SfButton"))
                {
                    replacement = parseResult.MatchValue.Replace($">{parseResult.LocalizableString}<"
                        , $">@_lc.Keys[\"{parseResult.Key}\"]<");
                }
                else
                {
                    replacement = parseResult.MatchValue.Replace(parseResult.LocalizableString, $"@_lc.Keys[\"{parseResult.Key}\"]");
                }

                content = content.Replace(parseResult.MatchValue, replacement);
            }

            // Write the last file if it exists
            if (currentFilePath != null && content != null)
            {
                File.WriteAllText(currentFilePath, content);
            }
        }

        private void ModifyCSharpFiles(CreateParms parms, List<ParseResult> localizableStrings)
        {
            var filesToModify = localizableStrings.Select(o => o.FilePath).Distinct().ToList();
            foreach (var file in filesToModify)
            {
                string cSharpFile = file + ".cs";
                if (!File.Exists(cSharpFile))
                {
                    continue;
                }

                string original;
                using (StreamReader reader = new StreamReader(cSharpFile))
                {
                    original = reader.ReadToEnd();
                }

                string modified = InjectLanguageContainer(original, parms.InjectLanguageContainerCode);
                modified = InitLanguageComponent(modified, parms.InitLanguageContainerCode);

                using (StreamWriter writer = new StreamWriter(cSharpFile))
                {
                    writer.Write(modified);
                }
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
                    && !trimmed.StartsWith("@_")
                    && !trimmed.StartsWith("[@_")
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
            const int maxWords = 4;
            string[] words = content.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder(content.Length);

            for (int i = 0; i < words.Length && i < maxWords; i++)
            {
                sb.Append(StringUtil.ProperCase(StringUtil.FilterAlphaNumeric(words[i])));
            }

            if (content.EndsWith(":"))
            {
                sb.Append("Colon");
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

        private void ValidateParseParameters(ParseParms parms)
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
                        .Concat(new[] { "        " + languageContainer })
                        .Concat(lines.Skip(lastInjectIndex + 1)));
            }

            // Find the index of the line with the opening curly brace of the class
            int classOpeningBraceIndex = Array.FindIndex(lines, l => l.TrimStart().StartsWith("public") && l.TrimEnd().EndsWith("{"));

            if (classOpeningBraceIndex != -1)
            {
                // Insert the new line after the class opening brace
                return string.Join(Environment.NewLine,
                    lines.Take(classOpeningBraceIndex + 1)
                        .Concat(new[] { "        " + languageContainer })
                        .Concat(lines.Skip(classOpeningBraceIndex + 1)));
            }

            // If no suitable location is found, return the original input
            return input;
        }

        public string InitLanguageComponent(string input, string initLanguage)
        {
            // Check if the initLanguage already exists in the input
            if (input.Contains(initLanguage))
            {
                return input;
            }

            string[] lines = input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Look for "protected override void OnInitialized()"
            string? insertOnInitializedResult = InsertOnInitialized(lines, initLanguage);
            if (insertOnInitializedResult != null)
            {
                return insertOnInitializedResult;
            }

            // Look for "protected override async Task OnInitializedAsync()"
            string? insertOnInitializedAsyncResult = InsertOnInitializedAsync(lines, initLanguage);

            if (insertOnInitializedAsyncResult != null)
            {
                return insertOnInitializedAsyncResult;
            }

            // If neither method is found, create a new OnInitialized method
            string? newOnInitializedResult = InsertNewOnInitialized(lines, initLanguage);

            if (newOnInitializedResult != null)
            {
                return newOnInitializedResult;
            }

            // If no suitable location is found, return the original input
            return input;
        }

        private string? InsertNewOnInitialized(string[] lines, string initLanguage)
        {
            string newMethod = $@"
    protected override void OnInitialized()
    {{
        {initLanguage}
    }}";

            // Find the last closing brace (namespace)
            int lastClosingBraceIndex = Array.FindLastIndex(lines, l => l.Trim() == "}");

            if (lastClosingBraceIndex != -1)
            {
                // Find the next-to-last closing brace by searching up to the last closing brace index (class)
                int nextToLastClosingBraceIndex = Array.FindLastIndex(lines, lastClosingBraceIndex - 1, l => l.Trim() == "}");

                if (nextToLastClosingBraceIndex != -1)
                {
                    // Insert the new method before the class closing brace
                    return string.Join(Environment.NewLine,
                        lines.Take(nextToLastClosingBraceIndex)
                            .Concat(new[] { newMethod })
                            .Concat(lines.Skip(nextToLastClosingBraceIndex)));
                }
            }

            return null;
        }


        private string? InsertOnInitializedAsync(string[] lines, string initLanguage)
        {
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
                            .Concat(new[] { "            " + initLanguage })
                            .Concat(lines.Skip(openingBraceIndex + 1)));
                }
            }

            return null;
        }

        private string? InsertOnInitialized(string[] lines, string initLanguage)
        {
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
                            .Concat(new[] { "            " + initLanguage })
                            .Concat(lines.Skip(openingBraceIndex + 1)));
                }
            }

            return null;
        }

        public void AddResourceKeyValue(string filePath, string key, string value)
        {
            string directory = Path.GetDirectoryName(filePath);
            // Create the directory if it does not exist
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create a new YAML file with the key-value pair if it doesn't exist
            if (!File.Exists(filePath))
            {
                CreateNewYamlFile(filePath, key, value);
                return;
            }

            UpdateYamlFile(filePath, key, value);
        }

        private void UpdateYamlFile(string filePath, string key, string value)
        {
            // Read the YAML file
            string yamlContent;
            using (StreamReader reader = new StreamReader(filePath))
            {
                yamlContent = reader.ReadToEnd();
            }

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
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write(updatedYamlContent);
            }
        }

        private void CreateNewYamlFile(string filePath, string key, string value)
        {
            var newSerializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            string newContent = newSerializer.Serialize(new Dictionary<string, string> { { key, value } });
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write(newContent);
            }
        }

        public Dictionary<string, List<string>> GetDuplicateValues(ParseParms parms)
        {
            var parseResults = GetLocalizableStrings(parms);
            var conflictingKeys = GetDuplicateValues(parseResults);
            return conflictingKeys;
        }

        private Dictionary<string, List<string>> GetDuplicateValues(List<ParseResult> parseResults)
        {
            var conflictingKeys = new Dictionary<string, List<string>>();
            var keyDict = new Dictionary<string, string>();

            foreach (var result in parseResults)
            {
                if (string.IsNullOrEmpty(result.Key))
                {
                    continue;
                }

                if (!keyDict.ContainsKey(result.Key))
                {
                    keyDict[result.Key] = result.LocalizableString;
                }
                else if (keyDict[result.Key] != result.LocalizableString)
                {
                    if (!conflictingKeys.ContainsKey(result.Key))
                    {
                        conflictingKeys.Add(result.Key, new List<string>() { keyDict[result.Key] });
                    }
                    conflictingKeys[result.Key].Add(result.LocalizableString);
                }
            }

            return conflictingKeys;
        }


        public List<string> GetUnusedKeys(CreateParms parms)
        {
            if (string.IsNullOrEmpty(parms.ResourceFilePath))
            {
                throw new ArgumentException("ResourceFilePath is required");
            }

            // Read existing keys from the YAML file
            var existingKeys = ReadYamlFile(parms.ResourceFilePath).Keys.ToList();

            // Get localizable strings to find keys in use
            var parseResults = GetExistingLocalizedStrings(parms);
            var keysInUse = parseResults.Select(r => r.Key).Distinct().ToList();

            // Find keys that exist in the YAML file but are not in use
            var unusedKeys = existingKeys.Except(keysInUse).ToList();

            return unusedKeys;
        }

        private Dictionary<string, string> ReadYamlFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new Dictionary<string, string>();
            }

            string yamlContent;
            using (StreamReader reader = new StreamReader(filePath))
            {
                yamlContent = reader.ReadToEnd();
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            return deserializer.Deserialize<Dictionary<string, string>>(yamlContent);
        }
    }
}
