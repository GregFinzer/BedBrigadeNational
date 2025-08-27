namespace BedBrigade.Common.Models
{
    public interface ILocationId
    {
        Int32 LocationId { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }
    }
}
