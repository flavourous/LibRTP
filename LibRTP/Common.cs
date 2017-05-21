using System;
using System.Collections.Generic;

namespace LibRTP
{
	// I omitted none=0 from the enum to guide users.
	[Flags]
	public enum RecurrSpan : uint { Day = 1, Week = 2, Month = 4, Year = 8 }; // none is special

	public interface IRecurr
	{
		IEnumerable<DateTime> GetOccurances (DateTime Start, DateTime End);
	}

	public static class PublicHelpers
	{
		public static String AsString(this RecurrSpan spn)
		{
			switch (spn) 
			{
				case RecurrSpan.Day: return "Day";
				case RecurrSpan.Week: return "Week";
				case RecurrSpan.Month: return "Month";
				case RecurrSpan.Year: return "Year";
				default: throw new ArgumentException ();
			}
		}
        public static DateTime StartOfDay(this DateTime somit)
        {
            return new DateTime(somit.Year, somit.Month, somit.Day);
        }
        public static DateTime FirstWeekOfYear(this DateTime value)
        {
            var nval = value.AddDays(-value.DayOfYear);
            int dow = nval.DayOfWeekStartingMonday();
            if (dow > 0) // move forward to start of first week, this is the last week in previous year or part of it...
                nval = nval.AddDays(7 - dow);
            return nval;
        }
        public static int DayOfWeekStartingMonday(this DateTime d)
        {
            return (d.DayOfWeek == 0 ? 6 : (int)d.DayOfWeek - 1);
        }
    }
}

