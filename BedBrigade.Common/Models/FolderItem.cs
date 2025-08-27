namespace BedBrigade.Common.Models
{
    public class FolderItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public List<FolderItem> SubFolders { get; set; } = new List<FolderItem>();
    }
}
