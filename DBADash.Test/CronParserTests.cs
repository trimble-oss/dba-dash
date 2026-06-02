using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    [TestClass]
    public class CronParserTests
    {
        [TestMethod]
        public void TryParseCronState_EveryNMinutes_Various()
        {
            var cases = new[] {
                (cron: "0 0/5 * * * ?", expectedInterval: 5, expectedBaseMinute: 0),
                (cron: "0 2/10 * * * ?", expectedInterval: 10, expectedBaseMinute: 2)
            };
            foreach (var c in cases)
            {
                Assert.IsTrue(CronParser.TryParseCronState(c.cron, out var state), $"Parsing failed for: {c.cron}");
                Assert.AreEqual(CronParser.FrequencyMode.EveryNMinutes, state.Mode);
                Assert.AreEqual(c.expectedInterval, state.Interval);
                Assert.AreEqual(0, state.BaseSecond);
                Assert.AreEqual(c.expectedBaseMinute, state.BaseMinute);
            }
        }

        [TestMethod]
        public void RangeExpandCompress()
        {
            var cases = new[] {
                (cron: "0 0 3 ? * MON-FRI", expectedExpandedCsv: "MON,TUE,WED,THU,FRI", expectedCompressed: "MON-FRI"),
                (cron: "0 0 3 ? * FRI-MON", expectedExpandedCsv: "FRI,SAT,SUN,MON", expectedCompressed: "FRI-MON")
            };
            foreach (var c in cases)
            {
                Assert.IsTrue(CronParser.TryParseCronState(c.cron, out var state), $"Parsing failed for: {c.cron}");
                var expectedExpanded = c.expectedExpandedCsv.Split(',');
                CollectionAssert.AreEqual(expectedExpanded, state.SelectedDays);
                var compressed = CronParser.CompressDayTokens(state.SelectedDays);
                CollectionAssert.AreEqual(new[] { c.expectedCompressed }, compressed);
            }
        }

        [TestMethod]
        public void AllDays_CompressesToStar()
        {
            var all = new[] { "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT" };
            var compressed = CronParser.CompressDayTokens(all);
            CollectionAssert.AreEqual(new[] { "*" }, compressed);
        }

        [TestMethod]
        public void TryParseCronState_InvalidCron_ReturnsFalse()
        {
            Assert.IsFalse(CronParser.TryParseCronState("not a cron", out var _));
        }

        [TestMethod]
        public void NumericDayTokens_ExpandAndCompress()
        {
            // single numeric tokens
            var one = CronParser.NormalizeDayTokens(new[] { "1" });
            CollectionAssert.AreEqual(new[] { "SUN" }, one);
            var seven = CronParser.NormalizeDayTokens(new[] { "7" });
            CollectionAssert.AreEqual(new[] { "SAT" }, seven);

            // numeric range
            var range = CronParser.NormalizeDayTokens(new[] { "1-5" });
            CollectionAssert.AreEqual(new[] { "SUN", "MON", "TUE", "WED", "THU" }, range);
            var compressed = CronParser.CompressDayTokens(range);
            CollectionAssert.AreEqual(new[] { "SUN-THU" }, compressed);
        }

        [TestMethod]
        public void MalformedRanges_ReturnNullAndParserRejects()
        {
            Assert.IsNull(CronParser.NormalizeDayTokens(new[] { "MON-" }));
            Assert.IsNull(CronParser.NormalizeDayTokens(new[] { "MON-XXX" }));
            Assert.IsFalse(CronParser.TryParseCronState("0 0 3 ? * MON-", out var _));
            Assert.IsFalse(CronParser.TryParseCronState("0 0 3 ? * MON-XXX", out var _));
        }

        [TestMethod]
        public void InvalidNumericIntervals_NonNumericAndZero()
        {
            // non-numeric interval should be rejected
            Assert.IsFalse(CronParser.TryParseCronState("0 0/abc * * * ?", out var _));
            Assert.IsFalse(CronParser.TryParseCronState("0/abc * * * * ?", out var _));

            // zero interval should be rejected
            Assert.IsFalse(CronParser.TryParseCronState("0 0/0 * * * ?", out var _));
            Assert.IsFalse(CronParser.TryParseCronState("0/0 * * * * ?", out var _));
            Assert.IsFalse(CronParser.TryParseCronState("0 0 0/0 * * ?", out var _));
        }

        [TestMethod]
        public void MixedTokens_ExpandCompress()
        {
            // MON,3,WED-FRI -> MON,TUE,WED,THU,FRI -> compress to MON-FRI
            var normalized = CronParser.NormalizeDayTokens(new[] { "MON", "3", "WED-FRI" });
            CollectionAssert.AreEqual(new[] { "MON", "TUE", "WED", "THU", "FRI" }, normalized);
            var compressed = CronParser.CompressDayTokens(normalized);
            CollectionAssert.AreEqual(new[] { "MON-FRI" }, compressed);
        }
    }
}
