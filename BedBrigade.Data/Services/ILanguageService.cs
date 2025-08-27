using System.Globalization;

namespace BedBrigade.Data.Services
{
    public interface ILanguageService
    {
        CultureInfo CurrentCulture { get; set; }
        event Func<CultureInfo, Task> LanguageChanged;
        Task NotifyLanguageChangedAsync();
    }
}
