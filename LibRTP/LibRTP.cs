using System;

// We will deal with thse situations - I think they may be generalisable.

// 1) Recurrance pattern.  Given a start (and possibly ending/truncation) date, specify one of these patterns:
//				-- Repeat every N Days/Weeks/Months/Years
//				-- Repeat on the Ith Day/Week/Month of each Week/Month/Year
// the lib should be able to be queried for occurances with a daterange

// 2) Region Patterns. Given a initial date (and a "rounding" param i guess), and an array of [ SpanUnit, SpanNumber ],
//		the lib should be queriable for the array index from a input date corresponding to the position on that mask.  Rounding would
//		simply help if specced by moving the initial date to a round number of the units of the first array type.
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;

namespace LibRTP
{
	// I omitted none=0 from the enum to guide users.
	[Flags]
	public enum RecurrSpan : uint { Day = 1, Week = 2, Month = 4, Year = 8 }; // none is special
	delegate DateTime DateTimeShifter(DateTime src, int val);
	class RUI {
		public readonly int MaxValue;
		public readonly DateTimeShifter CreateAtValue, NextValue;
		public RUI(int mv, DateTimeShifter createAtValue,  DateTimeShifter nextValue) {
			MaxValue = mv;
			CreateAtValue = createAtValue;
			NextValue = nextValue;
		}
	}
	delegate DateTime DateTimeMover(DateTime Fixed, DateTime At, int frequency);
	class RMOD {
		public readonly DateTimeMover ClosestToAt;
		public readonly DateTimeMover NextValue;
		public RMOD(DateTimeMover ClosestToAt, DateTimeMover NextValue)
		{
			this.ClosestToAt = ClosestToAt;
			this.NextValue = NextValue;
		}
	}
	// week starts on bloddy monday, srsly.  There's probabbly a ll8n issue here.
	static class Helpers
	{
		// this one is the the "every" types.  We have a method to modulo the closest repetition from a fixed date to a chosen date
		public static Dictionary<RecurrSpan,RMOD> Moduliser = new Dictionary<RecurrSpan, RMOD> {
			{ RecurrSpan.Day, new RMOD(
				(fixedDate, atDate, f) => atDate.AddDays((int)(atDate - fixedDate).TotalDays % f),
				(d, f, v) => d.AddDays(v)
			)},
			{ RecurrSpan.Week, new RMOD(
				(fixedDate, atDate, f) => atDate.AddDays((int)(atDate - fixedDate).TotalDays % 7*f),
				(d,f, v) => d.AddDays(v*7)
			)},
			{ RecurrSpan.Month, new RMOD(
				(fixedDate, atDate, f) => 
				{
					// difference in years.
					int ydiff = atDate.Year - fixedDate.Year;
					int monthdiff;
					if(ydiff == 0) monthdiff = fixedDate.Month - atDate.Month; // simple case.
					else 
					{
						// months in complete years apart
						monthdiff = 12*(ydiff-1);
						// add the months in the 2 years that fall on the dates
						if(ydiff > 0) monthdiff += 12 - fixedDate.Month + atDate.Month;
						if(ydiff < 0) monthdiff -= 12 - atDate.Month + fixedDate.Month; // its also a negative value.
					}
					DateTime closestMonth = new DateTime(atDate.Year, atDate.Month, fixedDate.Day);
					atDate.AddMonths(monthdiff % f);
					return closestMonth;
				},
				(d,f, v) => {
					var ret = d.AddMonths(v);
					if(ret.Day != f.Day)
					{
						var aret = ret.AddDays(f.Day-ret.Day);
						if(aret.Month == ret.Month)
							ret=aret;
					}
					return ret;
				}
			)},
			{ RecurrSpan.Year, new RMOD(
				(fixedDate, atDate, f) => {
					int closestYear =atDate.Year+ (atDate.Year - fixedDate.Year) % f;
					return new DateTime(closestYear, 1,1).AddDays(fixedDate.DayOfYear - 1);
				}, 
				(d,f,v) => {
					var ret = d.AddYears(v);
					if(f.DayOfYear != ret.DayOfYear)
					{
						var aret = ret.AddDays(f.DayOfYear - ret.DayOfYear);
							if(aret.Year == ret.Year)
								ret=aret;
					}
					return ret;
				}
			)}
		};
		// These figures define the allowed repeating "on" types - they are a lower limit that are applicabale to all months, leap years, etc.
		// In theory, though I wont do it, you can, since the pairs fall on unique indexes, use an array of length 15 (it's (biggest bit >> 1) - 1)
		public static Dictionary<RecurrSpan,RUI> Units = new Dictionary<RecurrSpan, RUI> {
			{ RecurrSpan.Year  | RecurrSpan.Month, new RUI (12, 
				(d, v) => d.AddMonths (v - d.Month),
				(d,n) => d.AddYears(n)
			)},
			{ RecurrSpan.Year  | RecurrSpan.Week, new RUI (52,
				(d, v) => d.FirstWeekOfYear().AddDays(7*(v-1)),
				(d,n) => {
					var firstWeekOfNextYear = d.AddYears(n).FirstWeekOfYear();
					var zeroBasedWeekOfFirstYear = (d.DayOfYear - d.FirstWeekOfYear().DayOfYear)/7; // should be integral.
					return firstWeekOfNextYear.AddDays(zeroBasedWeekOfFirstYear*7);
				}
			)},
			{ RecurrSpan.Year  | RecurrSpan.Day, new RUI (365, 
				(d, v) => d.AddDays (v - d.DayOfYear),
				(d,n) => d.AddYears(n)
			)},
			{ RecurrSpan.Month | RecurrSpan.Week, new RUI (4, 
				(d, v) => d.AddDays (v * 7 - d.Day),
				(d,n) => d.AddMonths(n)
			)},
			{ RecurrSpan.Month | RecurrSpan.Day, new RUI (28, 
				(d, v) => d.AddDays (v - d.Day),
				(d, n) => d.AddMonths(n)
			)},
			{ RecurrSpan.Week  | RecurrSpan.Day, new RUI (7, 
				(d, v) => d.AddDays (v - (d.DayOfWeekStartingMonday()+1) ),
				(d, n) => d.AddDays(n*7)
			)},
		};
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
	public interface IRecurr
	{
		IEnumerable<DateTime> GetOccurances (DateTime Start, DateTime End);
	}
	public class RecurrsEveryPattern : IRecurr
	{
		// Thse defaults are nice.
		readonly DateTime FixedPoint; 
		readonly DateTime? lowerBound, upperBound;
		readonly int frequency;
		readonly RecurrSpan units;

