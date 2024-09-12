namespace BedBrigade.Common.Models
{
    public class LocationDistance 
    {
        public Int32 LocationId { get; set; }
        public String Name { get; set; } = string.Empty;
        public String Route { get; set; } = string.Empty;
        public double Distance { get; set; }

        public string MilesAwayString
        {
            get
            {
                if (Distance == 0)
                {
                    return "is in your zip code";
                }

                return Math.Round(Distance, 1).ToString("0.0") + " miles away";
            }
        }
    }
}
