using System;
using System.Collections.Generic;
using System.IO;

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
			DateTime forming = RecurrsEveryPatternHelpers.Moduliser [units].ClosestToAt (FixedPoint, useStart, frequency);
			if (forming < Start)
				forming = RecurrsEveryPatternHelpers.Moduliser [units].NextValue (forming, FixedPoint, frequency);
			while (forming <= useEnd) {
				yield return forming;
				forming = RecurrsEveryPatternHelpers.Moduliser [units].NextValue (forming, FixedPoint, frequency);
			}
		}
		#endregion
	}
}

