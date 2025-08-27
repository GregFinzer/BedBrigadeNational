namespace BedBrigade.Common.Models
{
    public class FileItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Extension { get; set; }
        public string Type { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsSelected { get; set; }
    }
}
