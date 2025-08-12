
using System.ComponentModel;

namespace BedBrigade.Common.Enums;

public enum CanYouTranslate
{
    Yes = 1,
    [Description("Very Little")]
    VeryLittle = 2,
    [Description("NO")]
    No = 0
}
