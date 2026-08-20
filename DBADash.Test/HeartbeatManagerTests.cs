using System;
using System.Threading;
using DBADash.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    [TestClass]
    public class HeartbeatManagerTests
    {
        // Unmonitored messages never expire and a beat for one is a no-op ---------------------------

        [TestMethod]
        public void Unmonitored_message_never_expires()
        {
            var id = Guid.NewGuid();
            Assert.IsFalse(HeartbeatManager.IsExpired(id, TimeSpan.Zero));
        }

        [TestMethod]
        public void Beat_for_unmonitored_message_does_not_start_monitoring()
        {
            var id = Guid.NewGuid();
            HeartbeatManager.Beat(id); // no Register first
            // Still unmonitored, so still never expires (a stray beat must not create an entry that can then go stale).
            Assert.IsFalse(HeartbeatManager.IsExpired(id, TimeSpan.Zero));
        }

        // A freshly registered message isn't immediately stale --------------------------------------

        [TestMethod]
        public void Registered_message_is_not_immediately_expired()
        {
            var id = Guid.NewGuid();
            HeartbeatManager.Register(id);
            try
            {
                Assert.IsFalse(HeartbeatManager.IsExpired(id, TimeSpan.FromSeconds(30)));
            }
            finally
            {
                HeartbeatManager.Remove(id);
            }
        }

        // With a zero timeout the last beat is always "too old" once time has passed ----------------

        [TestMethod]
        public void Registered_message_expires_when_beats_stop()
        {
            var id = Guid.NewGuid();
            HeartbeatManager.Register(id);
            try
            {
                Thread.Sleep(20);
                Assert.IsTrue(HeartbeatManager.IsExpired(id, TimeSpan.FromMilliseconds(1)));
            }
            finally
            {
                HeartbeatManager.Remove(id);
            }
        }

        [TestMethod]
        public void Beat_resets_the_expiry_window()
        {
            var id = Guid.NewGuid();
            HeartbeatManager.Register(id);
            try
            {
                Thread.Sleep(20);
                HeartbeatManager.Beat(id); // fresh beat
                Assert.IsFalse(HeartbeatManager.IsExpired(id, TimeSpan.FromSeconds(30)));
            }
            finally
            {
                HeartbeatManager.Remove(id);
            }
        }

        // Removal stops monitoring (so a completed trace's id can't be considered stale) -------------

        [TestMethod]
        public void Removed_message_no_longer_expires()
        {
            var id = Guid.NewGuid();
            HeartbeatManager.Register(id);
            HeartbeatManager.Remove(id);
            Thread.Sleep(5);
            Assert.IsFalse(HeartbeatManager.IsExpired(id, TimeSpan.FromMilliseconds(1)));
        }
    }
}
