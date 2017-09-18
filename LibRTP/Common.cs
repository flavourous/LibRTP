using System;
using System.Collections.Generic;

namespace LibRTP
{
    [Flags]
	public enum RS : uint { None =0,  Minute = 1, Hour=2, Day = 4, Week = 8, Month = 16, Year = 32 }; // none is special

    public enum WeekStartConfig { Unary, Whole };
	public static class PublicHelpers
	{
		public static String AsString(this RS spn)
		{
			switch (spn) 
			{
                case RS.Minute: return "Minute";
                case RS.Hour: return "Hour";
                case RS.Day: return "Day";
				case RS.Week: return "Week";
				case RS.Month: return "Month";
				case RS.Year: return "Year";
				default: throw new ArgumentException ();
			}
		}
        public static DateTime StartOfDay(this DateTime somit)
        {
            return new DateTime(somit.Year, somit.Month, somit.Day);
        }
        public static DateTime FirstWeekOfYear(this DateTime value, WeekStartConfig c)
        {
            var nval = value.AddDays(-value.DayOfYear + 1);
            if (c == WeekStartConfig.Unary) return nval;
            int dow = nval.DayOfWeekStartingMonday();
            if (dow > 0) // move forward to start of first week, this is the last week in previous year or part of it...
                nval = nval.AddDays(7 - dow);
            return nval;
        }
        public static DateTime FirstWeekOfMonth(this DateTime value, WeekStartConfig c)
        {
            var d1 = new DateTime(value.Year, value.Month, 1);
            if (c == WeekStartConfig.Unary) return d1;
            return d1.AddDays((7 - d1.DayOfWeekStartingMonday()) % 7);
        }
        public static DateTime FirstDayOfWeek(this DateTime value, WeekStartConfig c)
        {
            var d1 = new DateTime(value.Year, value.Month, value.Day);
            if (c == WeekStartConfig.Unary) return d1;
            return d1.AddDays(-d1.DayOfWeekStartingMonday());
        }
        public static int DayOfWeekStartingMonday(this DateTime d)
        {
            return (d.DayOfWeek == 0 ? 6 : (int)d.DayOfWeek - 1);
        }
    }
}

