using System;
using System.Collections.Generic;
};

namespace LibRTP
{
	public class RecurringAggregatePattern
	{
		public DateTime FixedStartingPoint;
		public enum RangeType { DaysFromStart = 1, DaysEitherSide = 2 }
		public readonly int[] DayPattern; 
		public readonly double[] DayTargets; 
		public readonly int DayTargetRange;
		public readonly RangeType DayTargetRangeType; 
		public RecurringAggregatePattern(DateTime fixedStartingPoint, int targetRange, RangeType rangetype, int[] targetPattern, double[] patternTarget)
		{
			FixedStartingPoint = fixedStartingPoint;
			DayTargetRange = targetRange;
			DayTargetRangeType = rangetype;
			DayPattern = targetPattern;
			DayTargets = patternTarget;
		}
		public DayTargetReturn FindTargetForDay(DateTime dayStart)
		{
			var r = DayTargetRange;
			var t = DayTargetRangeType;

			// find the total length of the pattern
			int totalPatternLength = 0;
			foreach (var dp in DayPattern)
				totalPatternLength += dp;

			// f****k ok this is dumb ... lets return something here at least...
			if (totalPatternLength == 0)
				return new DayTargetReturn (dayStart, dayStart.AddDays (1), DayTargets.Length > 0 ? DayTargets [0] : 0.0);

			// easier to unwrap i think.
			double[] unwrapped = new double[totalPatternLength];
			int c = 0;
			for (int i = 0; i < DayTargets.Length; i++)
				for (int j = 0; j < DayPattern [i]; j++)
					unwrapped [c++] = DayTargets [i];

			//total days since started (big number)
			var daysSinceStart = (dayStart - FixedStartingPoint).TotalDays;

			// get the number of days into a pattern (could concieveably be negative)
			var daysSincePatternStarted = (int)(daysSinceStart % totalPatternLength);
			if (daysSincePatternStarted < 0) daysSincePatternStarted += totalPatternLength;

			// ok which way?
			double targ =0;
			DateTime useStart = DateTime.MinValue, useEnd = DateTime.MinValue;
			switch (t) {
			case RecurringAggregatePattern.RangeType.DaysFromStart:
				useStart = dayStart.AddDays (-daysSincePatternStarted);
				useEnd = dayStart.AddDays (r);
				for (int ds = 0; ds < r; ds++)
					targ += unwrapped [ds % totalPatternLength];
				break;
			case RecurringAggregatePattern.RangeType.DaysEitherSide:
				useStart = dayStart.AddDays (-r);
				useEnd = dayStart.AddDays (r);
				var rs = daysSincePatternStarted + r;
				for (int ds = daysSincePatternStarted - r; ds <= rs; ds++)
					targ += unwrapped [Math.Abs(ds % totalPatternLength)];
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
	}
}

