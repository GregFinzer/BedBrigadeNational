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
        private static List<string> _elementsToIgnore;
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

        /// <summary>
        /// Copy public properties from one object to another of differing types
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        public static void CopyProperties<TS, TD>(TS source, TD dest)
            where TD : class
            where TS : class
        {
            string name = string.Empty;

            if (source == null)
                throw new ArgumentNullException("source");

            if (dest == null)
                throw new ArgumentNullException("dest");

            InitElementsToIgnore();
            Type tsType = typeof(TS);
            Type tdType = typeof(TD);

            try
            {

                PropertyInfo[] tsProperties = tsType.GetProperties(); //Default is public instance

                foreach (PropertyInfo tsInfo in tsProperties)
                {
                    //If we can't read or write it, skip it
                    if (!tsInfo.CanRead || !tsInfo.CanWrite)
                        continue;

                    name = tsInfo.Name;

                    //Skip ignored elements
                    if (_elementsToIgnore.Contains(name))
                        continue;

                    Type tsInfoType = tsInfo.PropertyType;

                    if (tsInfoType.IsPrimitive
                        || tsInfoType.IsEnum
                        || tsInfoType.IsValueType
                        || tsInfoType == typeof(DateTime)
                        || tsInfoType == typeof(decimal)
                        || tsInfoType == typeof(string)
                        || tsInfoType == typeof(Guid)
                        || tsInfoType == typeof(TimeSpan))
                    {
                        object value = tsInfo.GetValue(source, null);

                        PropertyInfo[] tdProperties = tdType.GetProperties();

                        foreach (PropertyInfo tdInfo in tdProperties)
                        {
                            if (tsInfo.Name == tdInfo.Name)
                            {
                                if (value == null)
                                    tdInfo.SetValue(dest, null, null);
                                else
                                    tdInfo.SetValue(dest,
                                        ConvertUtil.ChangeType(value, tdInfo.PropertyType),
                                        null);

                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string msg = string.Format("{0}; Source Type: {1}, Dest Type: {2}, Property: {3}",
                                           ex.Message, tsType.FullName, tdType.FullName, name);
                throw new Exception(msg);

            }

        }

        private static void InitElementsToIgnore()
        {
            if (_elementsToIgnore == null)
            {
                _elementsToIgnore = new List<string>();
                _elementsToIgnore.Add("IsDirty");
                _elementsToIgnore.Add("IsNew");
            }
        }
    }
}
