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
		// check the number of bits present in the flag...
		public static IEnumerable<RecurrSpan> SplitFlags(this RecurrSpan value)
		{
			// BitMasks 101: 0 | 0 == 0, we cant pick up on that "flag" it's the implicit none. 
			uint current = (uint)value, compare = 1;
			while (current != 0) {
				if ((current & compare) != 0) {
					yield return (RecurrSpan)compare;
					current -= compare; 
				}
				compare *= 2; // next flag...
			}
		}
	}

	static class OurHelpers
	{
		public static DateTime FirstWeekOfYear(this DateTime value)
		{
			var nval = value.AddDays (-value.DayOfYear);
			int dow = nval.DayOfWeekStartingMonday();
			if (dow > 0) // move forward to start of first week, this is the last week in previous year or part of it...
				nval = nval.AddDays(7-dow);
			return nval;
		}
		public static int DayOfWeekStartingMonday(this DateTime d)
		{
			return (d.DayOfWeek == 0 ? 6 : (int)d.DayOfWeek - 1);
		}
	}
}