		public RecurrsEveryPattern(DateTime fixedPoint, int frequency, RecurrSpan units, DateTime? lowerBound, DateTime? upperBound)
		{
			this.FixedPoint = fixedPoint;
			this.frequency = frequency;
			this.units = units;
			this.lowerBound = lowerBound;
			this.upperBound = upperBound;
		}
		public byte[] ToBinary()
		{
			using (MemoryStream ms = new MemoryStream ())
			using (BinaryWriter bw = new BinaryWriter (ms)) {
				bw.Write (FixedPoint.ToBinary ());
				bw.Write (frequency);
				bw.Write ((uint)units);
				bw.Write (lowerBound.HasValue);
				if (lowerBound.HasValue)
					bw.Write (lowerBound.Value.ToBinary ());
				bw.Write (upperBound.HasValue);
				if (upperBound.HasValue)
					bw.Write (upperBound.Value.ToBinary ());
				return ms.ToArray ();
			}
		}
		public static RecurrsEveryPattern FromBinary(byte[] data)
		{
			using (MemoryStream ms = new MemoryStream (data))
			using (BinaryReader br = new BinaryReader (ms)) {
				var fp = DateTime.FromBinary (br.ReadInt64 ());
				var frq = br.ReadInt32 ();
				var units = (RecurrSpan)br.ReadUInt32 ();
				DateTime? lb = null, ub = null;
				if (br.ReadBoolean ())
					lb = DateTime.FromBinary (br.ReadInt64 ());
				if (br.ReadBoolean ())
					ub = DateTime.FromBinary (br.ReadInt64 ());
				return new RecurrsEveryPattern (fp, frq, units, lb, ub);
			}
		}
		#region IRecurr implementation
		public IEnumerable<DateTime> GetOccurances (DateTime Start, DateTime End)
		{
			DateTime useStart = lowerBound.HasValue ? Start > lowerBound.Value ? Start : lowerBound.Value : Start;
			DateTime useEnd = upperBound.HasValue ? End < upperBound.Value ? End : upperBound.Value : End;
			DateTime forming = Helpers.Moduliser [units].ClosestToAt (FixedPoint, useStart, frequency);
			if (forming < Start)
				forming = Helpers.Moduliser [units].NextValue (forming, FixedPoint, frequency);
			while (forming <= useEnd) {
				yield return forming;
				forming = Helpers.Moduliser [units].NextValue (forming, FixedPoint, frequency);
			}
		}
		#endregion
	}
	public class RecurrsOnPattern : IRecurr
	{
		readonly DateTime? PatternStarts; 
		readonly DateTime? PatternEnds;
		readonly IList<RecurrSpan> units;
		readonly int[] onIndexes;

