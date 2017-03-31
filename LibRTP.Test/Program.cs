using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;

namespace LibRTP.Test
{
	class MainClass
	{
		static readonly String[] green = new[] {
			"      xxxxxx           xxxxxxxx            xxxxxxxxxxxxxxx    xxxxxxxxxxxxxxx    xxxxxx          xxxxx",
			"    xxxxxxxxxx         xxxxxxxxxx          xxxxxxxxxxxxxxx    xxxxxxxxxxxxxxx    xxxxxxxx        xxxxx",
			" xxxxx      xxxxx      xxxxx  xxxxxx       xxxxx              xxxxx              xxxxxxxxxx      xxxxx",
			"xxxxx       xxxxx      xxxxx     xxxxx     xxxxx              xxxxx              xxxxx xxxxx     xxxxx",
			"xxxxx                  xxxxx      xxxxx    xxxxxxxxx          xxxxxxxxx          xxxxx  xxxxx    xxxxx",
			"xxxxx   xxxxxxx        xxxxx     xxxxx     xxxxxxxxxx         xxxxxxxxxx         xxxxx   xxxxx   xxxxx",
			"xxxxx     xxxxxxx      xxxxxx  xxxxx       xxxxxxxxx          xxxxxxxxx          xxxxx    xxxxx  xxxxx",
			"xxxxx        xxxxx     xxxxxxxxxxxxxx      xxxxx              xxxxx              xxxxx     xxxxx xxxxx",
			" xxxxx      xxxxx      xxxxxx    xxxxx     xxxxx              xxxxx              xxxxx      xxxxxxxxxx",
			"    xxxxxxxxxx         xxxxx      xxxxx    xxxxxxxxxxxxxxx    xxxxxxxxxxxxxxx    xxxxx        xxxxxxxx",
			"      xxxxxx           xxxxx       xxxx    xxxxxxxxxxxxxxx    xxxxxxxxxxxxxxx    xxxxx          xxxxxx"
		};
		public static void Main (string[] args)
		{
			// Ad hock "testing framework"
			var opt = new OnOfPatternTests ();
			opt.RunTests ();
			var ept = new EveryPatternTests ();
			ept.RunTests ();
			var apt = new AggregatePatternTests ();
			apt.RunTests ();

			// show happy green screen!
			int ctr = 0;
			DrawGreen ();
			Console.ForegroundColor = hl; 
			while (true) {
				UpdateGreen (ctr,ctr-4);
				if (Console.KeyAvailable) break;
				ctr += 4;
				if (ctr > green [0].Length) {
					ctr = 0;
					Console.ForegroundColor = Console.ForegroundColor == hl ? nrm : hl;
				}
				Thread.Sleep (30);
			}
		}
		static ConsoleColor nrm = ConsoleColor.Black, hl = ConsoleColor.Green;
		static void DrawGreen()
		{
			Console.CursorVisible = false;
			Console.ResetColor ();
			Console.BackgroundColor = ConsoleColor.DarkGreen;
			Console.ForegroundColor = nrm;
			Console.Clear ();
			int vofs = (Console.WindowHeight - green.Length)/2;
			for (int i = 0; i < vofs; i++)
				Console.WriteLine ();
			int hofs = (Console.WindowWidth - green [0].Length)/2;
			foreach (var l in green)
				Console.WriteLine (new String (' ', hofs) + l);
		}
		static void UpdateGreen(int pos, int upos)
		{
			int vofs = (Console.WindowHeight - green.Length)/2;
			int hofs = (Console.WindowWidth - green [0].Length)/2;

			int rn = pos - upos;
			Doit (pos, rn, hofs, vofs);
		}
		static void Doit(int start, int ln, int hofs, int vofs)
		{
			var uln = Math.Min (ln, green [0].Length - start);
			if (start >= 0 && uln > 0)
				for (int i = 0; i < green.Length; i++) {
					Console.SetCursorPosition (hofs + start, vofs + i);
					Console.Write (green [i].Substring (start, uln));
				}
		}
	}

