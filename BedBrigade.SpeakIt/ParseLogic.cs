using BedBrigade.Common.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BedBrigade.SpeakIt
{
    public class ParseLogic
    {
        private static List<string> _ignoreStartsWith = new List<string>() { "http://", "https://", "class=", "style=", "src=", "alt=", "width=", "height=", "id=", "if (", "var ", "%", "display:" };
        private const string ReplacementMarker = "~~~";
        private const string StringType = "string";
        private const string PropertyTypeGroup = "propertyType";
        private const string ContentGroup = "content";
        //We intentionally do not end with a ] because of other parameters that may be in the string
        private static Regex _keyReferenceRegex = new Regex("_lc.Keys\\[\\\\?\"(?<content>[^\\\\\"]+)\\\\?\"", RegexOptions.Compiled | RegexOptions.Multiline);

        private static Regex _propertyRegex =
            new Regex(@"public\s+(?<propertyType>[^\s\?]+\??)\s+(?<content>\w+)\s*{\s*get;\s*set;\s*}",
                RegexOptions.Compiled | RegexOptions.Multiline);

        private static Regex _requiredAttributeWithMessageRegex = new Regex(
            @"\[Required\(ErrorMessage\s*=\s*""(?<content>.*?)""\)\]", RegexOptions.Compiled | RegexOptions.Multiline);

        private static Regex _requiredAttributeRegex = new Regex(
            @"\[Required\]", RegexOptions.Compiled | RegexOptions.Multiline);

        private static Regex _maxLengthAttributeWithMessageRegex = new Regex(
            @"\[MaxLength\((?<maxLength>\d+),\s*ErrorMessage\s*=\s*""(?<content>.*?)""\)\]",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static Regex _maxLengthAttributeRegex = new Regex(
            @"\[MaxLength\((?<maxLength>\d+)\]",
            RegexOptions.Compiled | RegexOptions.Multiline);

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
            new Regex(@"\sPlaceholder=""(?<content>[^""]+)""\s", RegexOptions.Compiled | RegexOptions.Multiline),
            new Regex(@"\sLabel=""(?<content>[^""]+)""\s", RegexOptions.Compiled | RegexOptions.Multiline),
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
            //This pattern matches text that is NOT wrapped by an HTML tag. If the HTML is minified, this will not work. It expects CR or LF before the text
            new Regex(@"([\r\n]+\s*(?<content>[A-Za-z][^<\r\n>]+))|(^(?<content>[A-Za-z][^<\r\n>]+)$)", RegexOptions.Compiled | RegexOptions.Multiline),
        };

        
        public List<ParseResult> GetLocalizableStrings(SpeakItParms parms)
        {
            if (String.IsNullOrEmpty(parms.ResourceFilePath))
            {
                throw new ArgumentException("ResourceFilePath is required");
            }

            ValidateParseParameters(parms);

            List<string> files = GetAllFilesForSourceDirectoriesAndWildcards(parms);

            List<string> filesToProcess = files
                .Where(o => parms.ExcludeFiles.All(a => a != Path.GetFileName(o)))
                .Where(o => parms.ExcludeDirectories.All(a =>
                    !o.Contains(Path.DirectorySeparatorChar + a + Path.DirectorySeparatorChar)))
                .ToList();

            List<ParseResult> result = new List<ParseResult>();

            foreach (var file in filesToProcess)
            {
                string content = File.ReadAllText(file);
                List<ParseResult> fileResults = GetLocalizableStringsInText(content);
                fileResults.AddRange(GetRequiredWithErrorMessageInText(content));
                fileResults.AddRange(GetRequiredAttributeInText(content));
                fileResults.AddRange(GetMaxLengthAttributesWithErrorMessageInText(content));
                fileResults.AddRange(GetMaxLengthAttributesInText(content));
                foreach (var fileResult in fileResults)
                {
                    fileResult.FilePath = file;
                }
                result.AddRange(fileResults);
            }

            var existingKeyValues = ReadYamlFile(parms.ResourceFilePath);
            result = RemoveLocalizedKeys(result, existingKeyValues);
            return result;
        }

        private List<ParseResult> RemoveLocalizedKeys(List<ParseResult> parseResults, Dictionary<string, string> existingKeyValues)
        {
            List<ParseResult> result = new List<ParseResult>();

            foreach (var parseResult in parseResults)
            {
                if ((parseResult.Key.StartsWith(SpeakItGlobals.RequiredPrefix)
                    || parseResult.Key.StartsWith(SpeakItGlobals.MaxLengthPrefix)
                    || parseResult.Key.StartsWith(SpeakItGlobals.DynamicPrefix))
                    && !existingKeyValues.ContainsKey(parseResult.Key))
                {
                    result.Add(parseResult);
                }
            }

            return result;
        }

        public List<ParseResult> GetMaxLengthAttributesWithErrorMessageInText(string text)
        {
            List<ParseResult> result = new List<ParseResult>();

            MatchCollection matches = _maxLengthAttributeWithMessageRegex.Matches(text);

            foreach (Match match in matches)
            {
                string errorMessage = match.Groups[ContentGroup].Value;
                string maxLength = match.Groups["maxLength"].Value;
                int index = text.IndexOf(match.Value);
                Match propertyStringMatch = _propertyRegex.Match(text.Substring(index));
                string propertyName = propertyStringMatch.Groups[ContentGroup].Value;
                string propertyType = propertyStringMatch.Groups[PropertyTypeGroup].Value;

                //TODO:  We currently only support string properties for required
                if (!propertyType.ToLower().StartsWith(StringType))
                    continue;

                result.Add(new ParseResult
                {
                    LocalizableString = errorMessage,
                    MatchingExpression = _maxLengthAttributeWithMessageRegex,
                    FilePath = string.Empty,
                    MatchValue = match.Value,
                    Key = $"{SpeakItGlobals.MaxLengthPrefix}{propertyName}{maxLength}"
                });
            }

            return result;
        }

        public List<ParseResult> GetMaxLengthAttributesInText(string text)
        {
            List<ParseResult> result = new List<ParseResult>();

            MatchCollection matches = _maxLengthAttributeRegex.Matches(text);

            foreach (Match match in matches)
            {
                string maxLength = match.Groups["maxLength"].Value;
                int index = text.IndexOf(match.Value);
                Match propertyStringMatch = _propertyRegex.Match(text.Substring(index));
                string propertyName = propertyStringMatch.Groups[ContentGroup].Value;
                string propertyType = propertyStringMatch.Groups[PropertyTypeGroup].Value;

                //TODO:  We currently only support string properties for required
                if (!propertyType.ToLower().StartsWith(StringType))
                    continue;

                string errorMessage = $"{StringUtil.InsertSpaces(propertyName)} has a maximum length of {maxLength} characters";
                result.Add(new ParseResult
                {
                    LocalizableString = errorMessage,
                    MatchingExpression = _maxLengthAttributeRegex,
                    FilePath = string.Empty,
                    MatchValue = match.Value,
                    Key = $"{SpeakItGlobals.MaxLengthPrefix}{propertyName}{maxLength}"
                });
            }

            return result;
        }

        public List<ParseResult> GetRequiredWithErrorMessageInText(string text)
        {
            List<ParseResult> result = new List<ParseResult>();

            MatchCollection matches = _requiredAttributeWithMessageRegex.Matches(text);

            foreach (Match match in matches)
            {
                string errorMessage = match.Groups[ContentGroup].Value;
                int index = text.IndexOf(match.Value);
                Match propertyStringMatch = _propertyRegex.Match(text.Substring(index));
                string propertyName = propertyStringMatch.Groups[ContentGroup].Value;
                string propertyType = propertyStringMatch.Groups[PropertyTypeGroup].Value;

                //TODO:  We currently only support string properties for required
                if (!propertyType.ToLower().StartsWith(StringType))
                    continue;

                result.Add(new ParseResult
                {
                    LocalizableString = errorMessage,
                    MatchingExpression = _requiredAttributeWithMessageRegex,
                    FilePath = string.Empty,
                    MatchValue = match.Value,
                    Key = $"{SpeakItGlobals.RequiredPrefix}{propertyName}"
                });
            }

            return result;
        }

        public List<ParseResult> GetRequiredAttributeInText(string text)
        {
            List<ParseResult> result = new List<ParseResult>();

            MatchCollection matches = _requiredAttributeRegex.Matches(text);

            foreach (Match match in matches)
            {
                int index = text.IndexOf(match.Value);
                Match propertyStringMatch = _propertyRegex.Match(text.Substring(index));
                string propertyName = propertyStringMatch.Groups[ContentGroup].Value;
                string propertyType = propertyStringMatch.Groups[PropertyTypeGroup].Value;

                //TODO:  We currently only support string properties for required
                if (!propertyType.ToLower().StartsWith(StringType))
                    continue;

                string errorMessage = $"{StringUtil.InsertSpaces(propertyName)} is required";

                result.Add(new ParseResult
                {
                    LocalizableString = errorMessage,
                    MatchingExpression = _requiredAttributeWithMessageRegex,
                    FilePath = string.Empty,
                    MatchValue = match.Value,
                    Key = $"{SpeakItGlobals.RequiredPrefix}{propertyName}"
                });
            }

            return result;
        }

        public List<ParseResult> GetLocalizableStringsInText(string text)
        {
            List<ParseResult> result = new List<ParseResult>();

            foreach (Regex pattern in _contentPatterns)
            {
                MatchCollection matches = pattern.Matches(text);
                foreach (Match match in matches)
                {
                    string content = match.Groups[ContentGroup].Value;
                    content = RemovePatterns(content);
                    AddParseResult(pattern, match, result, content);
                }

                //We use this to split in the AddParseResult method
                //Example:  Some text <strong>bold text</strong> more text
                if (matches.Count > 0)
                {
                    text = pattern.Replace(text, ReplacementMarker);
                }
            }

            return result;
        }

        
        public List<ParseResult> GetExistingLocalizedStringsInText(string html,
            Dictionary<string, string> existingKeyValues)
        {
            List<ParseResult> result = new List<ParseResult>();
            html = RemoveScript(html);

            MatchCollection matches = _keyReferenceRegex.Matches(html);
            foreach (Match match in matches)
            {
                string existingKey = match.Groups[ContentGroup].Value;
                string existingValue =
                    existingKeyValues.ContainsKey(existingKey) ? existingKeyValues[existingKey] : null;

                result.Add(new ParseResult
                {
                    LocalizableString = existingValue,
                    MatchingExpression = _keyReferenceRegex,
                    FilePath = string.Empty,
                    MatchValue = match.Value,
                    Key = existingKey
                });
            }

            return result;
        }

        public Dictionary<string, List<string>> GetDuplicateValues(SpeakItParms parms)
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

        public List<string> GetUnusedKeys(SpeakItParms parms)
        {
            if (string.IsNullOrEmpty(parms.ResourceFilePath))
            {
                throw new ArgumentException("ResourceFilePath is required");
            }

            // Read existing keys from the YAML file
            var existingKeys = ReadYamlFile(parms.ResourceFilePath).Keys.ToList();

            //Exclude Required, MaxLength, Dynamic because those are used dynamically in ValidationLocalization
            existingKeys = existingKeys.Where(k => !k.StartsWith(SpeakItGlobals.RequiredPrefix)
                                                   && !k.StartsWith(SpeakItGlobals.MaxLengthPrefix)
                                                   && !k.StartsWith(SpeakItGlobals.DynamicPrefix)
                                                   ).ToList();

            // Get localizable strings to find keys in use
            var parseResults = GetExistingLocalizedStrings(parms);
            var keysInUse = parseResults.Select(r => r.Key)
                .Distinct()
                .ToList();

            // Find keys that exist in the YAML file but are not in use
            var unusedKeys = existingKeys.Except(keysInUse).ToList();

            return unusedKeys;
        }

        public List<ParseResult> GetExistingLocalizedStrings(SpeakItParms parms)
        {
            ValidateParseParameters(parms);
            var existingKeyValues = ReadYamlFile(parms.ResourceFilePath);
            List<string> files =  GetAllFilesForSourceDirectoriesAndWildcards(parms);

            List<string> filesToProcess = files
                .Where(o => parms.ExcludeFiles.All(a => a != Path.GetFileName(o)))
                .Where(o => parms.ExcludeDirectories.All(a =>
                    !o.Contains(Path.DirectorySeparatorChar + a + Path.DirectorySeparatorChar)))
                .ToList();

            List<ParseResult> result = new List<ParseResult>();

            foreach (var file in filesToProcess)
            {
                string content = File.ReadAllText(file);
                var fileResults = GetExistingLocalizedStringsInText(content, existingKeyValues);

                foreach (var fileResult in fileResults)
                {
                    fileResult.FilePath = file;
                }
                result.AddRange(fileResults);
            }

            return result;
        }

        public List<string> GetAllFilesForSourceDirectoriesAndWildcards(SpeakItParms parms)
        {
            List<string> files = new List<string>();

            foreach (var sourceDirectory in parms.SourceDirectories)
            {
                foreach (var wildcardPattern in parms.WildcardPatterns)
                {
                    string[] sourceFiles = Directory.GetFiles(sourceDirectory, wildcardPattern, SearchOption.AllDirectories);
                    files.AddRange(sourceFiles);
                }
            }

            return files.Distinct().OrderBy(o => o) .ToList();
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
                    && AtLeastOneAlphabeticCharacter(trimmed)
                    && !_ignoreStartsWith.Any(o => trimmed.StartsWith(o)))
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

        private void ValidateParseParameters(SpeakItParms parms)
        {
            if (parms.SourceDirectories == null || parms.SourceDirectories.Count == 0)
            {
                throw new ArgumentException("SourceDirectories is required and must have at least one directory specified");
            }

            if (parms.WildcardPatterns == null || parms.WildcardPatterns.Count == 0)
            {
                throw new ArgumentException("WildcardPattern is required and must have at least one specified.  Example:  *.razor");
            }

            foreach (var parmsSourceDirectory in parms.SourceDirectories)
            {
                if (!Directory.Exists(parmsSourceDirectory))
                {
                    throw new DirectoryNotFoundException($"The specified Source Directory does not exist: {parmsSourceDirectory}");
                }

            }
        }

        public string RemoveScript(string input)
        {
            // Regular expression to match anything between <script ...> and </script> tags
            string pattern = @"<script.*?>.*?</script>";

            // Replace the matched script block with an empty string
            string result = Regex.Replace(input, pattern, string.Empty, RegexOptions.Multiline | RegexOptions.IgnoreCase);

            return result;
        }
    }
}
