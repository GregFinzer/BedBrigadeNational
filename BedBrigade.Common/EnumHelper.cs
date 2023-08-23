using System.ComponentModel;

namespace BedBrigade.Common
{
    public static class EnumHelper
    {       
        public static string GetEnumDescription<T>(T value) where T : Enum
        {
            var fi = value.GetType().GetField(value.ToString());
            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }

        public static List<EnumNameValue<T>> GetEnumNameValues<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>().Select(e => new EnumNameValue<T> { Value = e, Name = GetEnumDescription(e) }).ToList();
        }
    }
}
