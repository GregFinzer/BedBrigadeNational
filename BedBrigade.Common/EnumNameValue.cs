namespace BedBrigade.Common
{
    public class EnumNameValue<T> where T : Enum
    {
        public T Value { get; set; }
        public string Name { get; set; }
    }
}
