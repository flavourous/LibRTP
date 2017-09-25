using System;
using System.Collections.Generic;
using System.IO;
using LibSharpHelp;
using System.Linq;
using System.Collections;
using System.Diagnostics;
using PH = LibRTP.PublicHelpers;

namespace LibRTP
{
    delegate DateTime DateTimeStarter(DateTime d);
    delegate int DateTimeUniter(int year, int month);
    delegate DateTime DateTimeShifter(DateTime src, int val);
    class RUIBuilder : IEnumerable
    {
        public class RUI
        {
            readonly RSpan on, of;
            readonly DateTimeUniter uniter;
            readonly int unitFac;
            readonly DateTimeShifter fromStartPoint;
            readonly DateTimeStarter startPoint;

            delegate int DateTimeMaxer(DateTimeUniter unit, int y, int m, int x, int fac);
            delegate (int div, int rem, int nYr, int nMon) DateTimeDivider(DateTimeUniter unit, int y, int m, int x, int fac);
            int YM(DateTimeUniter det, int ys, int ms, int x, int fac)
            {
                int tot = 0;
                for (int i = 0; i < x; i++)
                    tot += det(ys + i, ms) * fac;
                return tot;
            }
            int MM(DateTimeUniter det, int ys, int ms, int x, int fac)
            {
                int tot = 0;
                for (int i = 0; i < x; i++)
                    tot += det(ys + (ms + i) / 12, (ms + i) % 12) * fac;
                return tot;
            }
            int GM(DateTimeUniter det, int ys, int ms, int x, int fac) => fac * x;

            (int, int, int, int) YD(DateTimeUniter det, int ys, int ms, int o, int fac)
            {
                Debug.Assert(ms == 1); // shouldnt happen in current design.  ms is there to keep sig same.
                int trg = o / fac, rem = o % fac, div = 0, amt = 0, lys = ys;
                while ((amt = det(lys, ms)) <= trg)
                {
                    div++;
                    trg -= amt;
                    lys++;
                }
                rem += trg * fac;
                return (div, rem, lys, ms);
            }
            (int, int, int, int) MD(DateTimeUniter det, int ys, int ms, int o, int fac)
            {
                int trg = o / fac, rem = o % fac, div = 0, amt = 0, lys = ys, lms = ms;
                while ((amt = det(lys, ms)) <= trg)
                {
                    div++;
                    trg -= amt;
                    ms++;
                    if (ms == 13)
                    {
                        ms = 0;
                        lys++;
                    }
                }
                rem += trg * fac;
                return (div, rem, lys, ms);
            }
            (int, int, int, int) GD(DateTimeUniter det, int ys, int ms, int o, int fac) => (o / fac, o % fac, ys, ms);



