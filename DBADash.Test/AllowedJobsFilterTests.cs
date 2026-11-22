using DBADash.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace DBADash.Test
{
    /// <summary>CollectionConfig.AllowedJobs - which jobs the service will start or stop, for SQL Agent jobs and elastic jobs</summary>
    [TestClass]
    public class AllowedJobsFilterTests
    {
        private static readonly string[] Job = { "Maintenance", "Backup - Full", "AAAAAAAA-0000-0000-0000-000000000001" };

        [TestMethod]
        [DataRow(null, false)]
        [DataRow("", false)]
        [DataRow("*", true)]
        [DataRow("%", true)]
        [DataRow("Backup - Full", true)]
        [DataRow("backup - full", true)] // Case insensitive
        [DataRow("Backup", false)]
        [DataRow("Backup*", true)]
        [DataRow("Backup%", true)]
        [DataRow("%Full", true)]
        [DataRow("Back?p*", false)] // ? isn't a wildcard
        [DataRow("Maintenance", true)] // Category
        [DataRow("aaaaaaaa-0000-0000-0000-000000000001", true)] // ID
        [DataRow("Other, Backup - Full", true)]
        [DataRow("Other", false)]
        [DataRow("*,-Backup*", false)] // Deny wins
        [DataRow("Backup - Full,-Maintenance", false)]
        [DataRow("-Other", false)] // Denies only - nothing allowed
        [DataRow("*,-Other", true)]
        [DataRow("Backup (Full)", false)] // Regex characters are literal
        public void IsAllowed(string? allowedJobs, bool expected)
        {
            Assert.AreEqual(expected, AllowedJobsFilter.IsAllowed(allowedJobs!, Job));
        }

        [TestMethod]
        public void RegexCharactersAreLiteral()
        {
            Assert.IsTrue(AllowedJobsFilter.IsAllowed("Backup (Full)*", new[] { "Backup (Full) - nightly" }));
            Assert.IsFalse(AllowedJobsFilter.IsAllowed("Backup.Full", new[] { "BackupXFull" }));
        }

        [TestMethod]
        public void NullValuesAreIgnored()
        {
            // An elastic job has no category, a SQL Agent job's can be null
            Assert.IsTrue(AllowedJobsFilter.IsAllowed("Job*", new[] { null!, "Job1" }));
            Assert.IsFalse(AllowedJobsFilter.IsAllowed("Other*", new[] { null!, "Job1" }));
        }

        [TestMethod]
        public void ThrowIfNotAllowed_SaysWhetherDeniedOrNotAllowed()
        {
            var denied = Assert.ThrowsExactly<Exception>(() => AllowedJobsFilter.ThrowIfNotAllowed("*,-Backup*", Job, "Backup - Full"));
            Assert.AreEqual("Job execution is denied for Backup - Full", denied.Message);

            var notAllowed = Assert.ThrowsExactly<Exception>(() => AllowedJobsFilter.ThrowIfNotAllowed("Other", Job, "Backup - Full"));
            Assert.AreEqual("Job execution is not allowed for Backup - Full", notAllowed.Message);

            AllowedJobsFilter.ThrowIfNotAllowed("Backup*", Job, "Backup - Full");
        }
    }
}
