using System;
using System.Collections.Generic;
using System.Text;

namespace LibRTP
{
	public enum AggregateRangeType { DaysFromStart = 1, DaysEitherSide = 2 }
	public class RecurringAggregatePattern
	{
		public readonly int[] DayPattern; 
		public readonly double[] DayTargets; 
		public readonly int DayTargetRange;
		public readonly AggregateRangeType DayTargetRangeType; 
		readonly int totalPatternLength = 0;
		public RecurringAggregatePattern(int targetRange, AggregateRangeType rangetype, int[] targetPattern, double[] patternTarget)
		{
			DayTargetRange = targetRange;
			DayTargetRangeType = rangetype;
			DayPattern = targetPattern;
			DayTargets = patternTarget;

			// find the total length of the pattern
			totalPatternLength = 0;
			foreach (var dp in DayPattern)
				totalPatternLength += dp;

			// f****k ok this is dumb ... lets return something here at least... no I want faily!!
			if (totalPatternLength <= 0) throw new ArgumentException("Pattern must have a positive length...");
		}
		public override string ToString ()
		{
			var ts = new String[DayTargets.Length];
			for (int i = 0; i < DayTargets.Length; i++)
				ts [i] = String.Format ("{0} days of {1}", DayPattern [i], DayTargets [i]);
			return String.Format ("{0} - averaged {1} {2}", String.Join (", then ", ts), DayTargetRange, DayTargetRangeType.ToString ());
		}
		DateTime StartOfDay(DateTime somit)
		{
			return new DateTime (somit.Year, somit.Month, somit.Day);
		}
		public DayTargetReturn FindTargetForDay(DateTime fixedStartingPoint_in, DateTime dayStart_in)
		{
			// normalise input.
			DateTime fixedStartingPoint = StartOfDay (fixedStartingPoint_in);
			DateTime dayStart = StartOfDay (dayStart_in);

			var r = DayTargetRange;
			var t = DayTargetRangeType;

			// easier to unwrap i think.
			double[] unwrapped = new double[totalPatternLength];
			int c = 0;
			for (int i = 0; i < DayTargets.Length; i++)
				for (int j = 0; j < DayPattern [i]; j++)
					unwrapped [c++] = DayTargets [i];

			//total days since started (big number)
			var daysSinceStart = (dayStart - fixedStartingPoint).TotalDays;

			// get the number of days into a pattern (could concieveably be negative)
			var daysSincePatternStarted = (int)(daysSinceStart % totalPatternLength);
			if (daysSincePatternStarted < 0) daysSincePatternStarted += totalPatternLength;

			// ok which way?
			double targ =0;
			DateTime useStart = DateTime.MinValue, useEnd = DateTime.MinValue;
			switch (t) {
			case AggregateRangeType.DaysFromStart:
				useStart = dayStart;
				useEnd = dayStart.AddDays (r);
				for (int ds = 0; ds < r; ds++)
					targ += unwrapped [(daysSincePatternStarted + ds) % totalPatternLength];
				break;
			case AggregateRangeType.DaysEitherSide:
				useStart = dayStart.AddDays (-r);
				useEnd = dayStart.AddDays (r + 1);
				for (int ds = daysSincePatternStarted - r; ds <= daysSincePatternStarted + r; ds++)
					targ += unwrapped [((ds % totalPatternLength) + totalPatternLength) % totalPatternLength];
				break;
			}
			return new DayTargetReturn (useStart, useEnd, targ);
		}
	}
	public struct DayTargetReturn {
		public readonly DateTime begin;
		public readonly DateTime end;
		public readonly double target;
		public DayTargetReturn(DateTime begin, DateTime end, double target)
		{
			this.begin=begin;
			this.end=end;
			this.target=target;
		}
		public override string ToString ()
		{
			return String.Format ("{0} to {1}: {2}", begin.ToShortDateString(), end.ToShortDateString(), target); 
		}
	}
}

