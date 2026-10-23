using System;
using System.IO;
using System.Linq;
using System.Text;

namespace DBADash
{
    /// <summary>
    /// The SQL that reads a running request's live plan and operator counters.  Shared by the messages the DBA Dash service
    /// runs for the GUI (LivePlanMessage, LiveQueryProfileMessage) and the viewers' direct connection - a live plan opened
    /// from SSMS - so both follow a query the same way.  Both take @SessionID, @ExpectedStartUtc and @SnapshotUtcOffset.
    /// </summary>
    public static class LiveQuerySql
    {
        /// <summary>The request's live plan and which statement it is for - one row, LivePlan.</summary>
        public static string LivePlan => Read("SQLLivePlan.sql");

        /// <summary>The request's status and statement, then its operator counters - Request and Profiles.</summary>
        public static string LiveQueryProfile => Read("SQLLiveQueryProfile.sql");

        private static string Read(string fileName)
        {
            var assembly = typeof(LiveQuerySql).Assembly;
            var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith(".SQL." + fileName, StringComparison.Ordinal));
            using var stream = assembly.GetManifestResourceStream(name);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd().TrimStart((char)0xFEFF);
        }
    }
}
