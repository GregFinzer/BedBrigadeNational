namespace BedBrigade.SpeakIt
{
    public class SpeakItParms
    {
        public string? TargetDirectory { get; set; }
        public string? WildcardPattern { get; set; }
        public List<string> ExcludeDirectories { get; set; } = new List<string>();
        public List<string> ExcludeFiles { get; set; } = new List<string>();
    }
}