            // Get the max number of "on" for this many "of" starting here y/M.
            public int MaxValue(int year, int month, int nOf)
            {
                if (month < 0) throw new ArgumentException("month takes negative (last) form as has not been processed");
                DateTimeMaxer use = GM;
                if (this.of == RSpan.Year && this.on != RSpan.Month) use = YM;
                if (this.of == RSpan.Month) use = MM;
                return use(uniter, year, month, nOf, unitFac);
            }
            public (int div, int rem, int year, int month) DivideValue(int year, int month, int of)
            {
                DateTimeDivider use = GD;
                if (this.of == RSpan.Year && this.on != RSpan.Month) use = YD;
                if (this.of == RSpan.Month) use = MD;
                return use(uniter, year, month, of, unitFac);
            }
            public DateTime CreateAtValue(DateTime d, int v)
            {
                var max = MaxValue(d.Year, d.Month, 1);
                var nv = PatternHelpers.ReverseIfNegative(v, max);
                var start = startPoint(d);
                return fromStartPoint(start, nv);
            }
            public RUI(RSpan on, RSpan of, DateTimeUniter uniter, int dateTimeFac, DateTimeStarter startPoint, DateTimeShifter fromStartPoint)
            {
                this.of = on;
                this.on = of;
                this.uniter = uniter;
                this.unitFac = dateTimeFac;
                this.fromStartPoint = fromStartPoint;
                this.startPoint = startPoint;
            }
        }
        Dictionary<RSpan, RUI> rv = new Dictionary<RSpan, RUI>();
        public void Add(RSpan on, RSpan of, DateTimeUniter uniter, int dateTimeFac, DateTimeStarter startPoint, DateTimeShifter fromStartPoint)
        {
            var rs = on | of;
            rv[rs] = new RUI(on, of, uniter, dateTimeFac, startPoint, fromStartPoint);
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
        static class Constants
        {
            public static readonly int DaysInNormalYear, DaysInLeapYear;
            public static readonly int WeeksInNormalYear, WeeksInLeapYear;
            static Constants()
            {
                // Tie to a calc because that's what youd do in the first place.
                int cyear = 2015;
                bool ily = DateTime.IsLeapYear(cyear);
                int cdays = Enumerable.Range(1, 12).Select(x => DateTime.DaysInMonth(cyear, x)).Sum();
                int cdays1 = Enumerable.Range(1, 12).Select(x => DateTime.DaysInMonth(cyear + 1, x)).Sum();
                DaysInLeapYear = ily ? cdays : cdays1;
                DaysInNormalYear = ily ? cdays1 : cdays;
                WeeksInNormalYear = (int)Math.Ceiling(DaysInNormalYear / 7.0);
                WeeksInLeapYear = (int)Math.Ceiling(DaysInLeapYear / 7.0);
            }
        }
        public static int ReverseIfNegative(int of, int max) => of < 0 ? max - (of + 1) : of;
        static bool ValidateMax(RSpan on, int of, int max)
        {
            int use = ReverseIfNegative(of, max);
            switch (on)
            {
                default:
                case RSpan.None:
                    return false;
                case RSpan.Minute:
                case RSpan.Hour:
                    return use >= 0 && use <= max;
                case RSpan.Day:
                case RSpan.Week:
                case RSpan.Month:
                case RSpan.Year:
                    return use > 0 && use <= max;
            }
        }

        public static int WeeksInYear(int year, int month) => DateTime.IsLeapYear(year) ? Constants.WeeksInLeapYear : Constants.WeeksInNormalYear;
        public static int DaysInYear(int year, int month) => DateTime.IsLeapYear(year) ? Constants.DaysInLeapYear : Constants.DaysInNormalYear;
        public static int WeeksInMonth(int year, int month) => (int)Math.Ceiling(DateTime.DaysInMonth(year, month) / 7.0);
        public static int DaysInMonth(int y, int m) => DateTime.DaysInMonth(y, m);
        public static int Identity(int year, int month) => 1;

        public static DateTime StartOfYear(DateTime d) => new DateTime(d.Year, 1, 1);
        public static DateTime StartOfMonth(DateTime d) => new DateTime(d.Year, d.Month, 1);
        public static DateTime StartOfWeek(DateTime value)
        {
            var d1 = new DateTime(value.Year, value.Month, value.Day);
            return d1.AddDays(-d1.DayOfWeekStartingMonday());
        }
        public static DateTime StartOfDay(DateTime d) => new DateTime(d.Year, d.Month, d.Day);
        public static DateTime StartOfHour(DateTime d) => new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0);

        public static DateTime AddMinutes(DateTime d, int n) => d.AddMinutes(n);
        public static DateTime AddHours(DateTime d, int n) => d.AddHours(n);
        public static DateTime AddDaysMinus1(DateTime d, int n) => d.AddDays(n - 1);
        public static DateTime AddWeeksMinus1(DateTime d, int n) => d.AddDays(7 * (n - 1));
        public static DateTime AddMonthsMinus1(DateTime d, int n) => d.AddDays(n - 1);

