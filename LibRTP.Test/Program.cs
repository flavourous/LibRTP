using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;

namespace LibRTP.Test
{
	public class AggregatePatternTests
	{
        [TestCaseSource("Tests")]
        public void AggregateTests(AggTest t)
        {
            for (int i = 0; i < t.res.Length; i++)
            {
                var res = t.res[i]; var sd = t.startDate[i];
                var calc = t.pat.FindTargetForDay(t.fixedDate, sd);
                String info = String.Format("   Setup: {0}\n   Input: fixed at {1}, on day {2}\nExpected: {3}\n  Result: {4}",
                                 t.pat.ToString(), t.fixedDate.ToShortDateString(), sd.ToShortDateString(), res, calc);
                Assert.IsTrue(calc.begin == res.begin && calc.end == res.end && calc.target == res.target, info);
            }
        }

        public class AggTest 
		{
            public String name;
			public RecurringAggregatePattern pat;
			public DayTargetReturn[] res;
			public DateTime fixedDate;
			public DateTime[] startDate;
		}
		class AggList : List<AggTest>
		{
			public void Add(String name, RecurringAggregatePattern pat,DateTime fixedDate, DateTime startDate, DayTargetReturn res)
			{
				Add (new AggTest () { name=name, pat = pat, fixedDate = fixedDate, startDate = new[] { startDate }, res = new[] { res } });
			}
			public void Add(String name, RecurringAggregatePattern pat,DateTime fixedDate, DateTime[] startDates, DayTargetReturn[] ress)
			{
				Add (new AggTest () { name = name, pat = pat, fixedDate = fixedDate, startDate = startDates, res = ress });
			}
		}
        public static List<AggTest> Tests { get { return tests; } }
		static AggList tests = new AggList 
		{ 
			//some simple tests.
			{ 
                "testy",
				new RecurringAggregatePattern (1, AggregateRangeType.DaysFromStart, new[] { 1 }, new[] { 100.0 }),
				new DateTime (2015, 1, 1), new DateTime (2015, 1, 1),
				new DayTargetReturn (new DateTime (2015, 1, 1), new DateTime (2015, 1, 2), 100.0)
			},
			{
                "testy",
                new RecurringAggregatePattern (7, AggregateRangeType.DaysFromStart, new[] { 1 }, new[] { 100.0 }),
				new DateTime (2015, 1, 1), new DateTime (2015, 1, 1),
				new DayTargetReturn (new DateTime (2015, 1, 1), new DateTime (2015, 1, 8), 700.0)
			},
			{
                "testy",
                new RecurringAggregatePattern (0, AggregateRangeType.DaysEitherSide, new[] { 1 }, new[] { 100.0 }),
				new DateTime (2015, 1, 1), new DateTime (2015, 1, 1),
				new DayTargetReturn (new DateTime (2015, 1, 1), new DateTime (2015, 1, 2), 100.0)
			},
			{
                "testy",
                new RecurringAggregatePattern (3, AggregateRangeType.DaysEitherSide, new[] { 1 }, new[] { 100.0 }),
				new DateTime (2015, 1, 1), new DateTime (2015, 1, 1),
				new DayTargetReturn (new DateTime (2015, 1, 1).AddDays(-3), new DateTime (2015, 1, 2).AddDays(3), 700.0)
			},

			// Testing patterning is working.
			{
                "testy",
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
                "testy",
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
                "testy",
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
                "testy",
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
		
	}

	abstract class TestCase
	{
        public String name;
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
			Assert.AreEqual(test.Count,res.Count);
            CollectionAssert.AreEqual(test, res);
		}

		readonly String expectedFailure;
		public TestCase(String expectedFailure,DateTime testStart, DateTime testEnd) : this(testStart, testEnd)
		{ this.expectedFailure=expectedFailure; }
		public void AssertFail()
		{
			Exception ec = null;
			try {  var getit = d; }//create it
			catch(Exception e) { ec = e; }
            Assert.IsInstanceOf<ArgumentException>(ec);
            Assert.AreEqual((ec as ArgumentException).ParamName, expectedFailure);
		}
	}



	class EveryPatternTests
	{

        [TestCaseSource("cases")]
        public void EveryTests(EveryTestCase t)
        {
            Console.WriteLine(t.name);
            if (Debugger.IsAttached)
            {
                try { t.AssertCase(); }
                catch { Debugger.Break(); t.AssertCase(); }
            }
            else t.AssertCase();
        }

