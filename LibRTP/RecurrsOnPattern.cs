using System;
using System.Collections.Generic;
using System.IO;
using LibSharpHelp;

namespace LibRTP
{
	public class OnPatternArgumentException : ArgumentException
	{
		public OnPatternArgumentException(String msg, String arg) : base(msg,arg) {}
	}
	delegate DateTime DateTimeShifter(DateTime src, int val);
	delegate DateTime DateTimeIncrementor(DateTime src);
	class RUI {
		public readonly int MaxValue;
		public readonly DateTimeShifter CreateAtValue;
		public readonly DateTimeIncrementor NextValue;
		public RUI(int mv, DateTimeShifter createAtValue,  DateTimeIncrementor nextValue) {
			MaxValue = mv;
			CreateAtValue = createAtValue;
			NextValue = nextValue;
		}
	}
	// week starts on bloddy monday, srsly.  There's probabbly a ll8n issue here.
	static class RecurrsOnPatternHelpers
	{
		// These figures define the allowed repeating "on" types - they are a lower limit that are applicabale to all months, leap years, etc.
		// In theory, though I wont do it, you can, since the pairs fall on unique indexes, use an array of length 15 (it's (biggest bit >> 1) - 1)
		public static Dictionary<RecurrSpan,RUI> Units = new Dictionary<RecurrSpan, RUI> {
			{ RecurrSpan.Year  | RecurrSpan.Month, new RUI (12, 
				(d, v) => d.AddMonths (v - d.Month),
				d => d.AddYears(1)
			)},
			{ RecurrSpan.Year  | RecurrSpan.Week, new RUI (52,
				(d, v) => d.FirstWeekOfYear().AddDays(7*(v-1)),
				d => {
					var firstWeekOfNextYear = d.AddYears(1).FirstWeekOfYear();
					var zeroBasedWeekOfFirstYear = (d.DayOfYear - d.FirstWeekOfYear().DayOfYear)/7; // should be integral.
					return firstWeekOfNextYear.AddDays(zeroBasedWeekOfFirstYear*7);
				}
			)},
			{ RecurrSpan.Year  | RecurrSpan.Day, new RUI (365, 
				(d, v) => d.AddDays (v - d.DayOfYear),
				d => d.AddYears(1)
			)},
			{ RecurrSpan.Month | RecurrSpan.Week, new RUI (4, 
				(d, v) => d.AddDays (v * 7 - d.Day),
				d => d.AddMonths(1)
			)},
			{ RecurrSpan.Month | RecurrSpan.Day, new RUI (28, 
				(d, v) => d.AddDays (v - d.Day),
				d => d.AddMonths(1)
			)},
			{ RecurrSpan.Week  | RecurrSpan.Day, new RUI (7, 
				(d, v) => d.AddDays (v - (d.DayOfWeekStartingMonday()+1) ),
				d => d.AddDays(7)
			)},
		};
	}
	public class RecurrsOnPattern : IRecurr
	{
		public readonly DateTime? PatternStarts; 
		public readonly DateTime? PatternEnds;
		public readonly IList<RecurrSpan> units;
		public readonly int[] onIndexes;

		// for each flag in the unitsMask, we take and index and say of.  
		// on the  idx[0] flag[0] of the idx[1] flag[1] of the idx[2] flag[2] of the flag[3]
		// eg on the 1st day of the 2nd week of the 7th month of the year.
		// so there must always be one more flag set than length of indexes.
		public RecurrsOnPattern (int[] onIndexes, RecurrSpan unitsMask, DateTime? patternStart, DateTime? patternEnd)
		{
			// assert validity
			OnPatternArgumentException error;
			if(!IsValid(onIndexes,unitsMask, patternStart, patternEnd, out error))
				throw error;
			
			// Ok, we can give you an object.
			this.units = new List<RecurrSpan> (unitsMask.SplitFlags ());
			this.onIndexes = onIndexes;
			PatternStarts = patternStart;
			PatternEnds = patternEnd;
		}

		static bool IsValid(int[] onIndexes, RecurrSpan unitsMask, DateTime? patternStart, DateTime? patternEnd)
		{
			OnPatternArgumentException ex;
			return IsValid (onIndexes, unitsMask, patternStart, patternEnd, out ex);
		}
		static bool IsValid(int[] onIndexes, RecurrSpan unitsMask, DateTime? patternStart, DateTime? patternEnd, out OnPatternArgumentException error)
		{
			if (onIndexes.Length == 0) {
				error = new OnPatternArgumentException ("There must be some indexes specified", "onIndexes.Length");
				return false;
			}

			// Get split list of flags (backing down not dealing directly with the mask :-(, but cant think how right now)
			List<RecurrSpan> unitsLocal = new List<RecurrSpan> (unitsMask.SplitFlags ());

			// there must be an "onindex" for each present in the onUnitsMask, not more not less.
			if (unitsLocal.Count - 1 != onIndexes.Length) {
				error = new OnPatternArgumentException ("Number of indexes be one less than number of flags set in the mask. (1,3) (day|year) means first day of year every 3 years or (4,5)(week|month) start of every 4th week of every fifth month. ", "unitsMask");
				return false;
			}
			// we gotta make sure that each "on" staisfies the allowable maximum for the span type.
			for (int i = 0; i < unitsLocal.Count - 1; i++)
				if (onIndexes [i] > RecurrsOnPatternHelpers.Units [unitsLocal [i] | unitsLocal [i + 1]].MaxValue || onIndexes [i] <= 0) {
					error = new OnPatternArgumentException ("cant repeat on the " + onIndexes [i] + " " + unitsLocal [i] + " of every " + unitsLocal [i + 1], "onIndexes[" + i + "]");
					return false;
				}
			error = null;
			return true;
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
					umask |= (uint)um;
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
		public static bool TryFromBinary(byte[] data, out IRecurr value)
		{
			using (var ms = new MemoryStream (data))
			using (var br = new StreamReadingHelper (ms)) {
				value = null;

				int indexes; if(!br.TryRead(out indexes)) return false;

				List<int> onIndexes = new List<int> ();
				for (int i = 0; i < indexes; i++)
					if (!br.TryRead<int> (onIndexes.Add))
						return false;

				uint umask; if (!br.TryRead (out umask)) return false;

				bool has;
				DateTime? ps= null, pe = null;
				if (!br.TryRead (out has)) return false;
				if (has && !br.TryRead ((long dt) => ps = DateTime.FromBinary (dt)))
					return false;
				if (!br.TryRead (out has)) return false;
				if (has && !br.TryRead ((long dt) => pe = DateTime.FromBinary (dt)))
					return false;
				
				if(!RecurrsOnPattern.IsValid(onIndexes.ToArray(),  (RecurrSpan)umask, ps, pe))
					return false;

				value = new RecurrsOnPattern (onIndexes.ToArray (), (RecurrSpan)umask, ps, pe);
				return true;
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
			for (int i = onIndexes.Length-1; i >=0;i--)
				forming = RecurrsOnPatternHelpers.Units [units [i+1] | units [i]].CreateAtValue (forming, onIndexes [i]);

			Action Incrementor = () => {
				bool inc = true;
				for (int i = onIndexes.Length-1; i >=0;i--) 
				{
					var use = RecurrsOnPatternHelpers.Units[units [i+1] | units [i]];
					if(inc) forming = startTrack = use.NextValue(startTrack);
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
			while(forming < formEnd)
			{
				yield return forming;
				Incrementor ();
			} 
		}
		#endregion
	}
}