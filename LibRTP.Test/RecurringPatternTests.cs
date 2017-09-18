using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LibRTP.Test
{
    [TestFixture]
    public class HelperTests
    {
        [Test]
        public void OnOf_Full()
        {
            var res = PatternHelpers.EvaluateOnOf(new int?[] { 4, 5, 3, 3, 6, 2017 }, null, 0);
            Assert.AreEqual(new DateTime(2017, 6, 17, 5, 4, 0), res);
        }
        [Test]
        public void OnOf_TopHeavy()
        {
            var res = PatternHelpers.EvaluateOnOf(new int?[] { 3, 3, 6, 2017 }, null, 0);
            Assert.AreEqual(new DateTime(2017, 6, 17, 0, 0, 0), res);
        }
        [Test]
        public void OnOf_BottomHeavy()
        {
            var res = PatternHelpers.EvaluateOnOf(new int?[] { 4, 5, 3, 3 }, new DateTime(2017, 6, 5), 2);
            Assert.AreEqual(new DateTime(2017, 6, 17, 5, 4, 0), res);
        }
        [Test]
        public void OnOf_MidHeavy()
        {
            var res = PatternHelpers.EvaluateOnOf(new int?[] { 3, 3 }, new DateTime(2017, 6, 1), 2);
            Assert.AreEqual(new DateTime(2017, 6, 17, 0, 0, 0), res);
        }
        [Test]
        public void WeekyEval()
        {
            int?[] on = new int?[] { 0, 4, 3 }, at = new int?[] { 1, 2, 2015 };
            var topat = PatternHelpers.EvaluateOnOf(at, null, 0);
            var startingat = PatternHelpers.EvaluateOnOf(on, topat, 6 - on.Length);
            Assert.AreEqual(new DateTime(2015, 2, 1), topat);
            Assert.AreEqual(new DateTime(2015, 2, 3, 4, 0, 0), startingat);
        }
        [Test]
        public void TestFirstWeekOfMonth()
        {
            var wm = PublicHelpers.FirstWeekOfMonth(new DateTime(2015, 2, 1), WeekStartConfig.Unary);
            Assert.AreEqual(new DateTime(2015, 2, 1), wm);
        }
        [Test]
        public void LastWeekOfYear()
        {
            // weekstart whole. i.e. starts 1st jan, ramainter divisible is last week.
            var wy = PatternHelpers.WeeksInYear(2014);
            var dy = PatternHelpers.DaysInYear(2014);
            // days at end of year that arent a complete week
            var p7 = dy % 7;
            // how far from year end the start of that period is (or, it's a whole week)
            var p71 = p7 == 0 ? 7 : p7 - 1;
            // the start is this
            var lws = 31 - p71;
            var lwy = PatternHelpers.EvaluateOnOf(new int?[] { -1, null, 2014 }, null, 0);
            Assert.AreEqual(new DateTime(2014, 12, lws), lwy);
        }
        
        public static vool.voo[] vooex = new vool
        {
            { "0 0 500 _ | 48 _ @ 2 2015", true },
            { "0 0 500 _ | 48 _ @ 22 2015", false },
            { "0 25 500 _ | 48 _ @ 2 2015", false }
        }.ToArray();
        [Test, TestCaseSource("vooex")]
        public void ValidateOnOfs(vool.voo c)
        {
            var oea = PatternHelpers.ParsePattern(c.pat);
            var mm = PatternHelpers.SplitEveryPattern(oea.every, oea.on.Length);
            var vv = PatternHelpers.ValidateOnOf(oea.on, mm.minutes, mm.months, oea.at);
            Assert.AreEqual(c.exp, vv.success, vv.error);
        }
    }
    public class vool : IEnumerable<vool.voo>
    {
        public List<voo> back = new List<voo>();
        public IEnumerator<voo> GetEnumerator() => back.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => back.GetEnumerator();
        public class voo { public String pat; public bool exp; }
        public void Add(String p, bool x) { back.Add(new voo { exp = x, pat = p }); }
    }
    abstract class PatternTestCase
    {
        public String pattern { get; set; }
        public String name { get; set; }
        public DateTime[] range { get; set; }
        public abstract void AssertCase();
        protected DateTime[] Get()
        {
            Console.WriteLine(pattern);
            var pat = new RecurringPattern(pattern);
            var oc = pat.GetOccurances(range[0], range[1]).ToArray();
            return oc;
        }
    }
    class SuccessfulTestCase : PatternTestCase
    {
        public DateTime[] expected { get; set; }

        public override void AssertCase()
        {
            var oc = Get();
            CollectionAssert.AreEqual(expected, oc, "{4}\n pattern: {0}\n   range: {1}\nexpected: {2}\n  actual: {3}",
                pattern,
                range[0] + " to " + range[1],
                String.Join(", ", expected.Select(x => x.ToString("dd/MM/yyyy HH:mm:ss"))),
                String.Join(", ", oc.Select(x => x.ToString("dd/MM/yyyy HH:mm:ss"))),
                name
            );
        }
    }
    class FailureTestCase : PatternTestCase
    {
        public String exception { get; set; }
        public String message { get; set; }
        public override void AssertCase()
        {
            String m = null, e = null;
            try { Get(); }
            catch(Exception ex)
            {
                e = ex.GetType().Name;
                m = ex.Message;
            }
            if (m == null)
                Assert.Fail("Expected exception '{0}: {1}', but didnt get any.", exception, message);
            else
                Assert.Fail("Expected exception '{0}: {1}'\nGot exception '{2}: {3}'", exception, message, e, m);
        }
    }
    class PatternTests
    {
        [TestCaseSource("expected_to_pass")]
        public void AllTests(PatternTestCase t)
        {


            Console.WriteLine(t.name);
            if (Debugger.IsAttached)
            {
                try { t.AssertCase(); }
                catch { Debugger.Break(); t.AssertCase(); }
            }
            else t.AssertCase();
        }
        
        public static IEnumerable<TestCaseData> expected_to_pass
        {
            get
            {
                HashSet<String> use_unique_names_please = new HashSet<string>();
                var wd = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
                var td = Path.Combine(wd, "RecurringPatternTests.json");
                var jdata = File.ReadAllText(td);                
                var converter = new IsoDateTimeConverter { DateTimeFormat = "dd/MM/yyyy HH:mm:ss" };
                foreach (var d in JsonConvert.DeserializeObject<List<SuccessfulTestCase>>(JObject.Parse(jdata)["expected_to_pass"].ToString(), converter))
                {
                    use_unique_names_please.Add(d.name);
                    yield return new TestCaseData(d).SetName(d.name);
                }
            }
        }
        public static List<FailureTestCase> expected_to_fail
        {
            get
            {
                var wd = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
                var td = Path.Combine(wd, "RecurringPatternTests.json");
                var jdata = File.ReadAllText(td);
                var converter = new IsoDateTimeConverter { DateTimeFormat = "dd/MM/yyyy HH:mm:ss" };
                return JsonConvert.DeserializeObject<List<FailureTestCase>>(JObject.Parse(jdata)["expected_to_fail"].ToString(), converter);
            }
        }
    }
}