	class AggregatePatternTests
	{
		class AggTest 
		{
			public RecurringAggregatePattern pat;
			public DayTargetReturn[] res;
			public DateTime fixedDate;
			public DateTime[] startDate;
		}
		class AggList : List<AggTest>
		{
			public void Add(RecurringAggregatePattern pat,DateTime fixedDate, DateTime startDate, DayTargetReturn res)
			{
				Add (new AggTest () { pat = pat, fixedDate = fixedDate, startDate = new[] { startDate }, res = new[] { res } });
			}
			public void Add(RecurringAggregatePattern pat,DateTime fixedDate, DateTime[] startDates, DayTargetReturn[] ress)
			{
				Add (new AggTest () { pat = pat, fixedDate = fixedDate, startDate = startDates, res = ress });
			}
		}
		AggList tests = new AggList 
		{ 
			//some simple tests.
			{ 
				new RecurringAggregatePattern (1, AggregateRangeType.DaysFromStart, new[] { 1 }, new[] { 100.0 }),
				new DateTime (2015, 1, 1), new DateTime (2015, 1, 1),
				new DayTargetReturn (new DateTime (2015, 1, 1), new DateTime (2015, 1, 2), 100.0)
			},
			{ 
				new RecurringAggregatePattern (7, AggregateRangeType.DaysFromStart, new[] { 1 }, new[] { 100.0 }),
				new DateTime (2015, 1, 1), new DateTime (2015, 1, 1),
				new DayTargetReturn (new DateTime (2015, 1, 1), new DateTime (2015, 1, 8), 700.0)
			},
			{   
				new RecurringAggregatePattern (0, AggregateRangeType.DaysEitherSide, new[] { 1 }, new[] { 100.0 }),
				new DateTime (2015, 1, 1), new DateTime (2015, 1, 1),
				new DayTargetReturn (new DateTime (2015, 1, 1), new DateTime (2015, 1, 2), 100.0)
			},
			{   
				new RecurringAggregatePattern (3, AggregateRangeType.DaysEitherSide, new[] { 1 }, new[] { 100.0 }),
				new DateTime (2015, 1, 1), new DateTime (2015, 1, 1),
				new DayTargetReturn (new DateTime (2015, 1, 1).AddDays(-3), new DateTime (2015, 1, 2).AddDays(3), 700.0)
			},

			// Testing patterning is working.
			{   
				new RecurringAggregatePattern (1, AggregateRangeType.DaysFromStart, new[] { 3,2 }, new[] { 100.0, 50.0 }), 
				new DateTime (2015, 1, 1),
				new[] 
				{ 
					new DateTime (2015, 1, 1), 
					new DateTime (2015, 1, 2), 
					new DateTime (2015, 1, 3), 
					new DateTime (2015, 1, 4), 
					new DateTime (2015, 1, 5),
					new DateTime (2014, 12, 31), 
					new DateTime (2015, 1, 25),
					new DateTime (2014, 12, 29), 
				},
				new[] 
				{
					new DayTargetReturn (new DateTime (2015, 1, 1), new DateTime (2015, 1, 2), 100.0),
					new DayTargetReturn (new DateTime (2015, 1, 2), new DateTime (2015, 1, 3), 100.0),
					new DayTargetReturn (new DateTime (2015, 1, 3), new DateTime (2015, 1, 4), 100.0),
					new DayTargetReturn (new DateTime (2015, 1, 4), new DateTime (2015, 1, 5), 50.0),
					new DayTargetReturn (new DateTime (2015, 1, 5), new DateTime (2015, 1, 6), 50.0),
					new DayTargetReturn (new DateTime (2014, 12, 31), new DateTime (2015, 1, 1), 50.0),
					new DayTargetReturn (new DateTime (2015, 1, 25), new DateTime (2015, 1, 26), 50.0),
					new DayTargetReturn (new DateTime (2014, 12, 29), new DateTime (2014, 12, 30), 100.0),
				}
			},

			// Testing patterning combined with windowing
			{   
				new RecurringAggregatePattern (5, AggregateRangeType.DaysFromStart, new[] { 2,1 }, new[] { 100.0, 50.0 }), 
				new DateTime (2015, 1, 1),
				new[] 
				{ 
					new DateTime (2015, 1, 1), 
					new DateTime (2015, 1, 2), 
					new DateTime (2015, 1, 3), 
				},
				new[] 
				{
					new DayTargetReturn (new DateTime (2015, 1, 1), new DateTime (2015, 1, 6), 450.0),
					new DayTargetReturn (new DateTime (2015, 1, 2), new DateTime (2015, 1, 7), 400.0),
					new DayTargetReturn (new DateTime (2015, 1, 3), new DateTime (2015, 1, 8), 400.0),
				}
			},
			{   
				new RecurringAggregatePattern (4, AggregateRangeType.DaysEitherSide, new[] { 2,1 }, new[] { 100.0, 50.0 }), 
				new DateTime (2015, 1, 1),
				new[] 
				{ 
					new DateTime (2015, 1, 1), 
					new DateTime (2015, 1, 2), 
					new DateTime (2015, 1, 3), 
				},
				new[] 
				{
					new DayTargetReturn (new DateTime (2014, 12, 28), new DateTime (2015, 1, 6), 750.0),
					new DayTargetReturn (new DateTime (2014, 12, 29), new DateTime (2015, 1, 7), 750.0),
					new DayTargetReturn (new DateTime (2014, 12, 30), new DateTime (2015, 1, 8), 750.0),
				}
			},

			// testing dates before start date and windowings+patterns
			{   
				new RecurringAggregatePattern (1, AggregateRangeType.DaysEitherSide, new[] { 1,2,3,1 }, new[] { 10.0, 20.0, 30.0, 40.0 }), 
				new DateTime (2015, 1, 31),
				new[] 
				{ 
					new DateTime (2015, 1, 17), 
					new DateTime (2015, 1, 16), 
					new DateTime (2015, 1, 15), 
					new DateTime (2015, 1, 14), 
					new DateTime (2015, 1, 13), 
					new DateTime (2015, 1, 12), 
					new DateTime (2015, 1, 11), 
				},
				new[] 
				{
					new DayTargetReturn (new DateTime (2015, 1, 17-1), new DateTime (2015, 1, 17+2), 70.0),
					new DayTargetReturn (new DateTime (2015, 1, 16-1), new DateTime (2015, 1, 16+2), 80.0),
					new DayTargetReturn (new DateTime (2015, 1, 15-1), new DateTime (2015, 1, 15+2), 100.0),
					new DayTargetReturn (new DateTime (2015, 1, 14-1), new DateTime (2015, 1, 14+2), 90.0),
					new DayTargetReturn (new DateTime (2015, 1, 13-1), new DateTime (2015, 1, 13+2), 80.0),
					new DayTargetReturn (new DateTime (2015, 1, 12-1), new DateTime (2015, 1, 12+2), 70.0),
					new DayTargetReturn (new DateTime (2015, 1, 11-1), new DateTime (2015, 1, 11+2), 50.0),
				}
			},
		};
		public void RunTests()
		{
			RunTest (tests [tests.Count-1], 0);
			foreach (var t in tests) 
				for (int i = 0; i < t.res.Length; i++)
					RunTest (t, i);
		}
		void RunTest(AggTest t, int i)
		{
			var res = t.res [i]; var sd = t.startDate [i];
			var calc = t.pat.FindTargetForDay (t.fixedDate, sd);
			String info = String.Format ("   Setup: {0}\n   Input: fixed at {1}, on day {2}\nExpected: {3}\n  Result: {4}", 
				             t.pat.ToString (), t.fixedDate.ToShortDateString (), sd.ToShortDateString (), res, calc);
			if (!(calc.begin == res.begin && calc.end == res.end && calc.target == res.target)) {
				Console.WriteLine (info);
				Debug.Assert (false, "See Console");
			}
		}
	}

