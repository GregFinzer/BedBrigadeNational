namespace BedBrigade.Data.Models
{
    public class LocationDistance 
    {
        public Int32 LocationId { get; set; }
        public String Name { get; set; } = string.Empty;
        public String Route { get; set; } = string.Empty;
        public double Distance { get; set; }
    }
}
