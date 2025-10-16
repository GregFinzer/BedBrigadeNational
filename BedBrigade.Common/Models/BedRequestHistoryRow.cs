using System.ComponentModel.DataAnnotations.Schema;

namespace BedBrigade.Common.Models
{
    public class BedRequestHistoryRow
    {
        public int Year { get; set; }
        public int Month { get; set; } // 1-12
        public int Count { get; set; }

        [NotMapped]
        public string MonthName => System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(Month);
    }
}
