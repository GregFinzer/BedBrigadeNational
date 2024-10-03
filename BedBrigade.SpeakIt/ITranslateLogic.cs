using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.SpeakIt
{
    public interface ITranslateLogic
    {
        string GetTranslation(string? value);
    }
}
