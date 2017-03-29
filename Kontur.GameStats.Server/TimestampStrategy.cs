using System;

namespace Kontur.GameStats.Server
{
    public class TimestampStrategy
    {
        public TimestampStrategy()
        {
        }

        public static string GetDay(string timestamp)
        {
            return timestamp.Substring(0, 10);
        }

        public static string GetTime(string timestamp)
        {
            return timestamp.Substring (11, timestamp.Length - 11);
        }

        public static string GetMax(string timestamp1, string timestamp2)
        {
            if (String.Compare(timestamp1, timestamp2) > 0)
            {
                return timestamp1;
            }
            else
            {
                return timestamp2;
            }
        }

        public static string GetMin(string timestamp1, string timestamp2)
        {
            if (String.Compare(timestamp1, timestamp2) < 0)
            {
                return timestamp1;
            }
            else
            {
                return timestamp2;
            }
        }

        public static DateTime GetDateTime(string timestamp)
        {
            return DateTime
                .Parse (GetDay(timestamp) + " " + GetTime(timestamp));
        }

        public static DateTime GetDate(string timestamp)
        {
            return DateTime
                .Parse (GetDay (timestamp));
        }

        public static int GetDayDifference(string timestamp1, string timestamp2)
        {
            return ((int)((GetDate (timestamp2) - GetDate(timestamp1)).TotalDays)) + 1;
        }
    }
}

