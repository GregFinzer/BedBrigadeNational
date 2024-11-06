using System.Globalization;
using AKSoftware.Localization.MultiLanguages;
using BedBrigade.Common.Constants;

namespace BedBrigade.Data.Services
{
    public class LanguageService : ILanguageService
    {
        private readonly ILanguageContainerService _lc;
        private CultureInfo _currentCulture;
        public event Func<CultureInfo, Task>? LanguageChanged;

        public LanguageService(ILanguageContainerService lc)
        {
            _lc = lc;
            _currentCulture = new CultureInfo(Defaults.DefaultLanguage);
        }

        public CultureInfo CurrentCulture
        {
            get
            {
                return _currentCulture;
            }
            set
            {
                if (_currentCulture.Name != value.Name)
                {
                    _currentCulture = value;
                    _lc.SetLanguage(value);
                    NotifyLanguageChangedAsync();
                }
            }
        }



        public async Task NotifyLanguageChangedAsync()
        {
            if (LanguageChanged != null)
            {
                foreach (Func<CultureInfo, Task> handler in LanguageChanged.GetInvocationList())
                {
                    await handler.Invoke(_currentCulture);
                }
            }
        }
    }
}
