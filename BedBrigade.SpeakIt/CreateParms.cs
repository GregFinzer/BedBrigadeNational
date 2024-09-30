using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.SpeakIt
{
    public class CreateParms : ParseParms
    {
        public string InjectCode = @"[Inject] private ILanguageContainerService _lc { get; set; }";
    }
}
