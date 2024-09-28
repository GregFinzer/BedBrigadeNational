using System.Collections.Generic;
using System.Text.RegularExpressions;

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

        public List<ParseResult> GetLocalizableStrings(SpeakItParms parms)
        {
            ValidateParameters(parms);

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
                        MatchValue = match.Value
                    });
                }
            }
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

        private static void ValidateParameters(SpeakItParms parms)
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
    }
}
