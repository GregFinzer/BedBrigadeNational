using System.Text.RegularExpressions;

namespace BedBrigade.SpeakIt
{
    public class ParseResult
    {
        public Regex MatchingExpression { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string LocalizableString { get; set; } = string.Empty;
        public string MatchValue { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
    }
}
