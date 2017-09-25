using System;
using System.Collections.Generic;

namespace LibRTP
{
    [Flags]
	public enum RSpan : uint { None =0,  Minute = 1, Hour=2, Day = 4, Week = 8, Month = 16, Year = 32 }; // none is special

	public static class PublicHelpers
	{
		public static String AsString(this RSpan spn)
		{
			switch (spn) 
			{
                case RSpan.Minute: return "Minute";
                case RSpan.Hour: return "Hour";
                case RSpan.Day: return "Day";
				case RSpan.Week: return "Week";
				case RSpan.Month: return "Month";
				case RSpan.Year: return "Year";
				default: throw new ArgumentException ();
			}
		}
        public static DateTime StartOfDay(this DateTime somit)
        {
            return new DateTime(somit.Year, somit.Month, somit.Day);
        }
        public static DateTime FirstDayOfWeek(this DateTime value)
        {
            var d1 = new DateTime(value.Year, value.Month, value.Day);
            return d1.AddDays(-d1.DayOfWeekStartingMonday());
        }
        public static int DayOfWeekStartingMonday(this DateTime d)
        {
            return (d.DayOfWeek == 0 ? 6 : (int)d.DayOfWeek - 1);
        }
    }
}

