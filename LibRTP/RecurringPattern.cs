using System;
using System.Collections.Generic;
using System.IO;
using LibSharpHelp;
using System.Linq;
using System.Collections;
using System.Diagnostics;
using MH = LibRTP.ModulusHelpers;

namespace LibRTP
{
    delegate DateTime DateTimeStarter(DateTime d);
    delegate double DateTimeUniter(int year, int month);
    delegate DateTime DateTimeShifter(DateTime src, int val);
    delegate int DateTimeMaxer(DateTimeUniter unit, int y, int m, int x, int fac);
    delegate (int div, int rem, int nYr, int nMon) DateTimeDivider(DateTimeUniter unit, int y, int m, int x, int fac);
    class RUIBuilder : IEnumerable
    {
        public class RUI
        {
            readonly RSpan on, of;
            readonly DateTimeUniter uniter;
            readonly int unitFac;
            readonly DateTimeShifter fromStartPoint;
            readonly DateTimeStarter startPoint;
            readonly DateTimeMaxer maxer;
            readonly DateTimeDivider divider;
            
            // Get the max number of "on" for this many "of" starting here y/M.
            public int MaxValue(int year, int month, int nOf)
            {
                if (month < 0) throw new ArgumentException("month takes negative (last) form as has not been processed");
                return maxer(uniter, year, month, nOf, unitFac);
            }
            public (int div, int rem, int year, int month) DivideValue(int year, int month, int of)
            {
                return divider(uniter, year, month, of, unitFac);
            }
            public DateTime CreateAtValue(DateTime d, int v, int mult)
            {
                var max = MaxValue(d.Year, d.Month, mult);
                var nv = PatternHelpers.ReverseIfNegative(on, v, max);
                var start = startPoint(d);
                return fromStartPoint(start, nv);
            }
            public RUI(RSpan on, RSpan of, DateTimeMaxer maxer, DateTimeDivider divider, DateTimeUniter uniter, int dateTimeFac, DateTimeStarter startPoint, DateTimeShifter fromStartPoint)
            {
                this.of = on;
                this.on = of;
                this.uniter = uniter;
                this.unitFac = dateTimeFac;
                this.fromStartPoint = fromStartPoint;
                this.startPoint = startPoint;
                this.maxer = maxer;
                this.divider = divider;
            }
        }
        Dictionary<RSpan, RUI> rv = new Dictionary<RSpan, RUI>();
        public void Add(RSpan on, RSpan of, DateTimeUniter uniter, DateTimeMaxer maxer, DateTimeDivider divider,  int dateTimeFac, DateTimeStarter startPoint, DateTimeShifter fromStartPoint)
        {
            var rs = on | of;
            rv[rs] = new RUI(on, of, maxer, divider, uniter, dateTimeFac, startPoint, fromStartPoint);
        }

        public IEnumerator GetEnumerator()
        {
            return rv.GetEnumerator();
        }

        public RUI this[RSpan v] { get { return rv[v]; } }
    }

    // week starts on bloddy monday, srsly.  There's probabbly a ll8n issue here.
    static class PatternHelpers
    {
        
        public static int ReverseIfNegative(RSpan unit, int of, int max)
        {
            switch (unit)
            {
                default:
                case RSpan.None:
                case RSpan.Minute:
                case RSpan.Hour:
                    return of < 0 ? max + of : of;
                case RSpan.Day:
                case RSpan.Week:
                case RSpan.Month:
                case RSpan.Year:
                    return of < 0 ? max + (of + 1) : of;
            }
        }
        public static bool ValidateMax(RSpan unit, int of, int max)
        {
            int use = ReverseIfNegative(unit, of, max);
            switch (unit)
            {
                default:
                case RSpan.None:
                    return false;
                case RSpan.Minute:
                case RSpan.Hour:
                    return use >= 0 && use < max;
                case RSpan.Day:
                case RSpan.Week:
                case RSpan.Month:
                case RSpan.Year:
                    return use > 0 && use <= max;
            }
        }
        
