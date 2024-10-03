using System.ComponentModel;

namespace BedBrigade.Common.Logic
{
    /// <summary>
    /// Methods for converting values into specific types
    /// </summary>
    public static class ConvertUtil
    {
        /// <summary>
        /// Returns an Object with the specified Type and whose value is equivalent to the specified object.
        /// </summary>
        /// <param name="value">An Object that implements the IConvertible interface.</param>
        /// <param name="conversionType">The Type to which value is to be converted.</param>
        /// <returns>An object whose Type is conversionType (or conversionType's underlying type if conversionType
        /// is Nullable&lt;&gt;) and whose value is equivalent to value. -or- a null reference, if value is a null
        /// reference and conversionType is not a value type.</returns>
        /// <remarks>
        /// This method exists as a workaround to System.Convert.ChangeType(Object, Type) which does not handle
        /// nullables as of version 2.0 (2.0.50727.42) of the .NET Framework. The idea is that this method will
        /// be deleted once Convert.ChangeType is updated in a future version of the .NET Framework to handle
        /// nullable types, so we want this to behave as closely to Convert.ChangeType as possible.
        /// This method was written by Peter Johnson at:
        /// http://aspalliance.com/author.aspx?uId=1026.
        /// </remarks>
        public static object ChangeType(object value, Type conversionType)
        {
            // Note: This if block was taken from Convert.ChangeType as is, and is needed here since we're
            // checking properties on conversionType below.
            if (conversionType == null)
            {
                throw new ArgumentNullException("conversionType");
            } // end if

            // If it's not a nullable type, just pass through the parameters to Convert.ChangeType

            if (conversionType.IsGenericType &&
              conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                // It's a nullable type, so instead of calling Convert.ChangeType directly which would throw a
                // InvalidCastException (per http://weblogs.asp.net/pjohnson/archive/2006/02/07/437631.aspx),
                // determine what the underlying type is
                // If it's null, it won't convert to the underlying type, but that's fine since nulls don't really
                // have a type--so just return null
                // Note: We only do this check if we're converting to a nullable type, since doing it outside
                // would diverge from Convert.ChangeType's behavior, which throws an InvalidCastException if
                // value is null and conversionType is a value type.
                if (value == null)
                {
                    return null;
                } // end if

                // It's a nullable type, and not null, so that means it can be converted to its underlying type,
                // so overwrite the passed-in conversion type with this underlying type
                NullableConverter nullableConverter = new NullableConverter(conversionType);
                conversionType = nullableConverter.UnderlyingType;
            } // end if

            // Now that we've guaranteed conversionType is something Convert.ChangeType can handle (i.e. not a
            // nullable type), pass the call on to Convert.ChangeType
            return Convert.ChangeType(value, conversionType);
        }

        /// <summary>
        /// Convert the string to a double, ignoring nulls using the current culture
        /// </summary>
        /// <param name="value">The object or string to parse</param>
        /// <returns>A double number</returns>
        public static double cDbl(object value)
        {
            if (value == null)
                return 0;

            double result = 0;
            string valueString;

            try
            {
                valueString = value.ToString();

                valueString = StringUtil.RemoveCurrency(valueString);
                double.TryParse(valueString, out result);
            }
            catch
            {
            }

            return result;
        }

        /// <summary>
        /// Convert an unsigned short into a byte array
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] ShortToByteArray(ushort value)
        {
            byte[] bytes = new byte[2];
            bytes[0] = (byte)(value >> 8); // bits 1- 8
            bytes[1] = (byte)(value & 0x00FF); // bits 9-16
            return bytes;
        }

        /// <summary>
        /// Convert any object to a string
        /// </summary>
        /// <param name="value">Object to convert</param>
        /// <returns>A string or string.empty</returns>
        public static string cStr(object value)
        {
            try
            {
                if (value == null)
                    return string.Empty;
                if (value == DBNull.Value)
                    return string.Empty;

                return value.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Convert the passed object into a date or return the default date
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DateTime cDate(object value)
        {
            DateTime returnValue = DateTime.MinValue;
            string valueString = cStr(value);

            DateTime.TryParse(valueString, out returnValue);

            return returnValue;
        }

        public static long cLong(object value)
        {
            string valueString = cStr(value);
            long returnValue = 0;

            long.TryParse(valueString, out returnValue);

            return returnValue;
        }

        /// <summary>
        /// Convert the passed object into an integer
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int cInt(object value)
        {
            string valueString = cStr(value);
            int returnValue = 0;

            int.TryParse(valueString, out returnValue);

            return returnValue;
        }

        public static Guid cGuid(object value)
        {
            try
            {
                if (value == null || value == DBNull.Value)
                    return Guid.Empty;

                string valueString = cStr(value);
                return new Guid(valueString);
            }
            catch
            {
                return Guid.Empty;
            }
        }

        public static decimal cDecimal(object value)
        {
            string valueString = cStr(value);
            decimal returnValue = 0;

            valueString = valueString.Replace("$", string.Empty);
            if (valueString.StartsWith("(") && valueString.EndsWith(")"))
            {
                valueString = valueString.Substring(1, valueString.Length - 2);
                valueString = "-" + valueString;
            }

            decimal.TryParse(valueString, out returnValue);

            return returnValue;
        }

        public static bool cBool(object value)
        {
            string valueString = cStr(value);
            bool returnValue = false;

            bool.TryParse(valueString, out returnValue);

            returnValue = returnValue || valueString.ToLower() == "yes";
            returnValue = returnValue || valueString.ToLower() == "y";

            return returnValue;
        }

    }
}