	abstract class TestCase
	{
		protected abstract IRecurr d { get; }
		readonly DateTime s;
		readonly DateTime e;
		readonly List<DateTime> res;

		private TestCase(DateTime testStart, DateTime testEnd) { s = testStart; e = testEnd; }

		public TestCase(IEnumerable<DateTime> expectedResults,DateTime testStart, DateTime testEnd) : this(testStart, testEnd)
		{ res=new List<DateTime>(expectedResults); }
		public void AssertCase()
		{
			List<DateTime> test = new List<DateTime> (d.GetOccurances (s, e));
			Debug.Assert (test.Count == res.Count);
			for (int i = 0; i < test.Count; i++)
				Debug.Assert (test [i] == res [i], test [i].ToShortDateString () + " is not the expected " + res [i].ToShortDateString ());
		}

		readonly String expectedFailure;
		public TestCase(String expectedFailure,DateTime testStart, DateTime testEnd) : this(testStart, testEnd)
		{ this.expectedFailure=expectedFailure; }
		public void AssertFail()
		{
			Exception ec = null;
			try {  var getit = d; }//create it
			catch(Exception e) { ec = e; }
			Debug.Assert(ec is ArgumentException && (ec as ArgumentException).ParamName == expectedFailure);
		}
	}



	class EveryPatternTests
	{
		class EveryTestCase : TestCase
		{
			Func<RecurrsEveryPattern> _d;
			protected override IRecurr d { get { return _d(); } }
			public EveryTestCase(DateTime fx, int f, RecurrSpan t, DateTime? l, DateTime? u , DateTime testStart, DateTime testEnd, IEnumerable<DateTime> expectedResults)
				:base(expectedResults, testStart,testEnd)
			{ _d =()=> new RecurrsEveryPattern(fx,f,t,l,u); }
			public EveryTestCase(DateTime fx, int f, RecurrSpan t, DateTime? l, DateTime? u , DateTime testStart, DateTime testEnd, String exprectedError)
				:base(exprectedError, testStart,testEnd)
			{ _d =()=> new RecurrsEveryPattern(fx,f,t,l,u); }
		}
		List<TestCase> cases = new List<TestCase> {
			// Some on-every test cases...
            // check we dont lose the least significant part! 
			new EveryTestCase(new DateTime(2015,2,12,4,0,0),1,RecurrSpan.Day,null,null,new DateTime(1950,1,1),new DateTime(1950,1,6),
                new[] { new DateTime(1950,1,1,4,0,0),new DateTime(1950,1,2,4,0,0),new DateTime(1950,1,3,4,0,0),new DateTime(1950,1,4,4,0,0),new DateTime(1950,1,5,4,0,0) }
            ),
            new EveryTestCase(new DateTime(2015,2,2,4,0,0),1,RecurrSpan.Day,null,null,new DateTime(2015,2,10),new DateTime(2015,2,12,10,0,0),
                new[] { new DateTime(2015,2,10,4,0,0),new DateTime(2015,2,11,4,0,0),new DateTime(2015,2,12,4,0,0), }
            ),
            new EveryTestCase(new DateTime(2015,2,3,4,0,0),1,RecurrSpan.Week,null,null,new DateTime(2015,2,10),new DateTime(2015,2,25),
                new[] {new DateTime(2015,2,10,4,0,0),new DateTime(2015,2,17,4,0,0),new DateTime(2015,2,24,4,0,0) }
            ),
            new EveryTestCase(new DateTime(2014,2,3,4,0,0),1,RecurrSpan.Month,null,null,new DateTime(2018,1,1),new DateTime(2018,4,2),
                new[] {new DateTime(2018,1,3,4,0,0),new DateTime(2018,2,3,4,0,0),new DateTime(2018,3,3,4,0,0),}
            ),
			// leap year handling madness
			new EveryTestCase(new DateTime(2012,2,29,4,0,0),1,RecurrSpan.Year,null,null,new DateTime(2018,1,1),new DateTime(2022,1,2),
                new[] {new DateTime(2018,3,1,4,0,0),new DateTime(2019,3,1,4,0,0),new DateTime(2020,2,29,4,0,0),new DateTime(2021,3,1,4,0,0)}
            ),
			// impossible month handling
			new EveryTestCase(new DateTime(2010,1,31),1,RecurrSpan.Month,null,null,new DateTime(2012,1,1),new DateTime(2012,5,2),
                new[] {new DateTime(2012,1,31),new DateTime(2012,2,29),new DateTime(2012,3,31),new DateTime(2012,4,30)}
            ),
			// detect long range code changes at aleast...didnt confirm if correct! lol.
			new EveryTestCase(new DateTime(2015,2,12),1,RecurrSpan.Week,null,null,new DateTime(1950,1,1),new DateTime(1950,1,21),
                new[] { new DateTime(1950,1,4),new DateTime(1950,1,11),new DateTime(1950,1,18) }
            ),
			//thatll do for nows.
		};
		List<TestCase> failedContracts = new List<TestCase> {
		};
		public void RunTests()
		{
			foreach (var c in cases) 
				c.AssertCase ();
			foreach (var c in failedContracts)
				c.AssertFail ();
		}
	}
	class OnOfPatternTests
	{
		class OnTestCase : TestCase
		{
			Func<RecurrsOnPattern> _d;
			protected override IRecurr d { get { return _d(); } }
			public OnTestCase(int[] onIndexes, RecurrSpan unitsMask, DateTime patStart, DateTime patEnd, DateTime testStart, DateTime testEnd, IEnumerable<DateTime> expectedResults)
				:base(expectedResults, testStart,testEnd)
			{ _d =()=> new RecurrsOnPattern(onIndexes, unitsMask, patStart, patEnd); }
			public OnTestCase(int[] onIndexes, RecurrSpan unitsMask, DateTime patStart, DateTime patEnd, DateTime testStart, DateTime testEnd, String exprectedError)
				:base(exprectedError, testStart,testEnd)
			{ _d =()=> new RecurrsOnPattern(onIndexes, unitsMask, patStart, patEnd); }
		}
		List<TestCase> cases = new List<TestCase> {
			// for single concecutive pairs - pushing the last possible onindex
			new OnTestCase (
				new[] { 28 }, RecurrSpan.Day | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				new [] { new DateTime (2015, 1, 28), new DateTime (2015, 2, 28), new DateTime (2015, 3, 28) }
			),
			new OnTestCase (
				new[] { 365 }, RecurrSpan.Day | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2018, 1, 1),
				new [] { new DateTime (2015, 12, 31), new DateTime (2016, 12, 30), new DateTime (2017, 12, 31) }
			),
			new OnTestCase (
				new[] { 7 }, RecurrSpan.Day | RecurrSpan.Week, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 1, 31),
				new [] { new DateTime (2015, 1, 4), new DateTime (2015, 1, 11), new DateTime (2015, 1, 18), new DateTime (2015, 1, 25) }
			),
			new OnTestCase (
				new[] { 4 }, RecurrSpan.Week | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				new [] { new DateTime (2015, 1, 28), new DateTime (2015, 2, 28), new DateTime (2015, 3, 28) }
			),
			new OnTestCase (
				new[] { 52 }, RecurrSpan.Week | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2017, 1, 1),
				new [] { new DateTime (2015, 12, 28), new DateTime (2016, 12, 26) }
			),
			new OnTestCase(
				new[] { 12 }, RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 12, 1), new DateTime (2018, 1, 1),
				new [] { new DateTime (2015, 12, 1), new DateTime (2016, 12, 1), new DateTime (2017, 12, 1) }
			),
			// Triplets
			new OnTestCase(
				new[] { 4,3 }, RecurrSpan.Week | RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2017, 4, 1),
				new [] { new DateTime (2015, 3, 28), new DateTime (2016, 3, 28), new DateTime (2017, 3, 28) }
			),
			new OnTestCase(
				new[] { 4,4 }, RecurrSpan.Day | RecurrSpan.Week | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				new [] { new DateTime (2015, 1, 29), new DateTime (2015, 2, 26), new DateTime (2015, 3, 26) }
			),
			// Quaddie
			new OnTestCase(
				new[] { 7,4,12 }, RecurrSpan.Day | RecurrSpan.Week | RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2018, 4, 1),
				new [] { new DateTime (2016, 1, 3), new DateTime (2017, 1, 1), new DateTime (2017, 12, 31) }
			),
		};

		List<TestCase> failedContracts = new List<TestCase>
		{
			// out of ranges
			new OnTestCase (
				new[] { 13 }, RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]"),
			new OnTestCase (
				new[] { 53 }, RecurrSpan.Week | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]"),
			new OnTestCase (
				new[] { 5 }, RecurrSpan.Week | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]"),
			new OnTestCase (
				new[] { 366 }, RecurrSpan.Day | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]"),
			new OnTestCase (
				new[] { 29 }, RecurrSpan.Day | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]"),
			new OnTestCase (
				new[] { 8 }, RecurrSpan.Day | RecurrSpan.Week, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]"),
			new OnTestCase (
				new[] { 0 }, RecurrSpan.Day | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]"),

			// invalid comvinations
			new OnTestCase ( // too few indexes
				new[] { 29 }, RecurrSpan.Day | RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"unitsMask"),
			new OnTestCase (//to many indexes
				new[] { 29, 29, 29, 29 }, RecurrSpan.Day | RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"unitsMask"),
			new OnTestCase ( // mask too short/too many indexes
				new[] { 1,1 }, RecurrSpan.Day, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"unitsMask"),
			new OnTestCase ( // no empty indexes
				new int[] { }, RecurrSpan.Day, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes.Length"),
		};
		public void RunTests()
		{
			foreach (var c in cases) 
				c.AssertCase ();
			foreach (var c in failedContracts)
				c.AssertFail ();
		}
	}
}
