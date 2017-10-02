using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace LibRTP
{
    static class ModulusHelpers
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

        public static double WeeksInYear(int year, int month) => DaysInYear(year, month) / 7.0;
        public static double DaysInYear(int year, int month) => DateTime.IsLeapYear(year) ? Constants.DaysInLeapYear : Constants.DaysInNormalYear;
        public static double WeeksInMonth(int year, int month)
        {
            var wk = DateTime.IsLeapYear(year) || month != 2 ? 5 : 4;
            var dm = DateTime.DaysInMonth(year, month) - wk * 7;
            return wk + dm / 7.0;
        }
        public static double DaysInMonth(int y, int m) => DateTime.DaysInMonth(y, m);
        public static double Identity(int year, int month) => 1;

        public static DateTime StartOfYear(DateTime d) => new DateTime(d.Year, 1, 1);
        public static DateTime StartOfMonth(DateTime d) => new DateTime(d.Year, d.Month, 1);
        // hax, because the input pattern makes the decision about where the start of week is in the `on`->`at` parts,
        // although it's a bit clumsy (on the 3rd day of the 1st week of 2007, because that's wednesdays) due to the 
        // parse protection.  Spec day 3 of week is tricky?
        public static DateTime StartOfWeek(DateTime value) => new DateTime(value.Year, value.Month, value.Day);
        public static DateTime StartOfDay(DateTime d) => new DateTime(d.Year, d.Month, d.Day);
        public static DateTime StartOfHour(DateTime d) => new DateTime(d.Year, d.Month, d.Day, d.Hour, 0, 0);

        public static DateTime AddMinutes(DateTime d, int n) => d.AddMinutes(n);
        public static DateTime AddHours(DateTime d, int n) => d.AddHours(n);
        public static DateTime AddDaysMinus1(DateTime d, int n) => d.AddDays(n - 1);
        public static DateTime AddWeeksMinus1(DateTime d, int n) => d.AddDays(7 * (n - 1));
        public static DateTime AddMonthsMinus1(DateTime d, int n) => d.AddMonths(n - 1);

        public static int YearMaxer(DateTimeUniter det, int ys, int ms, int x, int fac)
        {
            double tot = 0;
            for (int i = 0; i < x; i++)
                tot += det(ys + i, ms) * fac;
            return (int)tot;
        }
        public static int MonthMaxer(DateTimeUniter det, int ys, int ms, int x, int fac)
        {
            double tot = 0;
            for (int i = 0; i < x; i++)
            {
                var msi = ms + i;
                var m12 = msi % 12;
                tot += det(ys + (msi - 1) / 12, m12 == 0 ? 12 : m12) * fac;
            }
            return (int)tot;
        }
        public static int UnitMaxer(DateTimeUniter det, int ys, int ms, int x, int fac) => fac * x;

        public static (int, int, int, int) YearDivider(DateTimeUniter det, int ys, int ms, int o, int fac)
        {
            Debug.Assert(ms == 1); // shouldnt happen in current design.  ms is there to keep sig same.
            int div = 0, lys = ys;
            double amt = 0, trg = o / fac, rem = o % fac;
            while ((amt = det(lys, ms)) <= trg)
            {
                div++;
                trg -= amt;
                lys++;
            }
            rem += trg * fac;
            return (div, (int)rem, lys, ms);
        }
        public static (int, int, int, int) MonthDivider(DateTimeUniter det, int ys, int ms, int o, int fac)
        {
            double trg = o / fac, rem = o % fac, div = 0, amt = 0;
            int lys = ys, lms = ms;
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
            return ((int)div, (int)rem, lys, ms);
        }
        public static (int, int, int, int) UnitDivider(DateTimeUniter det, int ys, int ms, int o, int fac) => (o / fac, o % fac, ys, ms);

        public static int WkMonMaxer(DateTimeUniter det, int ys, int ms, int x, int fac)
        {
            double tot = 0, tail = 0.0;
            for (int i = 0; i < x; i++)
            {
                var msi = ms + i;
                var m12 = msi % 12;
                var tw = det(ys + (msi - 1) / 12, m12 == 0 ? 12 : m12) - tail;
                var fw = Math.Floor(tw);
                tail = tw - fw;
                tot += fw * fac;
            }
            return (int)tot;
        }

        public static (int, int, int, int) WkMonDivider(DateTimeUniter det, int ys, int ms, int o, int fac)
        {
            double trg = o / fac, amt = 0, tail = 0.0;
            int lys = ys, lms = ms, rem = o % fac, div = 0;
            while ((amt = det(lys, lms) - tail) <= trg)
            {
                div++;
                var fam = Math.Floor(amt);
                tail = amt - fam;
                amt = fam;
                trg -= amt;
                lms++;
                if (lms == 13)
                {
                    lms = 0;
                    lys++;
                }
            }
            rem += (int)trg * fac;
            return (div, rem, lys, lms);
        }

        public static int WkYearMaxer(DateTimeUniter det, int ys, int ms, int x, int fac)
        {
            double tot = 0, tail = 0.0;
            for (int i = 0; i < x; i++)
            {
                var tw = det(ys + i, ms) - tail;
                var fw = Math.Floor(tw);
                tail = tw - fw;
                tot += fw * fac;
            }
            return (int)tot;
        }

        public static (int, int, int, int) WkYearDivider(DateTimeUniter det, int ys, int ms, int o, int fac)
        {
            Debug.Assert(ms == 1); // shouldnt happen in current design.  ms is there to keep sig same.
            double trg = o / fac, amt = 0, tail = 0.0;
            int lys = ys, rem = o % fac, div = 0;
            while ((amt = det(lys, ms) - tail) <= trg)
            {
                div++;
                var fam = Math.Floor(amt);
                tail = amt - fam;
                amt = fam;
                trg -= amt;
                lys++;
            }
            rem += (int)trg * fac;
            return (div, rem, lys, ms);
        }
    }
}
