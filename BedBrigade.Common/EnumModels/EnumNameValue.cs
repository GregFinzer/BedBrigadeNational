namespace BedBrigade.Common.EnumModels
{
    public class EnumNameValue<T> where T : Enum
    {
        public T Value { get; set; }
        public string Name { get; set; }
    }
}
