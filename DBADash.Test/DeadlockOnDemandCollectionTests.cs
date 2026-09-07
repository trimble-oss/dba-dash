using System.Collections.Generic;
using DBADash.Messaging;
using DBADashService;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    /// <summary>
    /// What a triggered collection does when the collection it was asked for is switched off for the
    /// instance.
    ///
    /// <para>A scheduled run never reaches this: the scheduler only runs what is enabled.  A triggered one
    /// can be asked for anything, and the deadlock reports trigger it precisely because the instance has no
    /// data - so the answer for deadlocks is system_health, which is already capturing on every supported
    /// on-premises instance and holds what it has deadlocked on recently.  Nothing else has a stand-in for
    /// what it reads, so nothing else is forced.</para>
    ///
    /// <para>Everything here is pure - <see cref="CollectionMessage.SkipDisabledCollections"/> decides from
    /// the config alone - so none of it needs an instance to run against.</para>
    /// </summary>
    [TestClass]
    public class DeadlockOnDemandCollectionTests
    {
        private const string SqlConnection = "Data Source=SQL1;Integrated Security=SSPI";

        private static DBADashSource Source(string deadlockSessionName, int slowQueryThresholdMs = 1000) =>
            new()
            {
                SourceConnection = new DBADashConnection(SqlConnection),
                DeadlockXESessionName = deadlockSessionName,
                SlowQueryThresholdMs = slowQueryThresholdMs
            };

        /// <summary>Runs the skip against a fresh message, returning what it left to run and the fallback session.</summary>
        private static (List<CollectionType> ToRun, string OnDemandSession) Skip(DBADashSource source,
            bool force, params CollectionType[] requested)
        {
            var message = new CollectionMessage(requested, "SQL1");
            var toRun = new List<CollectionType>(requested);
            var session = message.SkipDisabledCollections(new CollectionConfig(), source, toRun,
                new Dictionary<string, CustomCollection>(), "SQL1", force);
            return (toRun, session);
        }

        [TestMethod]
        public void DisabledDeadlocks_AreSkipped_WhenNotForced()
        {
            // The ordinary trigger: the user has not confirmed anything, so nothing is collected and the
            // warning says which collection was skipped and that configuration - not a schedule - skipped it.
            var ex = Assert.ThrowsExactly<CollectionScheduleDisabledException>(() =>
                Skip(Source(string.Empty), force: false, CollectionType.Deadlocks));

            CollectionAssert.AreEqual(new[] { "Deadlocks" }, ex.DisabledCollections);
            CollectionAssert.AreEqual(new[] { "Deadlocks" }, ex.ConfigurationDisabledCollections,
                "Configuration switched it off, so the GUI must offer system_health rather than a plain re-run.");
        }

        [TestMethod]
        public void DisabledDeadlocks_ReadSystemHealth_WhenForced()
        {
            // The user has confirmed.  Deadlocks stays in the run and the collector is pointed at
            // system_health, which is what makes the run worth doing - it already holds the history.
            var (toRun, session) = Skip(Source(string.Empty), force: true, CollectionType.Deadlocks);

            CollectionAssert.Contains(toRun, CollectionType.Deadlocks);
            Assert.AreEqual(DBADashSource.SystemHealthXESessionName, session);
        }

        [TestMethod]
        public void ConfiguredDeadlocks_NeedNoFallback()
        {
            // The collection is enabled, so the configured session is read whether or not the run was forced -
            // a force must not quietly redirect an instance that is already collecting to a different session.
            foreach (var force in new[] { false, true })
            {
                var (toRun, session) = Skip(Source(DBADashSource.ManagedDeadlockXESessionName), force,
                    CollectionType.Deadlocks);

                CollectionAssert.Contains(toRun, CollectionType.Deadlocks);
                Assert.IsNull(session, $"force={force}");
            }
        }

        [TestMethod]
        public void DisabledSlowQueries_StaySkipped_WhenForced()
        {
            // A negative threshold leaves the slow query session nothing to capture against, and there is no
            // second session holding slow queries the way system_health holds deadlocks.  Forcing it would
            // collect nothing and report success, so it is skipped and reported as configuration disabled.
            var ex = Assert.ThrowsExactly<CollectionScheduleDisabledException>(() =>
                Skip(Source(string.Empty, slowQueryThresholdMs: -1), force: true, CollectionType.SlowQueries));

            CollectionAssert.AreEqual(new[] { "SlowQueries" }, ex.ConfigurationDisabledCollections);
        }

        [TestMethod]
        public void ForcedDeadlocks_RunAlongsideACollectionThatCannotBeForced()
        {
            // Mixed request: deadlocks can be satisfied from system_health, slow queries cannot be satisfied
            // at all.  The run goes ahead with what it can collect rather than failing on what it cannot.
            var (toRun, session) = Skip(Source(string.Empty, slowQueryThresholdMs: -1), force: true,
                CollectionType.Deadlocks, CollectionType.SlowQueries);

            CollectionAssert.Contains(toRun, CollectionType.Deadlocks);
            CollectionAssert.DoesNotContain(toRun, CollectionType.SlowQueries);
            Assert.AreEqual(DBADashSource.SystemHealthXESessionName, session);
        }

        [TestMethod]
        public void UnscheduledCollection_IsStillForcedOnItsOwnSchedule()
        {
            // The pre-existing case, unchanged by the deadlock fallback: a collection with an empty cron
            // expression runs on a force, and nothing is reported as configuration disabled.
            var source = Source(string.Empty);
            source.CollectionSchedules = new CollectionSchedules
            {
                { CollectionType.Drives, new CollectionSchedule { Schedule = string.Empty } }
            };

            var ex = Assert.ThrowsExactly<CollectionScheduleDisabledException>(() =>
                Skip(source, force: false, CollectionType.Drives));
            CollectionAssert.AreEqual(new[] { "Drives" }, ex.DisabledCollections);
            Assert.AreEqual(0, ex.ConfigurationDisabledCollections.Count,
                "An empty schedule is not configuration switching the collection off - a re-run collects it.");

            var (toRun, session) = Skip(source, force: true, CollectionType.Drives);
            CollectionAssert.Contains(toRun, CollectionType.Drives);
            Assert.IsNull(session);
        }
    }
}
