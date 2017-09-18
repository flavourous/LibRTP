using NUnit.Framework;
using System;
using System.Collections.Generic;

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
            public void Add(String name, RecurringAggregatePattern pat, DateTime fixedDate, DateTime startDate, DayTargetReturn res)
            {
                Add(new AggTest() { name = name, pat = pat, fixedDate = fixedDate, startDate = new[] { startDate }, res = new[] { res } });
            }
            public void Add(String name, RecurringAggregatePattern pat, DateTime fixedDate, DateTime[] startDates, DayTargetReturn[] ress)
            {
                Add(new AggTest() { name = name, pat = pat, fixedDate = fixedDate, startDate = startDates, res = ress });
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
}