        // ab: a in b, 00:mm:hh dd/MM/yyyy  (max vals, if you go over here, we allow skip to infinity!)
        public static RSpan[] RSO = new RSpan[] { RSpan.Minute, RSpan.Hour, RSpan.Day, RSpan.Week, RSpan.Month, RSpan.Year };
        public static RUIBuilder Units = new RUIBuilder
        {
            { RSpan.Year,  RSpan.Month,   Identity,     12,      StartOfYear,  AddMonthsMinus1 },
            { RSpan.Year,  RSpan.Week,    WeeksInYear,  1,       StartOfYear,  AddWeeksMinus1  },
            { RSpan.Year,  RSpan.Day,     DaysInYear,   1,       StartOfYear,  AddDaysMinus1   },
            { RSpan.Year,  RSpan.Hour,    DaysInYear,   24,      StartOfYear,  AddHours        },
            { RSpan.Year,  RSpan.Minute,  DaysInYear,   24*60,   StartOfYear,  AddMinutes      },
            { RSpan.Month, RSpan.Week,    WeeksInMonth, 1,       StartOfMonth, AddWeeksMinus1  },
            { RSpan.Month, RSpan.Day,     DaysInMonth,  1,       StartOfMonth, AddDaysMinus1   },
            { RSpan.Month, RSpan.Hour,    DaysInMonth,  24,      StartOfMonth, AddHours        },
            { RSpan.Month, RSpan.Minute,  DaysInMonth,  24*60,   StartOfMonth, AddMinutes      },
            { RSpan.Week,  RSpan.Day,     Identity,     7,       StartOfWeek,  AddDaysMinus1   },
            { RSpan.Week,  RSpan.Hour,    Identity,     7*24,    StartOfWeek,  AddHours        },
            { RSpan.Week,  RSpan.Minute,  Identity,     7*24*60, StartOfWeek,  AddMinutes      },
            { RSpan.Day,   RSpan.Hour,    Identity,     24,      StartOfDay,   AddHours        },
            { RSpan.Day,   RSpan.Minute,  Identity,     24*60,   StartOfDay,   AddMinutes      },
            { RSpan.Hour,  RSpan.Minute,  Identity,     60,      StartOfHour,  AddMinutes      },
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
        // day 900 of every 70 months starting month 7 year 2012
        // 0 125 | 6 _ _ _ @ 1 _ 1 2012 
        // 0 0 900 _ | 70 _ @ 7 2012
        // 0 0 1 _ 500 | 3 @ 2012
        // but also required so that we can detect that day 366+355+355 of every 3 years, basically means to skip non leap years, for example
        public static (bool success, String error) ValidateOnOf(int?[] onStack, int?[] everyStack, int?[] atStack)
        {
            // mark the join: on/at with every
            var osl = onStack.Length;
            RSpan  everyBridge = RSO[osl - 1] | RSO[osl];
            RSpan? onBridge = osl == 1 ? null : (RSpan?)(RSO[osl - 1] | RSO[osl]);

            // prepare start point and year
            int year = atStack.Last().Value;
            int prev = year;
            int month = 1;

            // iterate them
            foreach (var (on, the, of) in IterateStack(onStack.Concat(atStack.Take(atStack.Length - 1)).ToArray(), 1))
            {
                if (on == RSpan.Month) month = the;
                int max = 0;
                // <BIG> of <normal>  - we pass every, e.g. 900th day of january (2015) given 31 months
                if ((on | of) == everyBridge)
                {
                    // so the question is not e.g. can we have day 500 of  month 12, but does 500 days lie within 48 months from month 12 y 2016?

                }
                // <normal> of <BIG>  - we need to figure out what the modulus of <BIG> is
                else if (onBridge.HasValue && (on | of) == onBridge.Value)
                {

                }
                // normally
                else max = Units[on | of].MaxValue(year, prev, 1);

                if(!ValidateMax(on,the,max))
                    return (false, string.Format("There is not a {0} {1} of {3} {2}", on, the, of, prev));
                if(the < 0) prev = ReverseIfNegative(the, max);
                else prev = the;
            }
            return (true, null);
        }

        public static DateTime EvaluateOnOf(int?[] onStack, DateTime? startingWith, int offset)
        {
            if (startingWith == null && (offset != 0 || !onStack.Last().HasValue))
                throw new ArgumentException("Unable to evaluate, no starting year.");
            if (onStack.Length + offset > PatternHelpers.RSO.Length)
                throw new ArgumentException(String.Format("part({0}) or offset({1}) are too long", onStack.Length, offset));
            DateTime forming;
            int?[] useOn;

            if (startingWith.HasValue)
            {
                forming = startingWith.Value;
                useOn = onStack;
            }
            else
            {
                forming = new DateTime(onStack.Last().Value, 1, 1);
                offset = 1;
                useOn = onStack.Take(onStack.Length - 1).ToArray();
            }

            Console.WriteLine(forming);
            foreach (var (on, the, of) in IterateStack(useOn, offset))
            {
                Console.WriteLine("{0} {1} of {2}", on, the, of);
                var use = PatternHelpers.Units[on | of];
                forming = use.CreateAtValue(forming, the);
                Console.WriteLine(forming);
            }
            Console.WriteLine();

            return forming;
        }
        static IEnumerable<(RSpan on, int the, RSpan of)> IterateStack(int?[] onStack, int offset)
        {
            RSpan ofContext = RSO[RSO.Length - offset];
            for (int j = onStack.Length - 1; j >= 0; j--)
            {
                var ri = RSO.Length - 1 - offset++;
                if (onStack[j].HasValue)
                {
                    var onContext = PatternHelpers.RSO[ri];
                    var skip = onStack[j].Value;
                    yield return (onContext, skip, ofContext);
                    ofContext = onContext;
                }
            }
        }
        public static (int minutes, int months) SplitEveryPattern(int?[] every, int onLength)
        {
            var devery = new int[2];
            int[] amt = new int[] { 1, 60, 60 * 24, 60 * 24 * 7, 1, 12 };
            int[] trg = new int[] { 0, 0, 0, 0, 1, 1 };
            for (int i = 0; i < every.Length; i++)
                if (every[i].HasValue)
                    devery[trg[i + onLength]] += every[i].Value * amt[i + onLength];
            if (devery[0] > 0 && devery[1] > 0)
                throw new NotSupportedException("No idea how to do the modulus of every (<month) + every (>=month).");
            return (devery[0], devery[1]);
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
        DateTime dat, buster;
        public PartsIterator(int?[] on, int?[] every, int?[] at, DateTime buster)
        {
            this.buster = buster;
            this.on = on;
            this.every = every;
            this.at = at;
            (minutes, months) = PatternHelpers.SplitEveryPattern(every, on.Length);
            this.dat = PatternHelpers.EvaluateOnOf(at, null, 0);
            Current = PatternHelpers.EvaluateOnOf(on, this.dat, 6 - on.Length);
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
                Console.WriteLine("Got at {0}, no need to check valid", dat);
            }
            else
            {
                var mdiff = ((to.Year - dat.Year) * 12 + to.Month - dat.Month);
                var mrem = mdiff % months;// negative is correct!
                dat = new DateTime(to.Year, to.Month, 1).AddMonths(-mrem);
                Console.WriteLine("Got at {0}, checking valid days", dat);
                SkipInvalidAt();
                Console.WriteLine("Arrived at {0}", dat);
            }
            var ocr = Current;
            Current = PatternHelpers.EvaluateOnOf(on, dat, 6 - on.Length);
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
            Current = PatternHelpers.EvaluateOnOf(on, dat, 6 - on.Length);
        }
        void SkipInvalidAt()
        {
            // Deal with mondthdays that dont exist (skip) 
            // buster is here incase some crazy pattern is made which has no existing values (and passed validation somehow)
            while(!PatternHelpers.ValidateOnOf(on, every, CalcEnd()).success && dat <= buster)
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
		public RecurringPattern(String format)
		{
            (on, every, at) = PatternHelpers.ParsePattern(format);   
            var m = PatternHelpers.SplitEveryPattern(every, on.Length);
            var val = PatternHelpers.ValidateOnOf(on, every, at);
            if (!val.success) throw new ArgumentException(val.error);
        }
		public IEnumerable<DateTime> GetOccurances (DateTime Start, DateTime End)
		{
            PartsIterator p = new PartsIterator(on, every, at, End);
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

