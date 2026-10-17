using DBADash.Messaging;
using DBADashService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace DBADash.Test
{
    /// <summary>
    /// Temporary schedules - e.g. a collection switched on while investigating an issue - and how the GUI reads the
    /// schedule back from the service.  Each test uses its own store file.
    /// </summary>
    [TestClass]
    public class TemporaryScheduleStoreTests
    {
        private string path = null!;

        [TestInitialize]
        public void Init() => path = Path.Combine(Path.GetTempPath(), $"TemporarySchedules_{Guid.NewGuid():N}.json");

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private static TemporarySchedule Temp(string schedule, DateTime until) => new() { Schedule = schedule, UntilUtc = until };

        [TestMethod]
        public void Set_And_Get_UntilTheyEnd()
        {
            var store = new TemporaryScheduleStore(path);
            store.Set("SQL1", new Dictionary<CollectionType, TemporarySchedule>
            {
                { CollectionType.TableSize, Temp("300", DateTime.UtcNow.AddHours(1)) },
                { CollectionType.FailedLogins, Temp("60", DateTime.UtcNow.AddMinutes(-1)) }
            });

            var active = store.Get("sql1")!;
            Assert.AreEqual(1, active.Count); // The ended one is ignored
            Assert.AreEqual("300", active[CollectionType.TableSize].Schedule);
            Assert.AreEqual("300", store.GetOverrides("SQL1")![CollectionType.TableSize].Schedule);
            Assert.IsNull(store.Get("SQL2"));
        }

        [TestMethod]
        public void SurvivesRestart()
        {
            var until = DateTime.UtcNow.AddHours(1);
            new TemporaryScheduleStore(path).Set("SQL1", new Dictionary<CollectionType, TemporarySchedule> { { CollectionType.TableSize, Temp("300", until) } });

            var reloaded = new TemporaryScheduleStore(path).Get("SQL1")!;
            Assert.AreEqual(until, reloaded[CollectionType.TableSize].UntilUtc);
            Assert.AreEqual(DateTimeKind.Utc, reloaded[CollectionType.TableSize].UntilUtc.Kind);
        }

        [TestMethod]
        public void NullEndsATemporarySchedule_AndFileGoesWhenEmpty()
        {
            var store = new TemporaryScheduleStore(path);
            store.Set("SQL1", new Dictionary<CollectionType, TemporarySchedule> { { CollectionType.TableSize, Temp("300", DateTime.UtcNow.AddHours(1)) } });
            Assert.IsTrue(File.Exists(path));

            store.Set("SQL1", new Dictionary<CollectionType, TemporarySchedule> { { CollectionType.TableSize, null! } });

            Assert.IsNull(store.Get("SQL1"));
            Assert.IsFalse(File.Exists(path));
        }

        [TestMethod]
        public void RemoveExpired_ReturnsTheInstancesToReschedule()
        {
            // Written directly, as the service would find it after a restart
            File.WriteAllText(path, JsonConvert.SerializeObject(new Dictionary<string, Dictionary<CollectionType, TemporarySchedule>>
            {
                { "SQL1", new Dictionary<CollectionType, TemporarySchedule> { { CollectionType.TableSize, Temp("300", DateTime.UtcNow.AddMinutes(-1)) } } },
                { "SQL2", new Dictionary<CollectionType, TemporarySchedule> { { CollectionType.TableSize, Temp("300", DateTime.UtcNow.AddHours(1)) } } }
            }));
            var store = new TemporaryScheduleStore(path);

            CollectionAssert.AreEqual(new List<string> { "SQL1" }, store.RemoveExpired());
            Assert.AreEqual(0, store.RemoveExpired().Count); // Already removed
            Assert.IsNotNull(store.Get("SQL2"));
            Assert.IsNull(new TemporaryScheduleStore(path).Get("SQL1")); // And from the file
        }

        [TestMethod]
        public void RemoveExpired_StillReportsAnInstanceAfterAChangeToAnother()
        {
            var store = new TemporaryScheduleStore(path);
            store.Set("SQL1", new Dictionary<CollectionType, TemporarySchedule> { { CollectionType.TableSize, Temp("300", DateTime.UtcNow.AddMinutes(-1)) } });
            store.Set("SQL2", new Dictionary<CollectionType, TemporarySchedule> { { CollectionType.TableSize, Temp("300", DateTime.UtcNow.AddHours(1)) } });

            CollectionAssert.AreEqual(new List<string> { "SQL1" }, store.RemoveExpired());
        }

        [TestMethod]
        public void CorruptFile_MeansNoTemporarySchedules()
        {
            File.WriteAllText(path, "not json");
            Assert.IsNull(new TemporaryScheduleStore(path).Get("SQL1"));
        }

        /// <summary>The DataSet as the GUI receives it: serialized into a reply by the service and read back.</summary>
        private static DataSet ThroughReply(DataSet ds) =>
            new ResponseMessage { DataString = new ResponseMessage { Data = ds }.DataString }.Data;

        [TestMethod]
        public void ScheduleGetMessage_DateTimesAreSentWithoutATimeZone()
        {
            // By default a DataSet writes a DateTime with the sending machine's UTC offset and the receiver converts it to
            // its own time zone - so a service and GUI in different time zones would see a different UTC time.  That
            // can't be reproduced on one machine, so check the columns are sent as they are instead.
            var instances = new[] { new ServiceInstanceInfo { ConnectionID = "SQL1", PausedUntilUtc = DateTime.UtcNow.AddHours(1) } };
            var ds = CollectionScheduleGetMessage.BuildDataSet(null, null,
                new Dictionary<CollectionType, TemporarySchedule> { { CollectionType.TableSize, Temp("300", DateTime.UtcNow.AddHours(1)) } },
                isInConfig: true, instances);

            var dateTimeColumns = ds.Tables.Cast<DataTable>().SelectMany(t => t.Columns.Cast<DataColumn>()).Where(c => c.DataType == typeof(DateTime)).ToList();
            Assert.AreEqual(3, dateTimeColumns.Count); // Schedules.UntilUtc, ServiceInstances.PausedUntilUtc, ServiceInstanceSchedules.UntilUtc
            Assert.IsTrue(dateTimeColumns.TrueForAll(c => c.DateTimeMode == DataSetDateTime.Unspecified));
        }

        [TestMethod]
        public void ScheduleGetMessage_DataSetRoundTrips()
        {
            var service = new CollectionSchedules { { CollectionType.Drives, new CollectionSchedule { Schedule = "600", RunOnServiceStart = true } } };
            var instance = new CollectionSchedules { { CollectionType.Jobs, new CollectionSchedule { Schedule = "", RunOnServiceStart = false } } };
            var until = DateTime.UtcNow.AddHours(1);
            var temporary = new Dictionary<CollectionType, TemporarySchedule> { { CollectionType.TableSize, Temp("300", until) } };

            // Through the reply - a UTC time mustn't come back shifted into the local time zone
            var ds = ThroughReply(CollectionScheduleGetMessage.BuildDataSet(service, instance, temporary, isInConfig: true));

            var readService = CollectionScheduleGetMessage.ReadSchedules(ds, CollectionScheduleGetMessage.LevelService);
            Assert.AreEqual("600", readService[CollectionType.Drives].Schedule);
            Assert.IsTrue(readService[CollectionType.Drives].RunOnServiceStart);
            Assert.AreEqual(string.Empty, CollectionScheduleGetMessage.ReadSchedules(ds, CollectionScheduleGetMessage.LevelInstance)[CollectionType.Jobs].Schedule);
            Assert.AreEqual(until, CollectionScheduleGetMessage.ReadTemporarySchedules(ds)[CollectionType.TableSize].UntilUtc);
            Assert.IsTrue((bool)ds.Tables["Instance"]!.Rows[0]["IsInConfig"]);
        }

        [TestMethod]
        public void ScheduleGetMessage_ServiceInstancesRoundTrip()
        {
            var until = DateTime.UtcNow.AddHours(1);
            var instances = new[]
            {
                new ServiceInstanceInfo
                {
                    ConnectionID = "SQL1",
                    IsInConfig = true,
                    InstanceSchedules = new CollectionSchedules { { CollectionType.Drives, new CollectionSchedule { Schedule = "60" } } }
                },
                new ServiceInstanceInfo
                {
                    ConnectionID = "SQL2|DB1",
                    IsInConfig = false, // Discovered at runtime
                    PausedUntilUtc = until,
                    TemporarySchedules = new Dictionary<CollectionType, TemporarySchedule> { { CollectionType.TableSize, Temp("300", until) } }
                }
            };

            var read = CollectionScheduleGetMessage.ReadServiceInstances(
                ThroughReply(CollectionScheduleGetMessage.BuildDataSet(null, null, null, isInConfig: true, instances)))!;

            Assert.AreEqual(2, read.Count);
            var sql1 = read.Single(i => i.ConnectionID == "SQL1");
            Assert.IsTrue(sql1.IsInConfig);
            Assert.IsNull(sql1.PausedUntilUtc);
            Assert.IsFalse(sql1.FollowsServiceSchedule(CollectionType.Drives)); // Has its own schedule - a service change doesn't affect it
            Assert.IsTrue(sql1.FollowsServiceSchedule(CollectionType.Jobs));

            var sql2 = read.Single(i => i.ConnectionID == "SQL2|DB1");
            Assert.IsFalse(sql2.IsInConfig);
            Assert.AreEqual(until, sql2.PausedUntilUtc);
            Assert.AreEqual(until, sql2.TemporarySchedules[CollectionType.TableSize].UntilUtc);
            Assert.IsTrue(sql2.FollowsServiceSchedule(CollectionType.TableSize)); // A temporary schedule only delays a service change
        }

        [TestMethod]
        public void ScheduleGetMessage_OlderServiceSendsNoInstances()
        {
            Assert.IsNull(CollectionScheduleGetMessage.ReadServiceInstances(
                CollectionScheduleGetMessage.BuildDataSet(null, null, null, isInConfig: true)));
        }
    }
}
