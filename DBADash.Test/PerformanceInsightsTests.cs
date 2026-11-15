using DBADash.DailyReport;
using DBADash.PerformanceInsights;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.Test
{
    [TestClass]
    public class PerformanceInsightsTests
    {
        private const int InstanceID = 1;

        /// <summary>Monday 09:00 to 15:00 UTC</summary>
        private static readonly DateTime From = new(2026, 11, 9, 9, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime To = From.AddHours(6);
        private static readonly DateTime Now = To.AddHours(1);

        private static PerformanceHour Hour(int period, int offset, double cpu = 20, long waitMs = 3_600_000) => new()
        {
            Period = period,
            HourOffset = offset,
            AvgCPU = cpu,
            MaxCPU = cpu + 5,
            CPUSamples = 60,
            HighCPUSamples = cpu >= 80 ? 60 : 0,
            WaitMs = waitMs,
            WaitSampleMs = 3_600_000,
            CriticalWaitMs = 0,
            LockWaitMs = 0
        };

        /// <param name="current">Period 0's hours, or null for the same as the baseline</param>
        private static PerformanceData Data(InsightWindow window, int baselineWeeks, Func<int, PerformanceHour>? current = null, IEnumerable<int>? missing = null)
        {
            var instance = new InstancePerformanceData { InstanceID = InstanceID, Instance = "SQL1" };
            var skip = missing?.ToHashSet() ?? new HashSet<int>();
            for (var hour = 0; hour < window.Hours; hour++)
            {
                if (!skip.Contains(hour)) instance.Hours.Add(current?.Invoke(hour) ?? Hour(0, hour));
                for (var p = 1; p <= baselineWeeks; p++) instance.Hours.Add(Hour(p, hour));
            }
            var data = new PerformanceData { WindowEndUtc = window.EndUtc, WindowHours = window.Hours, BaselineWeeks = window.BaselineWeeks };
            data.Instances.Add(instance);
            return data;
        }

        private static List<PerformanceInsightItem> Build(InsightWindow window, PerformanceData? data, PerformanceInsightsContext? context = null, List<InsightCounter>? counters = null) =>
            PerformanceInsightBuilder.Build(new PerformanceInsightsInput
            {
                InstanceID = InstanceID,
                Window = window,
                Performance = data,
                Context = context,
                Counters = counters,
                NowUtc = Now
            });

        #region Window

        [TestMethod]
        public void Window_IsWidenedToWholeHours()
        {
            var w = InsightWindow.For(From.AddMinutes(37), From.AddHours(1).AddMinutes(15));
            Assert.AreEqual(From, w.StartUtc);
            Assert.AreEqual(From.AddHours(2), w.EndUtc);
            Assert.AreEqual(2, w.Hours);
            Assert.IsTrue(w.IsRounded);
            Assert.IsTrue(w.IsCompared);
        }

        [TestMethod]
        public void Window_OnTheHour_IsNotRounded()
        {
            var w = InsightWindow.For(From, To);
            Assert.AreEqual(6, w.Hours);
            Assert.IsFalse(w.IsRounded);
        }

        [TestMethod]
        public void Window_OfAWeek_IsCompared_LongerIsNot()
        {
            Assert.IsTrue(InsightWindow.For(From, From.AddDays(7)).IsCompared);
            var eightDays = InsightWindow.For(From, From.AddDays(8));
            Assert.IsFalse(eightDays.IsCompared);
            Assert.AreEqual(0, eightDays.BaselineWeeks);
            Assert.IsFalse(eightDays.IsTooLong);
            Assert.IsTrue(InsightWindow.For(From, From.AddDays(40)).IsTooLong);
        }

        [TestMethod]
        public void HoursFor_KeepsTheFilteredHoursInEveryPeriod()
        {
            var w = InsightWindow.For(From, To);
            // 09:00-15:00 UTC, keep 10:00 and 11:00
            var hours = PerformanceData.HoursFor(w.EndUtc, w.Hours, 2, TimeZoneInfo.Utc, local => local.Hour is 10 or 11);
            CollectionAssert.AreEquivalent(new[] { (0, 1), (0, 2), (1, 1), (1, 2), (2, 1), (2, 2) }, hours.ToList());
        }

        #endregion

        #region Performance

        [TestMethod]
        public void NothingAboveBaseline_GivesOnlyTheSummary()
        {
            var w = InsightWindow.For(From, To);
            var cards = Build(w, Data(w, 4));
            Assert.AreEqual(1, cards.Count);
            Assert.IsTrue(cards[0].IsSummary);
            StringAssert.Contains(cards[0].Text, "in line with baseline for the time");
            StringAssert.Contains(cards[0].Text, "previous 4 weeks");
        }

        [TestMethod]
        public void CPUAboveBaseline_IsAWarningWithAZoomToTheWorstHour()
        {
            var w = InsightWindow.For(From, To);
            // 70% at 12:00 (offset 3) against a baseline of 20% - under the 75% warning threshold, but well above the baseline
            var cards = Build(w, Data(w, 4, h => Hour(0, h, cpu: h == 3 ? 70 : 20)));
            var cpu = cards.Single(c => c.Category == InsightCategory.Performance && c.Text.StartsWith("CPU"));
            Assert.AreEqual(InsightLevel.Warning, cpu.Level);
            StringAssert.Contains(cpu.Text, "above baseline for the time");
            StringAssert.Contains(cpu.Text, "baseline 20%");
            var zoom = cpu.Links.Single(l => l.ZoomFromUtc.HasValue);
            Assert.AreEqual(From.AddHours(3), zoom.ZoomFromUtc);
            Assert.AreEqual(From.AddHours(4), zoom.ZoomToUtc);
            StringAssert.Contains(cpu.Text, $"(action:{zoom.Key})");
        }

        [TestMethod]
        public void CPUOverCriticalThreshold_IsCritical()
        {
            var w = InsightWindow.For(From, To);
            var cards = Build(w, Data(w, 4, h => Hour(0, h, cpu: 95)));
            var cpu = cards.First(c => c.Category == InsightCategory.Performance);
            Assert.AreEqual(InsightLevel.Critical, cpu.Level);
            StringAssert.Contains(cpu.Text, "critical threshold");
            Assert.AreEqual(cpu, cards[0], "Critical cards come first");
        }

        [TestMethod]
        public void OneBaselineWeek_IsNotEnoughToSayAboveBaseline()
        {
            var w = InsightWindow.For(From, To);
            var cards = Build(w, Data(w, 1, h => Hour(0, h, cpu: h == 3 ? 70 : 20)));
            Assert.IsFalse(cards.Any(c => c.Category == InsightCategory.Performance));
            var summary = cards.Single(c => c.IsSummary);
            StringAssert.Contains(summary.Text, "thresholds only");
            StringAssert.Contains(summary.Text, "only 1 week of data");
        }

        [TestMethod]
        public void WaitsAboveBaseline_LinksToTheWaitsTab()
        {
            var w = InsightWindow.For(From, To);
            var cards = Build(w, Data(w, 4, h => Hour(0, h, waitMs: 3_600_000 * 4)));
            var waits = cards.Single(c => c.Text.StartsWith("Total waits"));
            Assert.IsTrue(waits.Links.Any(l => l.Tab == "Waits"));
        }

        [TestMethod]
        public void LongerThanAWeek_IsThresholdsOnly()
        {
            var w = InsightWindow.For(From, From.AddDays(10));
            var cards = Build(w, Data(w, 0));
            StringAssert.Contains(cards.Single(c => c.IsSummary).Text, "more than a week isn't compared");
        }

        [TestMethod]
        public void TooLong_SaysSoAndSkipsTheFigures()
        {
            var w = InsightWindow.For(From, From.AddDays(40));
            var cards = Build(w, null);
            StringAssert.Contains(cards.Single(c => c.IsSummary).Text, "more than 31 days");
        }

        [TestMethod]
        public void NoFiguresAtAll_IsOneCollectionCard()
        {
            var w = InsightWindow.For(From, To);
            var cards = Build(w, Data(w, 4, missing: Enumerable.Range(0, 6)));
            var card = cards.Single(c => !c.IsSummary);
            Assert.AreEqual(InsightCategory.DataCollection, card.Category);
            StringAssert.Contains(card.Text, "No CPU, IO or wait figures");
        }

        [TestMethod]
        public void MissingHours_AreAGap()
        {
            var w = InsightWindow.For(From, To);
            var cards = Build(w, Data(w, 4, missing: new[] { 2, 3 }));
            var gap = cards.Single(c => c.Category == InsightCategory.DataCollection);
            Assert.AreEqual(InsightLevel.Warning, gap.Level);
            StringAssert.Contains(gap.Text, "2 hours (11:00-13:00)");
        }

        [TestMethod]
        public void TheHourJustStarted_IsNotAGap()
        {
            var w = InsightWindow.For(From, To);
            var input = new PerformanceInsightsInput
            {
                InstanceID = InstanceID,
                Window = w,
                Performance = Data(w, 4, missing: new[] { 5 }),
                NowUtc = To.AddHours(-1).AddMinutes(5) // 14:05 - the 14:00 hour has barely started
            };
            Assert.IsFalse(PerformanceInsightBuilder.Build(input).Any(c => c.Category == InsightCategory.DataCollection));
        }

        [TestMethod]
        public void NewInstance_HoursBeforeItsFirstFigures_AreNotAGap()
        {
            var w = InsightWindow.For(From, To);
            var data = Data(w, 0, missing: new[] { 0, 1 });
            Assert.IsFalse(Build(w, data).Any(c => c.Category == InsightCategory.DataCollection));
        }

        #endregion

        #region What happened

        [TestMethod]
        public void OpenHighAlert_IsCritical_ResolvedOnlyIsInfo()
        {
            var w = InsightWindow.For(From, To);
            var context = new PerformanceInsightsContext();
            context.Alerts.Add(new InsightAlert { IsOpen = true, AlertType = "CPU", Message = "CPU 95%", Priority = 1, TriggerDateUtc = From.AddHours(1) });
            var open = Build(w, Data(w, 4), context).Single(c => c.Category == InsightCategory.Alerts);
            Assert.AreEqual(InsightLevel.Critical, open.Level);
            StringAssert.Contains(open.Text, "1 alert open: CPU 95% (high, raised 10:00)");
            Assert.IsTrue(open.Links.Any(l => l.Tab == "Alerts"));

            context = new PerformanceInsightsContext();
            context.Alerts.Add(new InsightAlert { IsOpen = false, AlertType = "Blocking", Message = "Blocked 60s", Priority = 1, TriggerDateUtc = From.AddHours(1), ResolvedDateUtc = From.AddHours(2) });
            var resolved = Build(w, Data(w, 4), context).Single(c => c.Category == InsightCategory.Alerts);
            Assert.AreEqual(InsightLevel.Info, resolved.Level);
            StringAssert.Contains(resolved.Text, "1 alert resolved in this period: Blocking - Blocked 60s (high, raised 10:00)");
        }

        [TestMethod]
        public void AlertResolvedAfterTheRange_WasOpenAtItsEnd()
        {
            // Looking back at Monday 09:00-15:00: raised at 10:00, resolved on Wednesday - open then, not resolved in the range
            var w = InsightWindow.For(From, To);
            var context = new PerformanceInsightsContext();
            context.Alerts.Add(new InsightAlert { IsOpen = false, AlertType = "CPU", Message = "CPU 95%", Priority = 1, TriggerDateUtc = From.AddHours(1), ResolvedDateUtc = From.AddDays(2) });
            var card = Build(w, Data(w, 4), context).Single(c => c.Category == InsightCategory.Alerts);
            StringAssert.Contains(card.Text, "1 alert open at the end of this period: CPU 95% (high, raised 10:00, since resolved)");
            Assert.IsFalse(card.Text.Contains("resolved in this period"));
            Assert.AreEqual(InsightLevel.Info, card.Level, "Not open now");
        }

        [TestMethod]
        public void BlackoutAlerts_AreLeftOut()
        {
            var w = InsightWindow.For(From, To);
            var context = new PerformanceInsightsContext();
            context.Alerts.Add(new InsightAlert { IsOpen = true, AlertType = "CPU", Message = "CPU", Priority = 1, IsBlackout = true, TriggerDateUtc = From });
            Assert.IsFalse(Build(w, Data(w, 4), context).Any(c => c.Category == InsightCategory.Alerts));
        }

        [TestMethod]
        public void Restart_InTheRange_IsAWarning_BeforeItIsNothing()
        {
            var w = InsightWindow.For(From, To);
            var restart = Build(w, Data(w, 4), new PerformanceInsightsContext { StartTimeUtc = From.AddHours(2) })
                .Single(c => c.Category == InsightCategory.Restart);
            Assert.AreEqual(InsightLevel.Warning, restart.Level);
            StringAssert.Contains(restart.Text, "restarted at 11:00");

            Assert.IsFalse(Build(w, Data(w, 4), new PerformanceInsightsContext { StartTimeUtc = From.AddDays(-3) })
                .Any(c => c.Category == InsightCategory.Restart));
        }

        [TestMethod]
        public void StillOffline_IsCritical()
        {
            var w = InsightWindow.For(From, To);
            var context = new PerformanceInsightsContext();
            context.Offline.Add(new InsightOfflinePeriod { FirstFailUtc = From.AddHours(4), LastFailUtc = To, IsCurrent = true, FailCount = 10 });
            var card = Build(w, Data(w, 4), context).Single(c => c.Category == InsightCategory.Offline);
            Assert.AreEqual(InsightLevel.Critical, card.Level);
            StringAssert.Contains(card.Text, "since 13:00 (still offline)");
        }

        [TestMethod]
        public void Failover_LinksToAGAndZooms()
        {
            var w = InsightWindow.For(From, To);
            var context = new PerformanceInsightsContext();
            context.RoleChanges.Add(new InsightRoleChange { AvailabilityGroup = "AG1", PreviousRole = "SECONDARY", NewRole = "PRIMARY", ChangedUtc = From.AddHours(1) });
            var card = Build(w, Data(w, 4), context).Single(c => c.Category == InsightCategory.Failover);
            StringAssert.Contains(card.Text, "AG1 secondary to primary at 10:00");
            Assert.IsTrue(card.Links.Any(l => l.Tab == "AG"));
            Assert.IsTrue(card.Links.Any(l => l.ZoomFromUtc == From.AddMinutes(30)));
        }

        [TestMethod]
        public void Counters_OutsideThresholds_AreListedWorstFirst()
        {
            var w = InsightWindow.For(From, To);
            var counters = new List<InsightCounter>
            {
                new() { CounterName = "Page life expectancy", Status = 2, Measure = "Min", Value = 120 },
                new() { CounterName = "Memory Grants Pending", Status = 1, Measure = "Max", Value = 5 }
            };
            var card = Build(w, Data(w, 4), counters: counters).Single(c => c.Category == InsightCategory.Counters);
            Assert.AreEqual(InsightLevel.Critical, card.Level);
            StringAssert.StartsWith(card.Text, "2 performance counters outside their thresholds: Memory Grants Pending max 5 (critical); Page life expectancy min 120 (warning).");
        }

        [TestMethod]
        public void Jobs_FailedAreAWarning_LongRunsAreInfo()
        {
            var w = InsightWindow.For(From, To);
            var context = new PerformanceInsightsContext();
            context.Jobs.Add(new InsightJobRun { JobName = "Backup", StartUtc = From.AddHours(1), DurationSec = 60, RunStatus = 0 });
            context.Jobs.Add(new InsightJobRun { JobName = "IndexOptimize", StartUtc = From, DurationSec = 3900, RunStatus = 1 });
            var cards = Build(w, Data(w, 4), context).Where(c => c.Category == InsightCategory.Jobs).ToList();
            Assert.AreEqual(2, cards.Count);
            var failed = cards.Single(c => c.Level == InsightLevel.Warning);
            StringAssert.Contains(failed.Text, "Backup at 10:00 (failed)");
            var longRun = cards.Single(c => c.Level == InsightLevel.Info);
            StringAssert.Contains(longRun.Text, "IndexOptimize 09:00 for 1h 5m");
        }

        [TestMethod]
        public void ConfigChanges_LinkToTheirTabs()
        {
            var w = InsightWindow.For(From, To);
            var context = new PerformanceInsightsContext();
            context.ConfigChanges.Add(new InsightConfigChange { ChangeType = "Configuration", Name = "max degree of parallelism", OldValue = "8", NewValue = "4", ChangeDateUtc = From.AddHours(2) });
            context.ConfigChanges.Add(new InsightConfigChange { ChangeType = "Trace flag", Name = "Trace flag 1117", OldValue = "Off", NewValue = "On", ChangeDateUtc = From.AddHours(3) });
            var card = Build(w, Data(w, 4), context).Single(c => c.Category == InsightCategory.Changes);
            StringAssert.Contains(card.Text, "max degree of parallelism 8 to 4 at 11:00");
            StringAssert.Contains(card.Text, "Trace flag 1117 turned on at 12:00");
            CollectionAssert.AreEquivalent(new[] { "Configuration", "TraceFlags" }, card.Links.Select(l => l.Tab).ToList());
        }

        [TestMethod]
        public void TooManyCards_AreCountedInTheLast()
        {
            var w = InsightWindow.For(From, To);
            var context = new PerformanceInsightsContext { StartTimeUtc = From.AddHours(1) };
            context.Alerts.Add(new InsightAlert { IsOpen = true, AlertType = "A", Message = "A", Priority = 1, TriggerDateUtc = From });
            context.Offline.Add(new InsightOfflinePeriod { FirstFailUtc = From, LastFailUtc = From.AddMinutes(5) });
            context.RoleChanges.Add(new InsightRoleChange { AvailabilityGroup = "AG", PreviousRole = "SECONDARY", NewRole = "PRIMARY", ChangedUtc = From });
            context.Jobs.Add(new InsightJobRun { JobName = "J1", StartUtc = From, DurationSec = 10, RunStatus = 0 });
            context.Jobs.Add(new InsightJobRun { JobName = "J2", StartUtc = From, DurationSec = 600, RunStatus = 1 });
            context.ConfigChanges.Add(new InsightConfigChange { ChangeType = "Configuration", Name = "x", OldValue = "1", NewValue = "2", ChangeDateUtc = From });
            context.CollectionErrors.Add(new InsightCollectionError { Source = "CPU", Errors = 3 });
            var counters = new List<InsightCounter> { new() { CounterName = "C", Status = 2, Measure = "Avg", Value = 1 } };
            var data = Data(w, 4, h => Hour(0, h, cpu: 95, waitMs: 3_600_000 * 10));
            var cards = Build(w, data, context, counters);
            Assert.AreEqual(PerformanceInsightBuilder.MaxCards + 1, cards.Count, "The cap, plus the summary note");
            StringAssert.StartsWith(cards[PerformanceInsightBuilder.MaxCards - 1].Text, "And ");
            Assert.IsTrue(cards[^1].IsSummary);
        }

        #endregion

        [TestMethod]
        public void Ranges_GroupConsecutiveHours()
        {
            var ranges = PerformanceInsightBuilder.Ranges(new[] { 7, 1, 2, 3 });
            CollectionAssert.AreEqual(new[] { (1, 3), (7, 7) }, ranges);
        }

        [TestMethod]
        public void BaselineByHour_IsTheMedianOfTheBaselineWeeks()
        {
            var hours = new[]
            {
                Hour(0, 0, cpu: 99),
                Hour(1, 0, cpu: 10), Hour(2, 0, cpu: 20), Hour(3, 0, cpu: 90),
                Hour(1, 1, cpu: 30)
            };
            var baseline = PerformanceAnalyzer.BaselineByHour(hours, 3, h => h.AvgCPU, minWeeks: 2);
            Assert.AreEqual(1, baseline.Count, "Hour 1 has one week only");
            Assert.AreEqual(20, baseline[0]);
        }

        [TestMethod]
        public void RangeWeights_GiveEachHourItsShareOfTheRange()
        {
            // 14:50-15:50 at 15:50: 10 minutes of the 14:00 hour and the 50 so far of the 15:00 hour
            var from = From.AddHours(5).AddMinutes(50);
            var now = from.AddHours(1);
            var w = InsightWindow.For(from, now);
            var weights = PerformanceData.RangeWeights(w.StartUtc, w.Hours, 2, w.FromUtc, w.ToUtc, now);
            Assert.AreEqual(10 / 60.0, weights[(0, 0)], 1e-9, "Current 14:00: 10 of its 60 minutes");
            Assert.AreEqual(1, weights[(0, 1)], 1e-9, "Current 15:00: all 50 minutes it has so far");
            Assert.AreEqual(10 / 60.0, weights[(1, 0)], 1e-9, "Baseline 14:00: 10 minutes' share");
            Assert.AreEqual(50 / 60.0, weights[(2, 1)], 1e-9, "Baseline 15:00: 50 minutes' share of a whole hour");
        }

        [TestMethod]
        public void HourInProgress_IsComparedWithItsShareOfTheBaselineHour()
        {
            // 14:00-14:30 at 14:30.  6 deadlocks in the 30 minutes so far, against 6 an hour usually: twice the rate.  Compared with the whole
            // baseline hour it would look normal
            var from = From.AddHours(5);
            var now = from.AddMinutes(30);
            var w = InsightWindow.For(from, now);
            PerformanceHour WithDeadlocks(int period, int deadlocks) => new() { Period = period, HourOffset = 0, AvgCPU = 20, CPUSamples = period == 0 ? 30 : 60, Deadlocks = deadlocks };
            var data = new PerformanceData { WindowEndUtc = w.EndUtc, WindowHours = w.Hours, BaselineWeeks = w.BaselineWeeks };
            var instance = new InstancePerformanceData { InstanceID = InstanceID, Instance = "SQL1" };
            instance.Hours.Add(WithDeadlocks(0, 6));
            for (var p = 1; p <= 4; p++) instance.Hours.Add(WithDeadlocks(p, 6));
            data.Instances.Add(instance);

            var cards = PerformanceInsightBuilder.Build(new PerformanceInsightsInput { InstanceID = InstanceID, Window = w, Performance = data, NowUtc = now });
            StringAssert.Contains(cards.Single(c => c.Text.StartsWith("Deadlocks")).Text, "Deadlocks above baseline for the time: 6 - baseline 3 (+100%)");
        }

        [TestMethod]
        public void BusiestHour_IsOneTheRangeMostlyCovers()
        {
            // 14:50-15:50: the 14:00 hour was at 95% but only 10 minutes of it are in the range - it isn't the busiest hour of the range
            var from = From.AddHours(5).AddMinutes(50);
            var now = from.AddHours(1);
            var w = InsightWindow.For(from, now);
            var data = Data(w, 4, h => Hour(0, h, cpu: h == 0 ? 95 : 20));
            var cards = PerformanceInsightBuilder.Build(new PerformanceInsightsInput { InstanceID = InstanceID, Window = w, Performance = data, NowUtc = now });
            Assert.IsFalse(cards.Any(c => !c.IsSummary && c.Text.StartsWith("CPU")), string.Join(" | ", cards.Select(c => c.Text)));
            StringAssert.Contains(cards.Single(c => c.IsSummary).Text, "each hour weighted by how much of the range it covers");
        }

        private static TimeZoneInfo UK() => TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "GMT Standard Time" : "Europe/London");

        [TestMethod]
        public void PeriodShifts_KeepTheSameLocalHoursAcrossAClockChange()
        {
            // Mon 2 Nov 2026 07:00-09:00 GMT.  The UK clocks went back on Sun 25 Oct, so the week before (26 Oct) is GMT too, but 19 Oct
            // and earlier were BST: 07:00 local then was 06:00 UTC, an hour earlier than exactly 14 days back
            var end = new DateTime(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);
            var shifts = PerformanceData.PeriodShifts(end, 2, 4, UK());
            CollectionAssert.AreEquivalent(new[] { new KeyValuePair<int, int>(2, -1), new KeyValuePair<int, int>(3, -1), new KeyValuePair<int, int>(4, -1) }, shifts.ToList());
            Assert.AreEqual("2:-60,3:-60,4:-60", PerformanceData.PeriodShiftParameter(shifts));
            Assert.AreEqual(0, PerformanceData.PeriodShifts(end, 2, 4, TimeZoneInfo.Utc).Count);
            Assert.IsNull(PerformanceData.PeriodShiftParameter(new Dictionary<int, int>()));
        }

        [TestMethod]
        public void HoursFor_UsesTheShiftedStartOfEachPeriod()
        {
            var end = new DateTime(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc);
            var tz = UK();
            var shifts = PerformanceData.PeriodShifts(end, 2, 2, tz);
            // Keep 07:00 local only: offset 0 in every period once shifted
            var hours = PerformanceData.HoursFor(end, 2, 2, tz, local => local.Hour == 7, shifts);
            CollectionAssert.AreEquivalent(new[] { (0, 0), (1, 0), (2, 0) }, hours.ToList());
        }

        #region Tempdb contention

        /// <summary>Total waits 4x the baseline, nearly all PAGELATCH_EX - a waits card with page latch waits up most</summary>
        private static PerformanceData PageLatchWaits(InsightWindow w)
        {
            var data = Data(w, 4, h => Hour(0, h, waitMs: 3_600_000 * 4));
            var instance = data.Instances.Single();
            instance.WaitTypes.Add(new PerformanceWaitType { Period = 0, WaitType = "PAGELATCH_EX", WaitMs = 3_600_000L * 3 * w.Hours });
            for (var p = 1; p <= 4; p++) instance.WaitTypes.Add(new PerformanceWaitType { Period = p, WaitType = "PAGELATCH_EX", WaitMs = 1000 });
            return data;
        }

        private static InsightTempdbContention Tempdb(int allocationSnapshots = 0, int allocationWaiting = 0, int metadataSnapshots = 0, int metadataWaiting = 0, int total = 100) => new()
        {
            Snapshots = total,
            SnapshotsWithTypes = total,
            AllocationSnapshots = allocationSnapshots,
            MaxAllocationWaiting = allocationWaiting,
            MaxAllocationWaitMs = allocationWaiting * 1500L,
            WorstAllocationSnapshotUtc = allocationSnapshots > 0 ? From.AddHours(2).AddMinutes(5) : null,
            MetadataSnapshots = metadataSnapshots,
            MaxMetadataWaiting = metadataWaiting,
            MaxMetadataWaitMs = metadataWaiting * 1500L,
            WorstMetadataSnapshotUtc = metadataSnapshots > 0 ? From.AddHours(3) : null
        };

        [TestMethod]
        public void PageLatchWaits_SayWhatTheSnapshotsShowOfTempdb_OnTheWaitsCard()
        {
            var w = InsightWindow.For(From, To);
            var cards = Build(w, PageLatchWaits(w), new PerformanceInsightsContext { Tempdb = Tempdb(allocationSnapshots: 12, allocationWaiting: 8) });
            var waits = cards.Single(c => c.Text.StartsWith("Total waits"));
            StringAssert.Contains(waits.Text, "PAGELATCH_EX");
            StringAssert.Contains(waits.Text, "Tempdb contention in the running queries snapshots: allocation (PFS/GAM/SGAM pages) in 12 of 100 snapshots (12%), up to 8 sessions at once (11:05)");
            Assert.IsTrue(waits.Links.Any(l => l.Tab == "RunningQueries"));
            Assert.IsFalse(cards.Any(c => c.Category == InsightCategory.Tempdb), "Explained on the waits card - no card of its own");
        }

        [TestMethod]
        public void PageLatchWaits_WithNoTempdbContention_PointElsewhere()
        {
            var w = InsightWindow.For(From, To);
            var cards = Build(w, PageLatchWaits(w), new PerformanceInsightsContext { Tempdb = Tempdb() });
            StringAssert.Contains(cards.Single(c => c.Text.StartsWith("Total waits")).Text, "more likely on a user database page");
        }

        [TestMethod]
        public void TempdbContention_WithoutAWaitsCard_GetsItsOwnCardWhenSignificant()
        {
            var w = InsightWindow.For(From, To);
            // Below both thresholds: 2 sessions in 1 of 100 snapshots
            Assert.IsFalse(Build(w, Data(w, 4), new PerformanceInsightsContext { Tempdb = Tempdb(allocationSnapshots: 1, allocationWaiting: 2) })
                .Any(c => c.Category == InsightCategory.Tempdb));

            var warning = Build(w, Data(w, 4), new PerformanceInsightsContext { Tempdb = Tempdb(metadataSnapshots: 3, metadataWaiting: 4) })
                .Single(c => c.Category == InsightCategory.Tempdb);
            Assert.AreEqual(InsightLevel.Warning, warning.Level);
            StringAssert.StartsWith(warning.Text, "Tempdb metadata contention: sessions waiting on other tempdb pages in 3 of 100 snapshots (3%), up to 4 sessions at once (12:00), 6s waiting in total.");
            Assert.IsTrue(warning.Links.Any(l => l.ZoomFromUtc == From.AddHours(3).AddMinutes(-10)));

            var critical = Build(w, Data(w, 4), new PerformanceInsightsContext { Tempdb = Tempdb(allocationSnapshots: 30, allocationWaiting: 2) })
                .Single(c => c.Category == InsightCategory.Tempdb);
            Assert.AreEqual(InsightLevel.Critical, critical.Level, "In 30% of snapshots");
        }

        [TestMethod]
        public void TempdbContention_FromAnOlderAgent_IsNotJudged()
        {
            var w = InsightWindow.For(From, To);
            var tempdb = new InsightTempdbContention { Snapshots = 100, SnapshotsWithTypes = 0 };
            var cards = Build(w, PageLatchWaits(w), new PerformanceInsightsContext { Tempdb = tempdb });
            Assert.IsFalse(cards.Any(c => c.Category == InsightCategory.Tempdb));
            Assert.IsFalse(cards.Single(c => c.Text.StartsWith("Total waits")).Text.Contains("running queries snapshots"));
        }

        #endregion

        [TestMethod]
        public void HourlyMBsec_IsBytesOverSampleTime()
        {
            // 3,600 MB read and 1,800 MB written in an hour = 1.5 MB/s
            var h = new PerformanceHour { ReadBytes = 3600L * 1024 * 1024, WriteBytes = 1800L * 1024 * 1024, IOSampleMs = 3_600_000 };
            Assert.AreEqual(1.5, PerformanceAnalyzer.HourlyValue("MBsec", h, 0)!.Value, 0.0001);
            Assert.IsNull(PerformanceAnalyzer.HourlyValue("MBsec", new PerformanceHour { IOSampleMs = 3_600_000 }, 0), "No byte counts - no figure rather than 0");
        }

        [TestMethod]
        public void LatencyContext_IncludesThroughput()
        {
            Assert.AreEqual("at 1,200 IOPs, 45 MB/s (baseline 800 IOPs, 12 MB/s)", PerformanceAnalyzer.IOPsContext(1200, 800, 45, 12));
            Assert.AreEqual("at 1,200 IOPs, 4.5 MB/s", PerformanceAnalyzer.IOPsContext(1200, null, 4.5));
            Assert.AreEqual("at 100 IOPs (baseline 100)", PerformanceAnalyzer.IOPsContext(100, 100), "Without byte counts, as before");
        }
    }
}
