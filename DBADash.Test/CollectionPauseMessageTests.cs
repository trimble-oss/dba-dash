using DBADash.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace DBADash.Test
{
    /// <summary>
    /// Pausing collections from the GUI: validating the request, applying it to the service config and checking whether an
    /// instance is paused.  All pure - no service or instance needed.
    /// </summary>
    [TestClass]
    public class CollectionPauseMessageTests
    {
        private static readonly DateTime Now = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);

        private static CollectionPauseMessage Pause(DateTime? until, params string[] connectionIDs) =>
            new() { ConnectionIDs = new List<string>(connectionIDs), PauseUntilUtc = until };

        [TestMethod]
        public void Validate_RejectsBadRequests()
        {
            Assert.ThrowsExactly<ArgumentException>(() => Pause(Now.AddHours(1)).Validate(Now)); // No instances
            Assert.ThrowsExactly<ArgumentException>(() => Pause(Now.AddMinutes(-1), "SQL1").Validate(Now)); // In the past
            Assert.ThrowsExactly<ArgumentException>(() => Pause(Now.Add(CollectionPauseMessage.MaxPause).AddMinutes(1), "SQL1").Validate(Now));

            Pause(Now.AddHours(1), "SQL1").Validate(Now);
            Pause(null, "SQL1").Validate(Now); // Resume
        }

        [TestMethod]
        public void Apply_PausesAndKeepsOtherPauses()
        {
            var current = new Dictionary<string, DateTime> { { "SQL2", Now.AddHours(2) } };
            var updated = Pause(Now.AddHours(1), "SQL1").Apply(current, Now);

            Assert.AreEqual(Now.AddHours(1), updated!["SQL1"]);
            Assert.AreEqual(Now.AddHours(2), updated["SQL2"]);
            Assert.AreNotSame(current, updated); // Replaced, not modified - read from other threads
            Assert.AreEqual(1, current.Count);
        }

        [TestMethod]
        public void Apply_Resume_RemovesPause_AndNullWhenNothingLeft()
        {
            var current = new Dictionary<string, DateTime> { { "SQL1", Now.AddHours(2) } };

            Assert.IsNull(Pause(null, "sql1").Apply(current, Now)); // ConnectionID matched ignoring case
        }

        [TestMethod]
        public void Apply_DropsExpiredPauses()
        {
            var current = new Dictionary<string, DateTime> { { "SQL2", Now.AddMinutes(-5) } };
            var updated = Pause(Now.AddHours(1), "SQL1").Apply(current, Now);

            Assert.IsFalse(updated!.ContainsKey("SQL2"));
        }

        private static string TempStorePath() => Path.Combine(Path.GetTempPath(), $"CollectionPauses_{Guid.NewGuid():N}.json");

        [TestMethod]
        public void Store_IsPaused_UntilThePauseEnds()
        {
            var path = TempStorePath();
            try
            {
                var store = new CollectionPauseStore(path);
                store.Update(_ => new Dictionary<string, DateTime>
                {
                    { "SQL1", DateTime.UtcNow.AddHours(1) },
                    { "SQL2", DateTime.UtcNow.AddMinutes(-1) }
                });

                Assert.IsTrue(store.IsPaused("sql1", out var until));
                Assert.IsTrue(until > DateTime.UtcNow);
                Assert.IsFalse(store.IsPaused("SQL2", out _)); // Expired - resumes without being cleared
                Assert.IsFalse(store.IsPaused("SQL3", out _));
                Assert.IsFalse(store.IsPaused(null!, out _));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void Store_SurvivesRestart_AsUtc()
        {
            var path = TempStorePath();
            try
            {
                var until = DateTime.UtcNow.AddHours(1);
                new CollectionPauseStore(path).Update(_ => new Dictionary<string, DateTime> { { "SQL1", until } });

                // A new store reads the file, as the service does after a restart
                Assert.IsTrue(new CollectionPauseStore(path).IsPaused("SQL1", out var reloaded));
                Assert.AreEqual(until, reloaded);
                Assert.AreEqual(DateTimeKind.Utc, reloaded.Kind);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void Store_DeletesFileWhenNothingPaused()
        {
            var path = TempStorePath();
            var store = new CollectionPauseStore(path);
            store.Update(_ => new Dictionary<string, DateTime> { { "SQL1", DateTime.UtcNow.AddHours(1) } });
            Assert.IsTrue(File.Exists(path));

            store.Update(current => Pause(null, "SQL1").Apply(current, DateTime.UtcNow));

            Assert.IsFalse(File.Exists(path));
            Assert.IsFalse(store.IsPaused("SQL1", out _));
        }

        [TestMethod]
        public void Store_CorruptFile_MeansNothingPaused()
        {
            var path = TempStorePath();
            try
            {
                File.WriteAllText(path, "not json");
                Assert.IsFalse(new CollectionPauseStore(path).IsPaused("SQL1", out _));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void Message_RoundTripsThroughSerialization()
        {
            var message = Pause(Now.AddHours(1), "SQL1", "SQL2");
            var copy = (CollectionPauseMessage)MessageBase.Deserialize(message.Serialize());

            CollectionAssert.AreEqual(message.ConnectionIDs, copy.ConnectionIDs);
            Assert.AreEqual(Now.AddHours(1), copy.PauseUntilUtc!.Value.ToUniversalTime());
        }
    }
}
