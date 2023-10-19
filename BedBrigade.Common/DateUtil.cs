using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BedBrigade.Common
{
    public static class DateUtil
    {
        public static String MillisecondsToTimeLapse(long milliseconds)
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(milliseconds);

            string result;

            if ((long)ts.TotalDays == 1)
                result = string.Format("{0:n0} day, {1:n0} hours, {2:n0} minutes, {3:n0} seconds, {4:n0} milliseconds", ts.Days, ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            else if (ts.TotalDays >= 1)
                result = string.Format("{0:n0} days, {1:n0} hours, {2:n0} minutes, {3:n0} seconds, {4:n0} milliseconds", ts.Days, ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            else if ((long)ts.TotalHours == 1)
                result = string.Format("{0:n0} hr, {1:n0} minutes, {2:n0} seconds, {3:n0} milliseconds", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            else if (ts.TotalHours >= 1)
                result = string.Format("{0:n0} hours, {1:n0} minutes, {2:n0} seconds, {3:n0} milliseconds", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
            else if ((long)ts.TotalMinutes == 1)
                result = string.Format("{0:n0} min, {1:n0} seconds, {2:n0} milliseconds", ts.Minutes, ts.Seconds, ts.Milliseconds);
            else if (ts.TotalMinutes >= 1)
                result = string.Format("{0:n0} minutes, {1:n0} seconds, {2:n0} milliseconds", ts.Minutes, ts.Seconds, ts.Milliseconds);
            else if ((long)ts.TotalSeconds == 1)
                result = string.Format("{0:n0} second, {1:n0} milliseconds", ts.Seconds, ts.Milliseconds);
            else if (ts.TotalSeconds >= 1)
                result = string.Format("{0:n0} seconds, {1:n0} milliseconds", ts.Seconds, ts.Milliseconds);
            else
                result = string.Format("{0:n0} milliseconds", ts.Milliseconds);

            result = result.Replace(" 1 hours", " 1 hour");
            result = result.Replace(" 1 minutes", " 1 minute");
            result = result.Replace(" 1 seconds", " 1 second");

            return result;
        } // Timelaps

        public static string GetFormattedDateTime(DateTime? myDate)
        {            
            string strReturnDate = string.Empty;
            string strFormattedDate = String.Empty;
            string strFormattedTime = string.Empty;

            if (myDate != null)
            {
                DateTime regularDate = myDate.HasValue ? myDate.Value : default(DateTime);


                try
                {
                    strFormattedDate = regularDate.ToShortDateString();
                    strFormattedTime = regularDate.ToShortTimeString();

                    strReturnDate = strFormattedDate + ", " + strFormattedTime;

                }
                catch(Exception ex) { }

            }

            return (strReturnDate);

        } // Format Date
       

    } // class
} // namespace
