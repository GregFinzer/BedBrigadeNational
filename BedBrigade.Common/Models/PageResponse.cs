namespace BedBrigade.Common.Models;

public class PageResponse<T> where T : class
{
    public int PageNumber { get; set; }
    public int MaxPage { get; set; }
    public int NumberOfItems { get; set; }
    public int ItemsPerPage { get; set; }

    public List<T> Items { get; set; } = new List<T>();
}