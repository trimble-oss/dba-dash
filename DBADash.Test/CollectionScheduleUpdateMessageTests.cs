using DBADash.Messaging;
using DBADashService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using static DBADash.Messaging.CollectionScheduleUpdateMessage;

namespace DBADash.Test
{
    /// <summary>
    /// How a schedule change requested from the GUI is validated and applied to the service config.  Applying it is
    /// pure, so none of this needs a service or an instance to run against.
    /// </summary>
    [TestClass]
    public class CollectionScheduleUpdateMessageTests
    {
        private const string ConnectionID = "SQL1";

        private static CollectionConfig Config(CollectionSchedules? instanceSchedules = null, CollectionSchedules? serviceSchedules = null) =>
            new()
            {
                CollectionSchedules = serviceSchedules,
                SourceConnections = new List<DBADashSource>
                {
                    new()
                    {
                        SourceConnection = new DBADashConnection("Data Source=SQL1;Integrated Security=SSPI"),
                        ConnectionID = ConnectionID,
                        CollectionSchedules = instanceSchedules
                    }
                }
            };

        private static CollectionScheduleUpdateMessage Message(ScheduleScopes scope, CollectionType type, CollectionSchedule? schedule) =>
            new()
            {
                Scope = scope,
                ConnectionID = scope == ScheduleScopes.Instance ? ConnectionID : null,
                Changes = new Dictionary<CollectionType, CollectionSchedule> { { type, schedule! } }
            };

        [TestMethod]
        [DataRow("")]
        [DataRow("60")]
        [DataRow("0 0/5 * * * ?")]
        [DataRow(" 0 0 0 * * ? ")]
        public void ValidSchedules(string schedule)
        {
            Assert.IsTrue(IsValidSchedule(schedule));
        }

        [TestMethod]
        [DataRow("0")]
        [DataRow("-10")]
        [DataRow("every minute")]
        [DataRow("0 0 25 * * ?")]
        [DataRow("0 0 0 30 2 ?")] // Valid syntax, but 30 February never comes - Quartz won't schedule it
        [DataRow("0 0 0 1 1 ? 2020")] // A year that has passed
        public void InvalidSchedules(string schedule)
        {
            Assert.IsFalse(IsValidSchedule(schedule));
        }

        [TestMethod]
        public void ScheduleInfo_IsNotConfigurable()
        {
            // Internal bookkeeping - it reports the schedule itself
            Assert.IsFalse(IsConfigurable(CollectionType.ScheduleInfo));
            Assert.IsTrue(IsConfigurable(CollectionType.Jobs));
        }

        [TestMethod]
        public void Validate_RejectsBadRequests()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new CollectionScheduleUpdateMessage { Scope = ScheduleScopes.Service }.Validate(DateTime.UtcNow));
            Assert.ThrowsExactly<ArgumentException>(() => Message(ScheduleScopes.Service, CollectionType.Jobs, new CollectionSchedule { Schedule = "nonsense" }).Validate(DateTime.UtcNow));
            Assert.ThrowsExactly<ArgumentException>(() => Message(ScheduleScopes.Service, CollectionType.ScheduleInfo, new CollectionSchedule { Schedule = "60" }).Validate(DateTime.UtcNow));

            var noConnection = Message(ScheduleScopes.Instance, CollectionType.Jobs, null);
            noConnection.ConnectionID = null;
            Assert.ThrowsExactly<ArgumentException>(() => noConnection.Validate(DateTime.UtcNow));

