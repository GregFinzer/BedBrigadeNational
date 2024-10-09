using System.Globalization;

namespace BedBrigade.SpeakIt
{
    public interface ITranslateLogic
    {
        string GetTranslation(string? value);
        List<CultureInfo> GetRegisteredLanguages();
    }
}