		// for each flag in the unitsMask, we take and index and say of.  
		// on the  idx[0] flag[0] of the idx[1] flag[1] of the idx[2] flag[2] of the flag[3]
		// eg on the 1st day of the 2nd week of the 7th month of the year.
		// so there must always be one more flag set than length of indexes.
		public RecurrsOnPattern (int[] onIndexes, RecurrSpan unitsMask, DateTime? patternStart, DateTime? patternEnd)
		{
			if(onIndexes.Length == 0) throw new ArgumentException("There must be some indexes specified", "onIndexes.Length");

			// Get split list of flags (backing down not dealing directly with the mask :-(, but cant think how right now)
			List<RecurrSpan> unitsLocal = new List<RecurrSpan> (unitsMask.SplitFlags ());

			// there must be an "onindex" for each present in the onUnitsMask, not more not less.
			if(unitsLocal.Count != onIndexes.Length) throw new ArgumentException("Number of indexes be equal to number of flags set in the mask. (1,3) (day|year) means first day of year every 3 years or (4,5)(week|month) start of every 4th week of every fifth month. ", "unitsMask");
			// we gotta make sure that each "on" staisfies the allowable maximum for the span type.
			for (int i = 0; i < unitsLocal.Count - 1; i++)
				if (onIndexes [i] > Helpers.Units [unitsLocal [i] | unitsLocal [i + 1]].MaxValue || onIndexes[i] <=0)
					throw new ArgumentException ("cant repeat on the " + onIndexes [i] + " " + unitsLocal[i] + " of every " + unitsLocal [i + 1], "onIndexes[" + i + "]");

			// Ok, we can give you an object.
			this.units = unitsLocal;
			this.onIndexes = onIndexes;
			PatternStarts = patternStart;
			PatternEnds = patternEnd;
		}

		public byte[] ToBinary()
		{
			using (var ms = new MemoryStream ())
			using (var bw = new BinaryWriter (ms)) {
				bw.Write (onIndexes.Length);
				for(int i=0;i<onIndexes.Length;i++)
					bw.Write (onIndexes[i]);
				uint umask = 0;
				foreach (var um in units)
					umask |= um;
				bw.Write (umask);
				bw.Write (PatternStarts.HasValue);
				if (PatternStarts.HasValue)
					bw.Write (PatternStarts.Value.ToBinary ());
				bw.Write (PatternEnds.HasValue);
				if (PatternEnds.HasValue)
					bw.Write (PatternEnds.Value.ToBinary ());
				return ms.ToArray ();
			}
		}
		public static RecurrsOnPattern FromBinary(byte[] data)
		{
			using (var ms = new MemoryStream (data))
			using (var br = new BinaryReader (ms)) {
				int indexes = br.ReadInt32 ();
				List<int> onIndexes = new List<int> ();
				for (int i = 0; i < indexes; i++)
					onIndexes.Add (br.ReadInt32 ());
				uint umask = br.ReadUInt32 ();
				DateTime? ps= null, pe = null;
				if (br.ReadBoolean ())
					ps = DateTime.FromBinary (br.ReadInt64 ());
				if (br.ReadBoolean ())
					pe = DateTime.FromBinary (br.ReadInt64 ());
				return new RecurrsOnPattern (onIndexes.ToArray (), (RecurrSpan)umask, ps, pe);
			}
		}

		#region IRecurr implementation
		public IEnumerable<DateTime> GetOccurances (DateTime Start, DateTime end)
		{
			// Firstly, we might be before the pattern actually starts, limit that.
			if(PatternStarts.HasValue && Start < PatternStarts.Value) Start = PatternStarts.Value;

			// go back through the indexes - we spam datetimes here, there should be a better way.
			// we are computing like: "Set month of year to x, then set week of month to y, then set day of week to z"
			// or if you like with one less flag eg. "Set month of year to x, then set day of month to y" 
			// and any other combinaton you can think of.
			DateTime forming = Start;
			DateTime startTrack = Start;
			for (int i = onIndexes.Length-2; i >=0;i--) // last one is the frequency multiplier
				forming = Helpers.Units [units [i+1] | units [i]].CreateAtValue (forming, onIndexes [i]);

			int frequency = onIndexes [onIndexes.Length - 1];

			Action Incrementor = () => {
				bool inc = true;
				for (int i = onIndexes.Length-2; i >=0;i--) // last one is the frequency multiplier
				{
					var use = Helpers.Units[units [i+1] | units [i]];
					if(inc) forming = startTrack = use.NextValue(startTrack,frequency);
					forming = use.CreateAtValue(forming, onIndexes[i]);
					inc = false; // now use createat to position correctly.
				}
			};

			// this gets a candidate start value, that might actually be before start, in which case we need to increment by one.
			if(forming < Start) Incrementor();

			// then we go untill we pass this ending value
			var formEnd = end;
			if (PatternEnds.HasValue && formEnd > PatternEnds.Value) formEnd = PatternEnds.Value;

			// by consecutive increment and yielding
			while(forming <= formEnd)
			{
				yield return forming;
				Incrementor ();
			} 
		}
		#endregion
	}
}