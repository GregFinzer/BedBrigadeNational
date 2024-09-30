using System.Text.RegularExpressions;

namespace BedBrigade.SpeakIt
{
    public class ParseResult
    {
        public Regex MatchingExpression { get; set; }
        public string FilePath { get; set; }
        public string LocalizableString { get; set; }
        public string MatchValue { get; set; }
        public string Key { get; set; }
    }
}
