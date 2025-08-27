using BedBrigade.Common.Models;
using System.Globalization;

namespace BedBrigade.SpeakIt
{
    public interface ITranslateLogic
    {
        string GetTranslation(string? value);
        List<CultureInfo> GetRegisteredLanguages();
        string? CleanUpSpacesAndLineFeedsFromHtml(string? input);
        string ComputeSHA512Hash(string input);
        Dictionary<string, List<Translation>> TranslationsToDictionary(List<Translation> translations);
        string? ParseAndTranslateText(string input, string targetCulture,  Dictionary<string, List<Translation>> translations);
    }
}
