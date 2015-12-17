using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace LibRTP.Test
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			// Ad hock "testing framework"
			var opt = new OnOfPatternTests ();
			opt.RunTests ();
			var ept = new EveryPatternTests ();
			ept.RunTests ();
			Console.WriteLine ("Yayyy tests passed!!");
			Console.BackgroundColor = ConsoleColor.Green;
			Console.ForegroundColor = ConsoleColor.Black;
			Console.WriteLine (new String ('G', Console.WindowWidth-1));
			Console.WriteLine (new String ('r', Console.WindowWidth-1));
			Console.WriteLine (new String ('e', Console.WindowWidth-1));
			Console.WriteLine (new String ('e', Console.WindowWidth-1));
			Console.WriteLine (new String ('n', Console.WindowWidth-1));
			Console.ReadKey ();
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
			new EveryTestCase(new DateTime(2015,2,12),1,RecurrSpan.Day,null,null,new DateTime(1950,1,1),new DateTime(1950,1,5),
				new[] { new DateTime(1950,1,1),new DateTime(1950,1,2),new DateTime(1950,1,3),new DateTime(1950,1,4),new DateTime(1950,1,5) }
			),
			new EveryTestCase(new DateTime(2015,2,3),1,RecurrSpan.Week,null,null,new DateTime(2015,2,10),new DateTime(2015,2,24),
				new[] {new DateTime(2015,2,10),new DateTime(2015,2,17),new DateTime(2015,2,24) }
			),
			new EveryTestCase(new DateTime(2014,2,3),1,RecurrSpan.Month,null,null,new DateTime(2018,1,1),new DateTime(2018,4,1),
				new[] {new DateTime(2018,1,3),new DateTime(2018,2,3),new DateTime(2018,3,3),}
			),
			// leap year handling madness
			new EveryTestCase(new DateTime(2012,2,29),1,RecurrSpan.Year,null,null,new DateTime(2018,1,1),new DateTime(2022,1,1),
				new[] {new DateTime(2018,3,1),new DateTime(2019,3,1),new DateTime(2020,2,29),new DateTime(2021,3,1)}
			),
			// impossible month handling
			new EveryTestCase(new DateTime(2010,1,31),1,RecurrSpan.Month,null,null,new DateTime(2012,1,1),new DateTime(2012,5,1),
				new[] {new DateTime(2012,1,31),new DateTime(2012,2,29),new DateTime(2012,3,31),new DateTime(2012,4,30)}
			),
			// detect long range code changes at aleast...didnt confirm if correct! lol.
			new EveryTestCase(new DateTime(2015,2,12),1,RecurrSpan.Week,null,null,new DateTime(1950,1,1),new DateTime(1950,1,20),
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
				new[] { 28,1 }, RecurrSpan.Day | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				new [] { new DateTime (2015, 1, 28), new DateTime (2015, 2, 28), new DateTime (2015, 3, 28) }
			),
			new OnTestCase (
				new[] { 28,4 }, RecurrSpan.Day | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 10, 1),
				new [] { new DateTime (2015, 1, 28), new DateTime (2015, 5, 28), new DateTime (2015, 9, 28) }
			),
			new OnTestCase (
				new[] { 365,1 }, RecurrSpan.Day | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2017, 12, 31),
				new [] { new DateTime (2015, 12, 31), new DateTime (2016, 12, 30), new DateTime (2017, 12, 31) }
			),
			new OnTestCase (
				new[] { 365,2 }, RecurrSpan.Day | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2017, 12, 31),
				new [] { new DateTime (2015, 12, 31), new DateTime (2017, 12, 31) }
			),
			new OnTestCase (
				new[] { 7,1 }, RecurrSpan.Day | RecurrSpan.Week, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 1, 31),
				new [] { new DateTime (2015, 1, 4), new DateTime (2015, 1, 11), new DateTime (2015, 1, 18), new DateTime (2015, 1, 25) }
			),
			new OnTestCase (
				new[] { 4,1 }, RecurrSpan.Week | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				new [] { new DateTime (2015, 1, 28), new DateTime (2015, 2, 28), new DateTime (2015, 3, 28) }
			),
			new OnTestCase (
				new[] { 52,1 }, RecurrSpan.Week | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2017, 1, 1),
				new [] { new DateTime (2015, 12, 28), new DateTime (2016, 12, 26) }
			),
			new OnTestCase(
				new[] { 12,1 }, RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 12, 1), new DateTime (2017, 12, 1),
				new [] { new DateTime (2015, 12, 1), new DateTime (2016, 12, 1), new DateTime (2017, 12, 1) }
			),
			// Triplets
			new OnTestCase(
				new[] { 4,3,1 }, RecurrSpan.Week | RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2017, 4, 1),
				new [] { new DateTime (2015, 3, 28), new DateTime (2016, 3, 28), new DateTime (2017, 3, 28) }
			),
			new OnTestCase(
				new[] { 4,3,2 }, RecurrSpan.Week | RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2017, 4, 1),
				new [] { new DateTime (2015, 3, 28), new DateTime (2017, 3, 28) }
			),
			new OnTestCase(
				new[] { 4,4,1 }, RecurrSpan.Day | RecurrSpan.Week | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				new [] { new DateTime (2015, 1, 29), new DateTime (2015, 2, 26), new DateTime (2015, 3, 26) }
			),
			new OnTestCase(
				new[] { 4,4,6 }, RecurrSpan.Day | RecurrSpan.Week | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 12, 1),
				new [] { new DateTime (2015, 1, 29), new DateTime (2015, 7, 30) }
			),
			// Quaddie
			new OnTestCase(
				new[] { 7,4,12,1 }, RecurrSpan.Day | RecurrSpan.Week | RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2018, 4, 1),
				new [] { new DateTime (2016, 1, 3), new DateTime (2017, 1, 1), new DateTime (2017, 12, 31) }
			),
			new OnTestCase(
				new[] { 7,4,12,3 }, RecurrSpan.Day | RecurrSpan.Week | RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2019, 4, 1),
				new [] { new DateTime (2016, 1, 3), new DateTime (2018, 12, 30)}
			),

		
		};

		List<TestCase> failedContracts = new List<TestCase>
		{
			// out of ranges
			new OnTestCase (
				new[] { 13,1 }, RecurrSpan.Month | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]"),
			new OnTestCase (
				new[] { 53,1 }, RecurrSpan.Week | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]"),
			new OnTestCase (
				new[] { 5,1 }, RecurrSpan.Week | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]"),
			new OnTestCase (
				new[] { 366,1 }, RecurrSpan.Day | RecurrSpan.Year, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]"),
			new OnTestCase (
				new[] { 29,1 }, RecurrSpan.Day | RecurrSpan.Month, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]"),
			new OnTestCase (
				new[] { 8,1 }, RecurrSpan.Day | RecurrSpan.Week, 
				DateTime.MinValue, DateTime.MaxValue, new DateTime (2015, 1, 1), new DateTime (2015, 4, 1),
				"onIndexes[0]"),
			new OnTestCase (
				new[] { 0,1 }, RecurrSpan.Day | RecurrSpan.Month, 
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
