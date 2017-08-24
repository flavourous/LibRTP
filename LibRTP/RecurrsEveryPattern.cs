﻿using System;
using System.Collections.Generic;
using System.IO;
using LibSharpHelp;

namespace LibRTP
{
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
	static class RecurrsEveryPatternHelpers
	{
		// this one is the the "every" types.  We have a method to modulo the closest repetition from a fixed date to a chosen date
		public static Dictionary<RecurrSpan,RMOD> Moduliser = new Dictionary<RecurrSpan, RMOD> {
			{ RecurrSpan.Day, new RMOD(
				(fixedDate, atDate, f) => 
                {
                    var td=  fixedDate.StartOfDay()-atDate.StartOfDay();
                    return atDate.AddDays((int)(td).TotalDays % f).Add(fixedDate.TimeOfDay);
                },
				(d, f, v) => d.AddDays(v)
			)},
			{ RecurrSpan.Week, new RMOD(
				(fixedDate, atDate, f) => 
                {
                    var tdd =  fixedDate.StartOfDay()-atDate.StartOfDay();
                    var rem = tdd.TotalDays % (7*f);
                    return atDate.AddDays(rem).Add(fixedDate.TimeOfDay);
                },
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
					return new DateTime(atDate.Year, atDate.Month, fixedDate.Day).Add(fixedDate.TimeOfDay);
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
					return new DateTime(closestYear, 1,1).AddDays(fixedDate.DayOfYear - 1).Add(fixedDate.TimeOfDay);
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
	}
	public class RecurrsEveryPattern : IRecurr
	{
		// Thse defaults are nice.
		public readonly DateTime FixedPoint; 
		public readonly DateTime? lowerBound, upperBound;
		public readonly int frequency;
		public readonly RecurrSpan units;

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
		public static bool TryFromBinary(byte[] data, out IRecurr value)
		{
			using (MemoryStream ms = new MemoryStream (data))
			using (var br = new StreamReadingHelper(ms)) {
				value = null;

				DateTime fp = DateTime.MinValue; 
				if (!br.TryRead ((long fpv) => fp = DateTime.FromBinary (fpv)))
					return false;
				
				int frq;
				if (!br.TryRead (out frq))
					return false;

				uint units; if (!br.TryRead (out units)) return false;

				bool has;
				DateTime? lb= null, ub = null;
				if (!br.TryRead (out has)) return false;
				if (has && !br.TryRead ((long dt) => lb = DateTime.FromBinary (dt)))
					return false;
				if (!br.TryRead (out has)) return false;
				if (has && !br.TryRead ((long dt) => ub = DateTime.FromBinary (dt)))
					return false;

				value = new RecurrsEveryPattern (fp, frq, (RecurrSpan)units, lb, ub);
				return true;
			}
		}
		#region IRecurr implementation
		public IEnumerable<DateTime> GetOccurances (DateTime Start, DateTime End)
		{
			DateTime useStart = lowerBound.HasValue ? Start > lowerBound.Value ? Start : lowerBound.Value : Start;
			DateTime useEnd = upperBound.HasValue ? End < upperBound.Value ? End : upperBound.Value : End;
			DateTime forming = RecurrsEveryPatternHelpers.Moduliser [units].ClosestToAt (FixedPoint, useStart, frequency);
			if (forming < Start)
				forming = RecurrsEveryPatternHelpers.Moduliser [units].NextValue (forming, FixedPoint, frequency);
			while (forming < useEnd) {
				yield return forming;
				forming = RecurrsEveryPatternHelpers.Moduliser [units].NextValue (forming, FixedPoint, frequency);
			}
		}
		#endregion
	}
}