            Message(ScheduleScopes.Instance, CollectionType.Jobs, null).Validate(DateTime.UtcNow); // Inherit is valid
        }

        [TestMethod]
        public void Instance_AddsOverride_WithoutTouchingService()
        {
            var config = Config();
            Message(ScheduleScopes.Instance, CollectionType.Jobs, new CollectionSchedule { Schedule = " 0 0/5 * * * ? ", RunOnServiceStart = false }).ApplyTo(config);

            var schedule = config.SourceConnections[0].CollectionSchedules[CollectionType.Jobs];
            Assert.AreEqual("0 0/5 * * * ?", schedule.Schedule);
            Assert.IsFalse(schedule.RunOnServiceStart);
            Assert.IsNull(config.CollectionSchedules);
        }

        [TestMethod]
        public void Instance_Disable_IsAnEmptySchedule()
        {
            var config = Config();
            Message(ScheduleScopes.Instance, CollectionType.TableSize, new CollectionSchedule { Schedule = string.Empty }).ApplyTo(config);

            Assert.AreEqual(string.Empty, config.SourceConnections[0].CollectionSchedules[CollectionType.TableSize].Schedule);
        }

        [TestMethod]
        public void Instance_Inherit_RemovesOverride_AndClearsAnEmptyList()
        {
            var config = Config(instanceSchedules: new CollectionSchedules { { CollectionType.Jobs, new CollectionSchedule { Schedule = "60" } } });
            Message(ScheduleScopes.Instance, CollectionType.Jobs, null).ApplyTo(config);

            // Null rather than empty so the config file doesn't keep an empty section
            Assert.IsNull(config.SourceConnections[0].CollectionSchedules);
        }

        [TestMethod]
        public void Instance_KeepsOtherOverrides_AndReplacesTheCollection()
        {
            var original = new CollectionSchedules
            {
                { CollectionType.Jobs, new CollectionSchedule { Schedule = "60" } },
                { CollectionType.Drives, new CollectionSchedule { Schedule = "120" } }
            };
            var config = Config(instanceSchedules: original);
            Message(ScheduleScopes.Instance, CollectionType.Jobs, null).ApplyTo(config);

            var schedules = config.SourceConnections[0].CollectionSchedules;
            Assert.IsFalse(schedules.ContainsKey(CollectionType.Jobs));
            Assert.AreEqual("120", schedules[CollectionType.Drives].Schedule);
            // Replaced rather than modified in place - the running service reads it from other threads
            Assert.AreNotSame(original, schedules);
            Assert.AreEqual(2, original.Count);
        }

        [TestMethod]
        public void Instance_NotInConfig_Throws()
        {
            var message = Message(ScheduleScopes.Instance, CollectionType.Jobs, null);
            message.ConnectionID = "SQL2|AzureDB";
            Assert.ThrowsExactly<ArgumentException>(() => message.ApplyTo(Config()));
        }

        [TestMethod]
        public void Instance_MatchesConnectionIDIgnoringCase()
        {
            var config = Config();
            var message = Message(ScheduleScopes.Instance, CollectionType.Jobs, new CollectionSchedule { Schedule = "60" });
            message.ConnectionID = "sql1";
            message.ApplyTo(config);

            Assert.AreEqual("60", config.SourceConnections[0].CollectionSchedules[CollectionType.Jobs].Schedule);
        }

        [TestMethod]
        public void Service_ChangesServiceSchedule_WithoutTouchingInstances()
        {
            var config = Config();
            Message(ScheduleScopes.Service, CollectionType.Drives, new CollectionSchedule { Schedule = "0 0 0/2 * * ?", RunOnServiceStart = true }).ApplyTo(config);

            Assert.AreEqual("0 0 0/2 * * ?", config.CollectionSchedules[CollectionType.Drives].Schedule);
            Assert.IsNull(config.SourceConnections[0].CollectionSchedules);
            // The effective schedule picks up the change; everything else stays at the default
            Assert.AreEqual("0 0 0/2 * * ?", config.GetSchedules()[CollectionType.Drives].Schedule);
            Assert.AreEqual(CollectionSchedules.DefaultSchedules[CollectionType.Jobs].Schedule, config.GetSchedules()[CollectionType.Jobs].Schedule);
        }

        [TestMethod]
        public void Service_Inherit_RevertsToDefault()
        {
            var config = Config(serviceSchedules: new CollectionSchedules { { CollectionType.Drives, new CollectionSchedule { Schedule = "60" } } });
            Message(ScheduleScopes.Service, CollectionType.Drives, null).ApplyTo(config);

            Assert.IsNull(config.CollectionSchedules);
            Assert.AreEqual(CollectionSchedules.DefaultSchedules[CollectionType.Drives].Schedule, config.GetSchedules()[CollectionType.Drives].Schedule);
        }

        [TestMethod]
        public void Service_DoesNotModifyTheDefaults()
        {
            var defaultSchedule = CollectionSchedules.DefaultSchedules[CollectionType.Drives].Schedule;
            Message(ScheduleScopes.Service, CollectionType.Drives, new CollectionSchedule { Schedule = "60" }).ApplyTo(Config());

            Assert.AreEqual(defaultSchedule, CollectionSchedules.DefaultSchedules[CollectionType.Drives].Schedule);
        }

        [TestMethod]
        public void Message_RoundTripsThroughSerialization()
        {
            var message = Message(ScheduleScopes.Instance, CollectionType.Jobs, new CollectionSchedule { Schedule = "60", RunOnServiceStart = false });
            message.Changes.Add(CollectionType.Drives, null!);

            var copy = (CollectionScheduleUpdateMessage)MessageBase.Deserialize(message.Serialize());

            Assert.AreEqual(ScheduleScopes.Instance, copy.Scope);
            Assert.AreEqual(ConnectionID, copy.ConnectionID);
            Assert.AreEqual("60", copy.Changes[CollectionType.Jobs].Schedule);
            Assert.IsFalse(copy.Changes[CollectionType.Jobs].RunOnServiceStart);
            Assert.IsNull(copy.Changes[CollectionType.Drives]);
        }

        // ---- temporary schedules ------------------------------------------------------------------

        [TestMethod]
        public void Temporary_Validate()
        {
            var now = DateTime.UtcNow;
            var enable = Message(ScheduleScopes.Instance, CollectionType.TableSize, new CollectionSchedule { Schedule = "300" });
            enable.Temporary = true;

            Assert.ThrowsExactly<ArgumentException>(() => enable.Validate(now)); // No end time
            enable.TemporaryUntilUtc = now.AddMinutes(-1);
            Assert.ThrowsExactly<ArgumentException>(() => enable.Validate(now)); // In the past
            enable.TemporaryUntilUtc = now.Add(MaxTemporary).AddMinutes(1);
            Assert.ThrowsExactly<ArgumentException>(() => enable.Validate(now)); // Too long
            enable.TemporaryUntilUtc = now.AddHours(1);
            enable.Validate(now);

            var service = Message(ScheduleScopes.Service, CollectionType.TableSize, new CollectionSchedule { Schedule = "300" });
            service.Temporary = true;
            service.TemporaryUntilUtc = now.AddHours(1);
            Assert.ThrowsExactly<ArgumentException>(() => service.Validate(now)); // Instance only

            var end = Message(ScheduleScopes.Instance, CollectionType.TableSize, null);
            end.Temporary = true;
            end.Validate(now); // Ending a temporary schedule needs no end time
        }

        [TestMethod]
        public void Temporary_Changes_CarryTheEndTime()
        {
            var until = DateTime.UtcNow.AddHours(1);
            var message = Message(ScheduleScopes.Instance, CollectionType.TableSize, new CollectionSchedule { Schedule = "300", RunOnServiceStart = true });
            message.Changes.Add(CollectionType.Drives, null!);
            message.Temporary = true;
            message.TemporaryUntilUtc = until;

            var changes = message.GetTemporaryChanges();

            Assert.AreEqual("300", changes[CollectionType.TableSize]!.Schedule);
            Assert.IsTrue(changes[CollectionType.TableSize]!.RunOnServiceStart);
            Assert.AreEqual(until, changes[CollectionType.TableSize]!.UntilUtc);
            Assert.IsNull(changes[CollectionType.Drives]);
        }

        // ---- changes from the schedule dialog -----------------------------------------------------

        private static CollectionSchedules Schedules(params (CollectionType Type, string Schedule)[] items)
        {
            var schedules = new CollectionSchedules();
            foreach (var (type, schedule) in items)
            {
                schedules[type] = new CollectionSchedule { Schedule = schedule, RunOnServiceStart = false };
            }
            return schedules;
        }

        [TestMethod]
        public void GetChanges_Permanent_AddedChangedAndRemoved()
        {
            var before = Schedules((CollectionType.Jobs, "60"), (CollectionType.Drives, "120"), (CollectionType.CPU, "30"));
            var after = Schedules((CollectionType.Jobs, "60"), (CollectionType.Drives, "600"), (CollectionType.TableSize, "300"));

            var changes = GetChanges(CollectionSchedules.DefaultSchedules, before, after, temporary: false);

            Assert.AreEqual(3, changes.Count);
            Assert.IsFalse(changes.ContainsKey(CollectionType.Jobs)); // Unchanged
            Assert.AreEqual("600", changes[CollectionType.Drives]!.Schedule);
            Assert.AreEqual("300", changes[CollectionType.TableSize]!.Schedule);
            Assert.IsNull(changes[CollectionType.CPU]); // Removed - inherits again
        }

        [TestMethod]
        public void GetChanges_Temporary_EffectiveDifferencesOnly()
        {
            var baseSchedule = CollectionSchedules.DefaultSchedules;
            var before = Schedules((CollectionType.Drives, "120"));
            // Enable TableSize (disabled by default) and put Drives back to its default
            var after = Schedules((CollectionType.TableSize, "300"));

            var changes = GetChanges(baseSchedule, before, after, temporary: true);

            Assert.AreEqual(2, changes.Count);
            Assert.AreEqual("300", changes[CollectionType.TableSize]!.Schedule);
            // Temporarily back to the default schedule rather than removed - a temporary schedule can only set one
            Assert.AreEqual(baseSchedule[CollectionType.Drives].Schedule, changes[CollectionType.Drives]!.Schedule);
        }

        [TestMethod]
        public void GetChanges_NothingChanged()
        {
            var before = Schedules((CollectionType.Jobs, "60"));
            Assert.AreEqual(0, GetChanges(CollectionSchedules.DefaultSchedules, before, Schedules((CollectionType.Jobs, " 60 ")), temporary: false).Count);
            Assert.AreEqual(0, GetChanges(CollectionSchedules.DefaultSchedules, before, Schedules((CollectionType.Jobs, "60")), temporary: true).Count);
        }

        // ---- sources the service discovered from an instance ---------------------------------------

        private static DBADashSource Source(string connectionID) =>
            new() { SourceConnection = new DBADashConnection("Data Source=SQL1;Integrated Security=SSPI"), ConnectionID = connectionID };

        [TestMethod]
        public void GetDerivedSources_AzureDatabasesAndReplicasFoundFromMaster()
        {
            var sources = new[]
            {
                Source("azsrv|master"),
                Source("azsrv|db1"), // Discovered through master
                Source("azsrv|db1|ReadOnly"), // Its read replica
                Source("azsrv|db2"), // In the config - has its own schedule
                Source("azsrv|db2|ReadOnly"), // Its read replica - derived from db2, not master
                Source("other|db3") // A different server
            };
            var configured = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { "azsrv|master", "AZSRV|DB2" };

            var derived = CollectionScheduleUpdateMessage.GetDerivedSources(sources, "AZSRV|master", configured).Select(s => s.ConnectionID).ToList();

            CollectionAssert.AreEquivalent(new[] { "azsrv|db1", "azsrv|db1|ReadOnly" }, derived);
        }

        [TestMethod]
        public void GetDerivedSources_ReadReplicaOfAConfiguredAzureDatabase()
        {
            var sources = new[] { Source("azsrv|master"), Source("azsrv|db2"), Source("azsrv|db2|ReadOnly") };
            var configured = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { "azsrv|master", "azsrv|db2" };

            var derived = CollectionScheduleUpdateMessage.GetDerivedSources(sources, "azsrv|db2", configured).Select(s => s.ConnectionID).ToList();

            CollectionAssert.AreEqual(new[] { "azsrv|db2|ReadOnly" }, derived);
        }

        [TestMethod]
        public void GetDerivedSources_ReadReplicaOfAnInstance()
        {
            var sources = new[] { Source("SQL1"), Source("SQL1|ReadOnly"), Source("SQL10") };
            var configured = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase) { "SQL1", "SQL10" };

            var derived = CollectionScheduleUpdateMessage.GetDerivedSources(sources, "SQL1", configured).Select(s => s.ConnectionID).ToList();

            CollectionAssert.AreEqual(new[] { "SQL1|ReadOnly" }, derived);
        }

        [TestMethod]
        public void IsAffectedByServiceChange_OnlyWhereTheServiceScheduleIsFollowed()
        {
            var message = Message(ScheduleScopes.Service, CollectionType.Jobs, new CollectionSchedule { Schedule = "60" });
            message.Changes.Add(CollectionType.Drives, new CollectionSchedule { Schedule = "120" });

            var ownSchedules = Source("SQL1");
            ownSchedules.CollectionSchedules = Schedules((CollectionType.Jobs, "30"), (CollectionType.Drives, "30"));
            var ownJobsSchedule = Source("SQL2");
            ownJobsSchedule.CollectionSchedules = Schedules((CollectionType.Jobs, "30"));

            Assert.IsFalse(message.IsAffectedByServiceChange(ownSchedules)); // Its own schedule for every changed collection
            Assert.IsTrue(message.IsAffectedByServiceChange(ownJobsSchedule)); // Follows the service schedule for Drives
            Assert.IsTrue(message.IsAffectedByServiceChange(Source("SQL3"))); // No schedules of its own
        }

        // ---- warnings returned with a reply --------------------------------------------------------

        [TestMethod]
        public void MessageWarnings_RoundTripThroughAReply()
        {
            var warning = MessageWarnings.ForScheduleInfoFailures(new[] { "SQL1: Login failed" }, "alerts won't be muted");
            var reply = new ResponseMessage { Data = MessageWarnings.ToDataSet(new[] { warning! }) };

            var received = new ResponseMessage { DataString = reply.DataString };

            var warnings = MessageWarnings.Read(received.Data);
            Assert.AreEqual(1, warnings.Count);
            StringAssert.Contains(warnings[0], "SQL1: Login failed");
            StringAssert.Contains(warnings[0], "alerts won't be muted");
            Assert.IsNull(MessageWarnings.ForScheduleInfoFailures(new List<string>(), "anything"));
            Assert.IsNull(MessageWarnings.ToDataSet(new List<string>()));
            Assert.AreEqual(0, MessageWarnings.Read(null!).Count);
        }
    }
}
