//The reason why we have an iFrame Control is that the Syncfusion Rich Text editor
//will strip out iFrame tags when saving the content.
using System.Text.RegularExpressions;

namespace BedBrigade.Client.Services
{
    public class IFrameControlService : IIFrameControlService
    {
        private const string _pattern = @"<div\s+data-component=""(?<component>[^""]+)""\s+id=""(?<id>[^""]+)""\s+width=""(?<width>[^""]+)""\s+height=""(?<height>[^""]+)""\s+src=""(?<src>[^""]+)""[^>]*>";
        private static readonly Regex _regex = new Regex(_pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public string ReplaceiFrames(string html)
        {
            var tags = GetBbIFrameTags(html);
            if (!tags.Any())
                return html;

            foreach (var tag in tags)
            {
                html = html.Replace(tag.Tag, GenerateIFrameHtml(tag.Id, tag.Width, tag.Height, tag.Src));
            }
            return html;
        }

        private string? GenerateIFrameHtml(string tagId, string tagWidth, string tagHeight, string tagSrc)
        {
            if (tagSrc.Contains("youtube"))
            {
                return $"<iframe id=\"{tagId}\" src=\"{tagSrc}\" style=\"border:none; width:{tagWidth}; height:{tagHeight};\" allow=\"accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture\" allowfullscreen=\"\"></iframe>";
            }
            return $"<iframe id=\"{tagId}\" src=\"{tagSrc}\" style=\"border:none; width:{tagWidth}; height:{tagHeight};\"></iframe>";
        }

        private List<(string Tag, string Id, string Width, string Height, string Src)> GetBbIFrameTags(string htmlText)
        {
            var matches = _regex.Matches(htmlText);
            var tags = new List<(string Tag, string Id, string Width, string Height, string Src)>();
            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 6)
                {
                    tags.Add((match.Value,
                        match.Groups["id"].Value,
                        match.Groups["width"].Value,
                        match.Groups["height"].Value,
                        match.Groups["src"].Value));
                }
            }
            return tags;
        }

    }
}
