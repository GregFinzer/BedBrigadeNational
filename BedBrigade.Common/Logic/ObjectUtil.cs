using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.Reports;
using KellermanSoftware.Serialization;

namespace BedBrigade.Common.Logic
{
    public static class ObjectUtil
    {
        public static string ObjectToString<T>(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => ShouldOutput(p.PropertyType))
                .ToList();

            int maxLength = properties.Max(p => p.Name.Length);

            StringBuilder result = new StringBuilder();

            foreach (var property in properties)
            {
                result.AppendLine($"{property.Name.PadRight(maxLength)}: {property.GetValue(obj)}");
            }

            return result.ToString();
        }

        private static bool ShouldOutput(Type type)
        {
            return type.IsPrimitive
                   || type == typeof(string)
                   || type.IsEnum
                   || type == typeof(DateTime)
                   || type == typeof(DateTime?)
                   || type == typeof(decimal)
                   || type == typeof(decimal?)
                   || type == typeof(Guid)
                   || type == typeof(Guid?);
        }

        public static T Clone<T>(T obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            Serializer serializer = new Serializer();
            byte[] bytes = serializer.Serialize(obj);
            return serializer.Deserialize<T>(bytes);
        }

        public static string Differences<T>(T obj1, T obj2)
        {
            CompareLogic compareLogic = new CompareLogic();
            compareLogic.Config.MaxDifferences = int.MaxValue;
            ComparisonResult result = compareLogic.Compare(obj1, obj2);

            StringBuilder sb = new StringBuilder();
            int maxLength = result.Differences.Max(d => d.PropertyName.Length);
            foreach (var difference in result.Differences)
            {
                sb.AppendLine($"{difference.PropertyName.PadRight(maxLength)}: {difference.Object1Value} => {difference.Object2Value}");
            }

            return sb.ToString();
        }
    }
}