        // ab: a in b, 00:mm:hh dd/MM/yyyy  (max vals, if you go over here, we allow skip to infinity!)
        public static RSpan[] RSO = new RSpan[] { RSpan.Minute, RSpan.Hour, RSpan.Day, RSpan.Week, RSpan.Month, RSpan.Year };
        public static RUIBuilder Units = new RUIBuilder
        {
            { RSpan.Year,  RSpan.Month,   MH.Identity,     MH.UnitMaxer,   MH.UnitDivider,   12,      MH.StartOfYear,  MH.AddMonthsMinus1 },
            { RSpan.Year,  RSpan.Week,    MH.WeeksInYear,  MH.WkYearMaxer, MH.WkYearDivider, 1,       MH.StartOfYear,  MH.AddWeeksMinus1  },
            { RSpan.Year,  RSpan.Day,     MH.DaysInYear,   MH.YearMaxer,   MH.YearDivider,   1,       MH.StartOfYear,  MH.AddDaysMinus1   },
            { RSpan.Year,  RSpan.Hour,    MH.DaysInYear,   MH.YearMaxer,   MH.YearDivider,   24,      MH.StartOfYear,  MH.AddHours        },
            { RSpan.Year,  RSpan.Minute,  MH.DaysInYear,   MH.YearMaxer,   MH.YearDivider,   24*60,   MH.StartOfYear,  MH.AddMinutes      },
            { RSpan.Month, RSpan.Week,    MH.WeeksInMonth, MH.WkMonMaxer,  MH.WkMonDivider,  1,       MH.StartOfMonth, MH.AddWeeksMinus1  },
            { RSpan.Month, RSpan.Day,     MH.DaysInMonth,  MH.MonthMaxer,  MH.MonthDivider,  1,       MH.StartOfMonth, MH.AddDaysMinus1   },
            { RSpan.Month, RSpan.Hour,    MH.DaysInMonth,  MH.MonthMaxer,  MH.MonthDivider,  24,      MH.StartOfMonth, MH.AddHours        },
            { RSpan.Month, RSpan.Minute,  MH.DaysInMonth,  MH.MonthMaxer,  MH.MonthDivider,  24*60,   MH.StartOfMonth, MH.AddMinutes      },
            { RSpan.Week,  RSpan.Day,     MH.Identity,     MH.UnitMaxer,   MH.UnitDivider,   7,       MH.StartOfWeek,  MH.AddDaysMinus1   },
            { RSpan.Week,  RSpan.Hour,    MH.Identity,     MH.UnitMaxer,   MH.UnitDivider,   7*24,    MH.StartOfWeek,  MH.AddHours        },
            { RSpan.Week,  RSpan.Minute,  MH.Identity,     MH.UnitMaxer,   MH.UnitDivider,   7*24*60, MH.StartOfWeek,  MH.AddMinutes      },
            { RSpan.Day,   RSpan.Hour,    MH.Identity,     MH.UnitMaxer,   MH.UnitDivider,   24,      MH.StartOfDay,   MH.AddHours        },
            { RSpan.Day,   RSpan.Minute,  MH.Identity,     MH.UnitMaxer,   MH.UnitDivider,   24*60,   MH.StartOfDay,   MH.AddMinutes      },
            { RSpan.Hour,  RSpan.Minute,  MH.Identity,     MH.UnitMaxer,   MH.UnitDivider,   60,      MH.StartOfHour,  MH.AddMinutes      },
        };
        const String formatNames = "mhwdMy";
        static int?[] ParsePart(String partName, String part, int formatStart)
        {
            var sparts = part.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int?[] ret = new int?[sparts.Length];
            for (int i = 0; i < sparts.Length; i++)
            {
                if (sparts[i] == "_")
                    ret[i] = null;
                else if (int.TryParse(sparts[i], out int oi))
                    ret[i] = oi;
                else
                    throw new ArgumentException(String.Format("{0} of {1} is invalid", formatNames[i + formatStart], partName));
            }
            return ret;
        }
        public static (int?[] on, int?[] every, int?[] at) ParsePattern(string format)
        {
            if (format == null)
                throw new ArgumentNullException("format");

            var pipe_index = format.IndexOf('|');
            var at_index = format.IndexOf('@');
            if (pipe_index == -1 || at_index == -1 || pipe_index > at_index)
                throw new ArgumentException("format must contain | and @ in that order, see the docs");

            var parts = format.Split('|', '@');
            var on = ParsePart("on", parts[0], 0);
            var every = ParsePart("every", parts[1], on.Length);
            var at = ParsePart("at", parts[2], on.Length);

            if (on.Length + every.Length != 6)
                throw new ArgumentException("'on' and 'every' do not cover all 6 indicies");
            if (every.Length != at.Length)
                throw new ArgumentException("Number of indcies in 'at'and 'every' do not match");
            if (!every.First().HasValue)
                throw new ArgumentException("'every' cannot begin with a '_' specifier");
            if (!at.First().HasValue)
                throw new ArgumentException("'at' cannot begin with a '_' specifier");
            if (!at.Last().HasValue)
                throw new ArgumentException("'at' cannot end with a '_' specifier");
            return (on, every, at);
        }
        
