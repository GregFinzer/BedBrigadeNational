using System.Reflection;

namespace BedBrigade.Common
{
    public static class TypeHelper
    {
        /// <summary>
        /// Return true if the type is an enum
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsEnum(Type type)
        {
            if (type == null)
                return false;

            if (type.GetTypeInfo().IsGenericType && type.GetTypeInfo().GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }

            return type.GetTypeInfo().IsEnum;
        }

        /// <summary>
        /// Return true if the type is a Double
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsDouble(Type type)
        {
            if (type == null)
                return false;

            return type == typeof(Double);
        }

        /// <summary>
        /// Return true if the type is a Decimal
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsDecimal(Type type)
        {
            if (type == null)
                return false;

            return type == typeof(Decimal);
        }

        /// <summary>
        /// Return true if the type is a DateTime
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsDateTime(Type type)
        {
            if (type == null)
                return false;

            return type == typeof(DateTime);
        }

        /// <summary>
        /// Return true if the type is a string
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsString(Type type)
        {
            if (type == null)
                return false;

            return type == typeof(string);
        }

        /// <summary>
        /// Return true if the type is a guid
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsGuid(Type type)
        {
            if (type == null)
                return false;

            return type == typeof(Guid);
        }

    }
}
