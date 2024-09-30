using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.SpeakIt
{
    public class CreateParms : ParseParms
    {
        public string? ResourceFilePath { get; set; }
        public string InjectLanguageContainerCode { get; set; } = @"[Inject] private ILanguageContainerService _lc { get; set; }";
        public string InitLanguageContainerCode { get; set; } = @"_lc.InitLocalizedComponent(this);";
    }
}