        public static (int minutes, int months) SplitEveryPattern(int?[] every, int onLength, RSpan forceUnits = RSpan.None)
        {
            var devery = new int[2];
            int[] amt = new int[] { 1, 60, 60 * 24, 60 * 24 * 7, 1, 12 };
            int[] trg = new int[] { 0, 0, 0, 0, 1, 1 };
            for (int i = 0; i < every.Length; i++)
                if (every[i].HasValue)
                    devery[trg[i + onLength]] += every[i].Value * amt[i + onLength];
  
            if(forceUnits != RSpan.None)
            {
                var ux = Array.IndexOf(RSO, forceUnits);
                devery[trg[ux]] /= amt[ux];
            }

            if (devery[0] > 0 && devery[1] > 0)
                throw new NotSupportedException("No idea how to do the modulus of every (<month) + every (>=month).");
            return (devery[0], devery[1]);
        }
    }

    class PartsVisitor
    {
        int bridgeEvery;
        RSpan bridgeUnit;
        /// <summary>
        /// Args from the basis of the pattern, rather than the current
        /// problem to solve as prescribed by the similar member method args
        /// </summary>
        public PartsVisitor(int?[] onBasis, int?[] everyBasis, int?[] atBasis)
        {
            var mm = PatternHelpers.SplitEveryPattern(everyBasis, onBasis.Length, PatternHelpers.RSO[onBasis.Length]);
            bridgeEvery = mm.minutes + mm.months;
            bridgeUnit = PatternHelpers.RSO[onBasis.Length];
        }
        // day 900 of every 70 months starting month 7 year 2012
        // 0 125 | 6 _ _ _ @ 1 _ 1 2012 
        // 0 0 900 _ | 70 _ @ 7 2012
        // 0 0 1 _ 500 | 3 @ 2012
        // but also required so that we can detect that day 366+355+355 of every 3 years, basically means to skip non leap years, for example
        public (bool success, String error) ValidateOnOf(int?[] onStack, int?[] everyStack, int?[] atStack)
        {
            // prepare start point and year
            int year = atStack.Last().Value;
            int month = 1;

            // two checks.  Check Maxvalue, but also form it and check against AT
            DateTime forming = new DateTime(year, month, 1), maxAt = new DateTime(year, month, 1);
            bool beyondBridge = false;

            // calc everies
            var se = PatternHelpers.SplitEveryPattern(everyStack, onStack.Length);
            var mm = PatternHelpers.SplitEveryPattern(everyStack, onStack.Length, PatternHelpers.RSO[onStack.Length]);
            int riskyEvery = mm.minutes + mm.months;
            Debug.Assert(mm.minutes == 0 || mm.months == 0);

            // iterate them
            foreach (var (on, the, of) in IterateStack(onStack.Concat(atStack.Take(atStack.Length - 1)).ToArray(), 1))
            {
                int max = 0, mult = 1;
                var units = PatternHelpers.Units[on | of];

                // <BIG> of <normal>  - we pass every, e.g. 900th day of january (2015) given 31 months
                if (Array.IndexOf(PatternHelpers.RSO, of) == onStack.Length)
                {
                    mult = riskyEvery;
                    beyondBridge = true;
                    maxAt = forming.AddMinutes(se.minutes).AddMonths(se.months);
                }

                // Continue forming, and if beyond the bridge, check we're not beyond the caluclate at
                forming = units.CreateAtValue(forming, the, mult);
                if (beyondBridge && forming > maxAt)
                    return (false, string.Format("{0} {1} of {2} moves beyond {3}", on, the, of, maxAt));

                // Calculate the max & check limits
                max = units.MaxValue(year, month, mult);
                if (!PatternHelpers.ValidateMax(on, the, max))
                    return (false, string.Format("There is not a {0} {1} +of {3} {2}", on, the, of, mult));

                // <normal> of <BIG>  - we need to figure out what the modulus of <BIG> is
                // doesnt matter on the first of this loop
                // -1 could easily become 55. possibly overgeneralized right now.
                if (on == RSpan.Month)
                    month = units.DivideValue(year, month, PatternHelpers.ReverseIfNegative(RSpan.Month, the, max)).rem;
            }
            return (true, null);
        }

