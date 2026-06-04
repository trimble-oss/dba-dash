using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    [TestClass]
    public class CronParserTests
    {
        [TestMethod]
        public void GroupedBySchedule_NormalizesEquivalentSchedules()
        {
            var schedules = new DBADashService.CollectionSchedules
            {
                [DBADash.CollectionType.CPU] = new DBADashService.CollectionSchedule { Schedule = "0 0/1 * * * ?" },
                [DBADash.CollectionType.IOStats] = new DBADashService.CollectionSchedule { Schedule = "0 * * ? * *" },
                [DBADash.CollectionType.Waits] = new DBADashService.CollectionSchedule { Schedule = "0 0/1 * * * ?" }
            };

            var grouped = schedules.GroupedBySchedule;

            Assert.AreEqual(1, grouped.Count);
            Assert.IsTrue(grouped.ContainsKey(CronParser.NormalizeCronExpression("0 0/1 * * * ?")));
            CollectionAssert.AreEquivalent(
                new[] { DBADash.CollectionType.CPU, DBADash.CollectionType.IOStats, DBADash.CollectionType.Waits },
                grouped[CronParser.NormalizeCronExpression("0 0/1 * * * ?")]);
        }

        [TestMethod]
        public void TryParseCronState_EveryNMinutes_Various()
        {
            var cases = new[] {
                (cron: "0 0/5 * * * ?", expectedInterval: 5, expectedBaseMinute: 0),
                (cron: "0 2/10 * * * ?", expectedInterval: 10, expectedBaseMinute: 2),
                (cron: "0 * * ? * *", expectedInterval: 1, expectedBaseMinute: 0)
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
        public void TryParseCronState_EveryNHours_Various()
        {
            var cases = new[] {
                (cron: "0 0 0/1 * * ?", expectedInterval: 1, expectedBaseHour: 0, expectedBaseMinute: 0),
                (cron: "0 0 * ? * *", expectedInterval: 1, expectedBaseHour: 0, expectedBaseMinute: 0)
            };

            foreach (var c in cases)
            {
                Assert.IsTrue(CronParser.TryParseCronState(c.cron, out var state), $"Parsing failed for: {c.cron}");
                Assert.AreEqual(CronParser.FrequencyMode.EveryNHours, state.Mode);
                Assert.AreEqual(c.expectedInterval, state.Interval);
                Assert.AreEqual(c.expectedBaseHour, state.BaseHour);
                Assert.AreEqual(c.expectedBaseMinute, state.BaseMinute);
                Assert.AreEqual(0, state.BaseSecond);
            }
        }

        [TestMethod]
        public void TryParseCronState_Daily_Various()
        {
            var cases = new[] {
                (cron: "0 0 0 * * ?", expectedHour: 0, expectedMinute: 0, expectedSecond: 0),
                (cron: "0 0 23 * * ?", expectedHour: 23, expectedMinute: 0, expectedSecond: 0),
                (cron: "0 0 22 * * ?", expectedHour: 22, expectedMinute: 0, expectedSecond: 0),
                (cron: "0 0 0 1/1 * ? *", expectedHour: 0, expectedMinute: 0, expectedSecond: 0),
                (cron: "0 0 23 1/1 * ? *", expectedHour: 23, expectedMinute: 0, expectedSecond: 0),
                (cron: "0 0 22 1/1 * ? *", expectedHour: 22, expectedMinute: 0, expectedSecond: 0)
            };

            foreach (var c in cases)
            {
                Assert.IsTrue(CronParser.TryParseCronState(c.cron, out var state), $"Parsing failed for: {c.cron}");
                Assert.AreEqual(CronParser.FrequencyMode.Daily, state.Mode);
                Assert.AreEqual(c.expectedHour, state.BaseHour);
                Assert.AreEqual(c.expectedMinute, state.BaseMinute);
                Assert.AreEqual(c.expectedSecond, state.BaseSecond);
            }
        }

        [TestMethod]
        public void NormalizeCronExpression_EquivalentForms_MapToSameValue()
        {
            Assert.AreEqual(CronParser.NormalizeCronExpression("0 0 * ? * *"), CronParser.NormalizeCronExpression("0 0 0/1 * * ?"));
            Assert.AreEqual(CronParser.NormalizeCronExpression("0 0 0 1/1 * ? *"), CronParser.NormalizeCronExpression("0 0 0 * * ?"));
            Assert.AreEqual(CronParser.NormalizeCronExpression("0 0 23 1/1 * ? *"), CronParser.NormalizeCronExpression("0 0 23 * * ?"));
            Assert.AreEqual(CronParser.NormalizeCronExpression("0 0 22 1/1 * ? *"), CronParser.NormalizeCronExpression("0 0 22 * * ?"));
        }

        [TestMethod]
        public void NormalizeCronExpression_AllDaysSelection_UsesDailyCanonicalForm()
        {
            var normalized = CronParser.NormalizeCronExpression("0 0 3 ? * SUN,MON,TUE,WED,THU,FRI,SAT");

            Assert.AreEqual("0 0 3 * * ?", normalized);
            Assert.IsTrue(CronParser.TryParseCronState(normalized, out var state));
            Assert.AreEqual(CronParser.FrequencyMode.Daily, state.Mode);
        }

        [TestMethod]
        public void EquivalentEveryMinuteForms_ParseTheSame()
        {
            Assert.IsTrue(CronParser.TryParseCronState("0 0/1 * * * ?", out var stepState));
            Assert.IsTrue(CronParser.TryParseCronState("0 * * ? * *", out var legacyState));

            Assert.AreEqual(stepState.Mode, legacyState.Mode);
            Assert.AreEqual(stepState.Interval, legacyState.Interval);
            Assert.AreEqual(stepState.BaseMinute, legacyState.BaseMinute);
            Assert.AreEqual(stepState.BaseSecond, legacyState.BaseSecond);
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
        [DataRow("0 0 3 ? * MON,FRI,MON", "0 0 3 ? * MON,FRI")]
        [DataRow("0 0 3 ? * FRI,MON", "0 0 3 ? * MON,FRI")]
        [DataRow("0 0 3 ? * MON-FRI", "0 0 3 ? * MON-FRI")]
        [DataRow("0 0 3 ? * FRI-MON", "0 0 3 ? * FRI-MON")]
        [DataRow("0 0 3 ? * MON,TUE,MON,TUE", "0 0 3 ? * MON-TUE")]
        public void NormalizeCronExpression_SortsAndDeduplicatesSelectedDays(string cron, string expected)
        {
            Assert.AreEqual(expected, CronParser.NormalizeCronExpression(cron));
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