        public class EveryTestCase : TestCase
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
        
		public static List<TestCase> cases = new List<TestCase> {
			// Some on-every test cases...
            // check we dont lose the least significant part! 
			new EveryTestCase(new DateTime(2015,2,12,4,0,0),1,RecurrSpan.Day,null,null,new DateTime(1950,1,1),new DateTime(1950,1,6),
                new[] { new DateTime(1950,1,1,4,0,0),new DateTime(1950,1,2,4,0,0),new DateTime(1950,1,3,4,0,0),new DateTime(1950,1,4,4,0,0),new DateTime(1950,1,5,4,0,0) }
            ) { name = "LeastSig-days-old" },
            new EveryTestCase(new DateTime(2015,2,2,4,0,0),1,RecurrSpan.Day,null,null,new DateTime(2015,2,10),new DateTime(2015,2,12,10,0,0),
                new[] { new DateTime(2015,2,10,4,0,0),new DateTime(2015,2,11,4,0,0),new DateTime(2015,2,12,4,0,0), }
            ) { name = "LeastSig-days" },
            new EveryTestCase(new DateTime(2015,2,3,4,0,0),1,RecurrSpan.Week,null,null,new DateTime(2015,2,10),new DateTime(2015,2,25),
                new[] {new DateTime(2015,2,10,4,0,0),new DateTime(2015,2,17,4,0,0),new DateTime(2015,2,24,4,0,0) }
            ) { name = "LeastSig-weeks" },
            new EveryTestCase(new DateTime(2014,2,3,4,0,0),1,RecurrSpan.Month,null,null,new DateTime(2018,1,1),new DateTime(2018,4,2),
                new[] {new DateTime(2018,1,3,4,0,0),new DateTime(2018,2,3,4,0,0),new DateTime(2018,3,3,4,0,0),}
            ) { name = "LeastSig-months" },
            // and with shifted starts (because this broke it at one point)
            new EveryTestCase(new DateTime(2017,8,22,8,0,0),3,RecurrSpan.Day,null,null,new DateTime(2017,8,23),new DateTime(2017,8,29),
                new[] { new DateTime(2017,8,25,8,0,0), new DateTime(2017,8,28,8,0,0) }
            ) { name = "ShiftedStart-days" },
            new EveryTestCase(new DateTime(2015,2,3,4,0,0),1,RecurrSpan.Week,null,null,new DateTime(2015,2,5),new DateTime(2015,2,25),
                new[] {new DateTime(2015,2,10,4,0,0),new DateTime(2015,2,17,4,0,0),new DateTime(2015,2,24,4,0,0) }
            ) { name = "ShiftedStart-weeks" },
            new EveryTestCase(new DateTime(2014,2,3,4,0,0),1,RecurrSpan.Month,null,null,new DateTime(2017,12,20),new DateTime(2018,4,2),
                new[] {new DateTime(2018,1,3,4,0,0),new DateTime(2018,2,3,4,0,0),new DateTime(2018,3,3,4,0,0),}
            ) { name = "ShiftedStart-months" },
			// leap year handling madness
			new EveryTestCase(new DateTime(2012,2,29,4,0,0),1,RecurrSpan.Year,null,null,new DateTime(2018,1,1),new DateTime(2022,1,2),
                new[] {new DateTime(2018,3,1,4,0,0),new DateTime(2019,3,1,4,0,0),new DateTime(2020,2,29,4,0,0),new DateTime(2021,3,1,4,0,0)}
            ) { name = "leap years" },
			// impossible month handling
			new EveryTestCase(new DateTime(2010,1,31),1,RecurrSpan.Month,null,null,new DateTime(2012,1,1),new DateTime(2012,5,2),
                new[] {new DateTime(2012,1,31),new DateTime(2012,2,29),new DateTime(2012,3,31),new DateTime(2012,4,30)}
            ) { name = "impossible month" },
			// detect long range code changes at aleast...didnt confirm if correct! lol.
			new EveryTestCase(new DateTime(2015,2,12),1,RecurrSpan.Week,null,null,new DateTime(1950,1,1),new DateTime(1950,1,21),
                new[] { new DateTime(1950,1,5),new DateTime(1950,1,12),new DateTime(1950,1,19) }
            ) { name = "long range recursion" },
			// lets look more closesly at ones with occurance ranges before the fixed point
            new EveryTestCase(new DateTime(2015,2,2,4,0,0),2,RecurrSpan.Day,null,null,new DateTime(2015,1,10),new DateTime(2015,1,16),
                new[] { new DateTime(2015,1,11,4,0,0),new DateTime(2015,1,13,4,0,0),new DateTime(2015,1,15,4,0,0), }
            ) { name = "backward-days" },
            new EveryTestCase(new DateTime(2015,2,3,4,0,0),2,RecurrSpan.Week,null,null,new DateTime(2015,1,1),new DateTime(2015,1,30),
                new[] {new DateTime(2015,1,6,4,0,0),new DateTime(2015,1,20,4,0,0) }
            ) { name = "backward-weeks" },
            new EveryTestCase(new DateTime(2014,2,3,4,0,0),1,RecurrSpan.Month,null,null,new DateTime(2012,1,1),new DateTime(2012,4,2),
                new[] {new DateTime(2012,1,3,4,0,0),new DateTime(2012,2,3,4,0,0),new DateTime(2012,3,3,4,0,0),}
            ) { name = "backward-months" },
        };
		List<TestCase> failedContracts = new List<TestCase> {
		};
	}
	class OnOfPatternTests
	{
        [TestCaseSource("cases")]
        public void OnTests(OnTestCase t)
        {
            t.AssertCase();
        }
        [TestCaseSource("failedContracts")]
        public void OnTestsExpectedFailures(OnTestCase t)
        {
            t.AssertFail();
        }
        public class OnTestCase : TestCase
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
		public static List<OnTestCase> cases = new List<OnTestCase> {
			// for single concecutive pairs - pushing the last possible onindex
			new OnTestCase (
				new[] { 28, 1 }, RecurrSpan.Day | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				new [] { new DateTime (2015, 1, 28), new DateTime (2015, 2, 28), new DateTime (2015, 3, 28) }
            ) { name = "lol" },
            new OnTestCase (
				new[] { 365, 1 }, RecurrSpan.Day | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2018, 1, 1),
				new [] { new DateTime (2015, 12, 31), new DateTime (2016, 12, 30), new DateTime (2017, 12, 31) }
            ) { name = "lol" },
            new OnTestCase (
				new[] { 7, 1 }, RecurrSpan.Day | RecurrSpan.Week, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 1, 31),
				new [] { new DateTime (2015, 1, 4), new DateTime (2015, 1, 11), new DateTime (2015, 1, 18), new DateTime (2015, 1, 25) }
            ) { name = "lol" },
            new OnTestCase (
				new[] { 4, 1 }, RecurrSpan.Week | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				new [] { new DateTime (2015, 1, 28), new DateTime (2015, 2, 28), new DateTime (2015, 3, 28) }
            ) { name = "lol" },
            new OnTestCase (
				new[] { 52, 1 }, RecurrSpan.Week | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2017, 1, 1),
				new [] { new DateTime (2015, 12, 28), new DateTime (2016, 12, 26) }
            ) { name = "lol" },
            new OnTestCase(
				new[] { 12, 1 }, RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 12, 1), new DateTime (2018, 1, 1),
				new [] { new DateTime (2015, 12, 1), new DateTime (2016, 12, 1), new DateTime (2017, 12, 1) }
            ) { name = "lol" },
			// Triplets
			new OnTestCase(
				new[] { 4,3, 1 }, RecurrSpan.Week | RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2017, 4, 1),
				new [] { new DateTime (2015, 3, 28), new DateTime (2016, 3, 28), new DateTime (2017, 3, 28) }
            ) { name = "lol" },
            new OnTestCase(
				new[] { 4,4, 1 }, RecurrSpan.Day | RecurrSpan.Week | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				new [] { new DateTime (2015, 1, 29), new DateTime (2015, 2, 26), new DateTime (2015, 3, 26) }
            ) { name = "lol" },
			// Quaddie
			new OnTestCase(
				new[] { 7,4,12, 1 }, RecurrSpan.Day | RecurrSpan.Week | RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2018, 4, 1),
				new [] { new DateTime (2016, 1, 3), new DateTime (2017, 1, 1), new DateTime (2017, 12, 31) }
            ) { name = "lol" },


            //a again for bigger range on last digig
            // for single concecutive pairs - pushing the last possible onindex

            //9
			new OnTestCase (
                new[] { 28, 2 }, RecurrSpan.Day | RecurrSpan.Month,
                DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
                new [] { new DateTime (2015, 1, 28), new DateTime (2015, 3, 28) }
            ) { name = "lol" },
            new OnTestCase (
                new[] { 365, 3 }, RecurrSpan.Day | RecurrSpan.Year,
                DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2019, 1, 1),
                new [] { new DateTime (2015, 12, 31), new DateTime (2018, 12, 31) }
            ) { name = "lol" },
            new OnTestCase (
                new[] { 7, 2 }, RecurrSpan.Day | RecurrSpan.Week,
                DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 1, 31),
                new [] { new DateTime (2015, 1, 4), new DateTime (2015, 1, 18) }
            ) { name = "lol" },
            new OnTestCase (
                new[] { 4, 3 }, RecurrSpan.Week | RecurrSpan.Month,
                DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 5, 1),
                new [] { new DateTime (2015, 1, 28), new DateTime (2015, 4, 28) }
            ) { name = "lol" },
            //13
            new OnTestCase (
                new[] { 52, 2 }, RecurrSpan.Week | RecurrSpan.Year,
                DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2018, 1, 1),
                new [] { new DateTime (2015, 12, 28), new DateTime (2017, 12, 25) }
            ) { name = "lol" },
            new OnTestCase(
                new[] { 12, 2 }, RecurrSpan.Month | RecurrSpan.Year,
                DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 12, 1), new DateTime (2018, 1, 1),
                new [] { new DateTime (2015, 12, 1), new DateTime (2017, 12, 1) }
            ) { name = "lol" },
			// Triplets
			new OnTestCase(
                new[] { 4, 3, 2 }, RecurrSpan.Week | RecurrSpan.Month | RecurrSpan.Year,
                DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2017, 4, 1),
                new [] { new DateTime (2015, 3, 28), new DateTime (2017, 3, 28) }
            ) { name = "lol" },
            new OnTestCase(
                new[] { 4,4, 2 }, RecurrSpan.Day | RecurrSpan.Week | RecurrSpan.Month,
                DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
                new [] { new DateTime (2015, 1, 29), new DateTime (2015, 3, 26) }
            ) { name = "lol" },
			// Quaddie
			new OnTestCase(
                new[] { 7,4,12, 2 }, RecurrSpan.Day | RecurrSpan.Week | RecurrSpan.Month | RecurrSpan.Year,
                DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2018, 4, 1),
                new [] { new DateTime (2016, 1, 3), new DateTime (2017, 12, 31) }
            ) { name = "lol" },
        };

		public static List<OnTestCase> failedContracts = new List<OnTestCase>
		{
			// out of ranges
			new OnTestCase (
				new[] { 13, 1 }, RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]") { name = "lol" },
            new OnTestCase (
				new[] { 53, 1 }, RecurrSpan.Week | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]") { name = "lol" },
            new OnTestCase (
				new[] { 5, 1 }, RecurrSpan.Week | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]") { name = "lol" },
            new OnTestCase (
				new[] { 366, 1 }, RecurrSpan.Day | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]") { name = "lol" },
            new OnTestCase (
				new[] { 29, 1 }, RecurrSpan.Day | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]") { name = "lol" },
            new OnTestCase (
				new[] { 8, 1 }, RecurrSpan.Day | RecurrSpan.Week, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]") { name = "lol" },
            new OnTestCase (
				new[] { 0, 1 }, RecurrSpan.Day | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]") { name = "lol" },

			// invalid comvinations
			new OnTestCase ( // too few indexes
				new[] { 29 }, RecurrSpan.Day | RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"unitsMask") { name = "lol" },
            new OnTestCase (//to many indexes
				new[] { 29, 29, 29, 29 }, RecurrSpan.Day | RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"unitsMask") { name = "lol" },
            new OnTestCase ( // mask too short/too many indexes
				new[] { 1,1 }, RecurrSpan.Day, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"unitsMask") { name = "lol" },
            new OnTestCase ( // no empty indexes
				new int[] { }, RecurrSpan.Day, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes.Length") { name = "lol" },
        };
	}
}