        public DateTime EvaluateOnOf(int?[] combinedStack, DateTime? startingWith, int offset)
        {
            if (startingWith == null && (offset != 0 || !combinedStack.Last().HasValue))
                throw new ArgumentException("Unable to evaluate, no starting year.");
            if (combinedStack.Length + offset > PatternHelpers.RSO.Length)
                throw new ArgumentException(String.Format("part({0}) or offset({1}) are too long", combinedStack.Length, offset));
            DateTime forming;
            int?[] useOn;

            if (startingWith.HasValue)
            {
                forming = startingWith.Value;
                useOn = combinedStack;
            }
            else
            {
                forming = new DateTime(combinedStack.Last().Value, 1, 1);
                offset = 1;
                useOn = combinedStack.Take(combinedStack.Length - 1).ToArray();
            }
            
            Console.WriteLine(forming);
            foreach (var (on, the, of) in IterateStack(useOn, offset))
            {
                Console.WriteLine("{0} {1} of {2}", on, the, of);
                var use = PatternHelpers.Units[on | of];
                forming = use.CreateAtValue(forming, the, of == bridgeUnit ? bridgeEvery : 1);
                Console.WriteLine(forming);
            }
            Console.WriteLine();

            return forming;
        }
        IEnumerable<(RSpan on, int the, RSpan of)> IterateStack(int?[] onStack, int offset)
        {
            RSpan ofContext = PatternHelpers.RSO[PatternHelpers.RSO.Length - offset];
            for (int j = onStack.Length - 1; j >= 0; j--)
            {
                var ri = PatternHelpers.RSO.Length - 1 - offset++;
                if (onStack[j].HasValue)
                {
                    var onContext = PatternHelpers.RSO[ri];
                    var skip = onStack[j].Value;
                    yield return (onContext, skip, ofContext);
                    ofContext = onContext;
                }
            }
        }
    }

    class PartsIterator
    {
        // Invalids:
        // 0 0 5 | _ 2 _ @ 1 _ 2017 :  on 5th day of every 2 months starting 1st week of 2017  :  invalid because every starts with _'s
        // 0 0 2 _ | 2 _ @ _ 2016  : on 2nd day of every 2 months starting 2016  : invalid because every and at dont match in _'s
        // 0 0 2 | 2 _ _ @ _ 1 2016  : on 2nd day of every 2 weeks starting 1 2016  : invalid because every and at dont match in _'s


        // 0 0 2 | 2 _ _ @ 3 _ 2016  ::   2nd day of every 2 weeks starting  3rd week of 2016.  how to do 02/01/2016 every 2 weeks?

        // 0 0 2 | 2 _ _ @ _ _ 2 _ 1 2016 ?? this is only valid by luck, 0 0 2 | 2 _ _ @ 1 2 3 _ 1 2016  is contradictory

        // so we need every to start with number, and on to start and end with a number
        // 09/01/2017 every 2 weeks e.g. is possible, but tricky therefore:
        //  0 0 2 | 2 _ _ @ 2 _ 2017  (first week in jan is the 2nd jan) meaning 2nd day of every 2 weeks starting 2nd week of 2017

        int minutes, months;
        int?[] on, every, at;
        PartsVisitor visitor;
        DateTime dat, buster;
        public PartsIterator(int?[] on, int?[] every, int?[] at, PartsVisitor visitor, DateTime buster)
        {
            this.visitor = visitor;
            this.buster = buster;
            this.on = on;
            this.every = every;
            this.at = at;
            (minutes, months) = PatternHelpers.SplitEveryPattern(every, on.Length);
            this.dat = visitor.EvaluateOnOf(at, null, 0);
            Current = visitor.EvaluateOnOf(on, this.dat, 6 - on.Length);
        }
        public DateTime Current { get; private set; }

//#error It's not possible to alter the "at" date or part.  It uniquely defines e.g. the  day of the week.
    // For example,on day 3 (ev week) at week 1 of feb 2015 is week starting sunday. 
    //   closest do jan 1st 2015, we subtract a bunch of weeks to reach 28th dec 2014
    //   when EvaluateOnOf runs, it's not gonna get 

            //  actually, no, thats the on part.


