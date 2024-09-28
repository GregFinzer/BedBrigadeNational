using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BedBrigade.SpeakIt
{
    public class ParseResult
    {
        public Regex MatchingExpression { get; set; }
        public string FilePath { get; set; }
        public string LocalizableString { get; set; }
        public string MatchValue { get; set; }
    }
}
