using System.Text.RegularExpressions;

namespace BedBrigade.Common.Logic;

public static partial class HtmlContentPathNormalizer
{
    private static readonly string[] RootRelativePrefixes =
    [
        "media/",
        "polaris/"
    ];

    [GeneratedRegex("(?<prefix>\\b(?:href|src)=\"|\\b(?:href|src)='\')(?<path>[^\"']+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RelativePathAttributeRegex();

    public static string NormalizeSeededPaths(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        return RelativePathAttributeRegex().Replace(html, match =>
        {
            string path = match.Groups["path"].Value;
            if (path.StartsWith("/", StringComparison.Ordinal)
                || path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("#", StringComparison.Ordinal)
                || path.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            {
                return match.Value;
            }

            if (RootRelativePrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                return $"{match.Groups["prefix"].Value}/{path}";
            }

            return match.Value;
        });
    }
}