        public void MoveToClosest(DateTime to)
        {
            Console.WriteLine("Moving to closest (from {0})", dat);
            // advance the at date by the split minutes or months modulue
            if (minutes > 0)
            {
                var mdiff = (int)(to - dat).TotalMinutes;
                var mrem = mdiff % minutes; // negative is correct!
                dat = to.AddMinutes(-mrem);
                if (mrem < 0) dat = dat.AddMinutes(-minutes); // go back one it'll be infront
                Console.WriteLine("Got at {0}, no need to check valid", dat);
            }
            else
            {
                var mdiff = ((to.Year - dat.Year) * 12 + to.Month - dat.Month);
                var mrem = mdiff % months;// negative is correct!
                dat = new DateTime(to.Year, to.Month, 1).AddMonths(-mrem);
                if (mrem < 0) dat = dat.AddMonths(months);
                Console.WriteLine("Got at {0}, checking valid days", dat);
                SkipInvalidAt();
                Console.WriteLine("Arrived at {0}", dat);
            }
            var ocr = Current;
            Current = visitor.EvaluateOnOf(on, dat, 6 - on.Length);
            Console.WriteLine("Moved Current from {0} to {1}", ocr, Current);
        }
        public void MoveToNext()
        {
            Console.WriteLine("Moving to next");
            // advance the at date by the split minutes or months
            if (minutes > 0)
            {
                dat = dat.AddMinutes(minutes);
            }
            else
            {
                dat = dat.AddMonths(months);
                SkipInvalidAt();
            }
            Current = visitor.EvaluateOnOf(on, dat, 6 - on.Length);
        }
        void SkipInvalidAt()
        {
            // Deal with mondthdays that dont exist (skip) 
            // buster is here incase some crazy pattern is made which has no existing values (and passed validation somehow)
            while(!visitor.ValidateOnOf(on, every, CalcEnd()).success && dat <= buster)
                dat = dat.AddMonths(months);
        }
        int?[] CalcEnd()
        {
            var endStack = new int?[6 - on.Length];
            for (int i = 0; i < endStack.Length; i++)
            {
                int ival = 0;
                switch (i)
                {
                    // this is ok beacuse we're only working with months here! not days/weeks/lower etc.
                    case 0: ival = dat.Year; break;
                    case 1: ival = dat.Month; break;
                }
                endStack[endStack.Length - 1 - i] = ival;
            }
            return endStack;
        }
    }


    // some thought examples:
    //
    // "0 0 12 | _ _ 1 @ 2 3 2012"  This is invalid since you can't have 12th day of 2nd week (of 3rd month of 2012)
    // "0 0 6 | _ _ 1 @ 2 3 2012"  but this one is ok to say.  Therefore we should not force a format, just raise invalid date error.
    // "0 0 72 | _ _ 1 @ _ _ 2012"  and this one for example is also ok

    /// <summary>
    /// Like CRON, but with support for non factorial steps. e.g "Repeat every 3 days (27 sep, 30 sep, 2 oct, ...)"
    /// Works with two cronlike parts, the "on" part and the "every" part, although the interpretation is different.
    /// 
    /// The crontab format is `minute hour dayofmonth month dayofweek`.
    /// Our format here is a composition of `minute hour day week month year`, 
    /// 
    ///              (m  h | d w M y)@ d w M y
    ///               on     every     at
    /// For example: "30 8 | 3 _ 1 _ @ 4 2 6 2017" 
    ///   literally: "|on|    The 30th minute of the 8th hour
    ///               |every| 1 month and 3 days 
    ///               |at|    the 4th day of the 2nd week of the 6th month of 2017"
    ///      simply: "08:30 every month and 3 days, (±) starting at 18th June 2017" 
    ///   . valid patterns have len(on)+len(every)=6, len(every)=len(at), and every cannot start with _
    ///   . multiple spaces are treated as one
    ///   . _ means not specified, 0 is equivilant to _ on the "every" part, not the "on" part. week (only) is optional in the "at" part if it can appear.
    ///   . negative values are allowed in the "on" part to cope with month days e.g. (-1 means last day of month, in minute -1 == 59)
    ///   . @ is not technically required if it only contains M y or y
    ///   
    /// </summary>
	public class RecurringPattern
    {
        // Thse defaults are nice.
        readonly int?[] on, every, at;
        PartsVisitor visitor;
		public RecurringPattern(String format)
		{
            (on, every, at) = PatternHelpers.ParsePattern(format);
            visitor = new PartsVisitor(on, every, at);
            var m = PatternHelpers.SplitEveryPattern(every, on.Length);
            var val = visitor.ValidateOnOf(on, every, at);
            if (!val.success) throw new ArgumentException(val.error);
        }
		public IEnumerable<DateTime> GetOccurances (DateTime Start, DateTime End)
		{
            PartsIterator p = new PartsIterator(on, every, at, visitor, End);
            p.MoveToClosest(Start);
			while (p.Current < End)
            {
                if(p.Current >= Start) 
				    yield return p.Current;
                p.MoveToNext();
			}
		}
	}
}

