using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DBADash.XE
{
    /// <summary>
    /// Shared helpers for locating and reading the live targets of an <b>existing</b> XE session.  Used by both the
    /// non-destructive live watch (<c>XEWatchSessionMessage</c>) and the one-shot target-data view
    /// (<c>XEViewTargetDataMessage</c>) so the target-resolution logic lives in one place.
    /// </summary>
    public static class XESessionTargetResolver
    {
        /// <summary>
        /// Reads the live targets of the running session, keyed by <c>target_name</c> -&gt; <c>target_data</c> XML.
        /// Only a running session has live targets (they come from the <c>dm_xe_*</c> views).
        /// </summary>
        public static async Task<Dictionary<string, string>> GetSessionTargetsAsync(string connectionString,
            bool databaseScoped, string sessionName, CancellationToken ct)
        {
            var sessions = databaseScoped ? "sys.dm_xe_database_sessions" : "sys.dm_xe_sessions";
            var targetsView = databaseScoped ? "sys.dm_xe_database_session_targets" : "sys.dm_xe_session_targets";
            var sql =
                "SELECT t.target_name, CAST(t.target_data AS NVARCHAR(MAX)) AS target_data " +
                $"FROM {sessions} s JOIN {targetsView} t ON t.event_session_address = s.address " +
                "WHERE s.name = @name";

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(sql, cn) { CommandType = CommandType.Text };
            cmd.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = sessionName;
            await cn.OpenAsync(ct);
            await using var rdr = await cmd.ExecuteReaderAsync(ct);
            while (await rdr.ReadAsync(ct))
            {
                var name = rdr.GetString(0);
                var data = rdr.IsDBNull(1) ? null : rdr.GetString(1);
                result[name] = data; // one target per type per session
            }
            return result;
        }

        /// <summary>
        /// The exact name of the session's <b>current</b> (newest) event_file, taken from the running target_data
        /// (<c>&lt;File name="...\base_0_133...xel"&gt;</c>).  Used to locate the file's directory + rollover stem for
        /// the fast binary (XELite) read.  Pure - fully unit-testable.
        /// </summary>
        public static string ResolveEventFileCurrentFile(string targetDataXml)
        {
            if (string.IsNullOrEmpty(targetDataXml)) return null;
            try
            {
                var root = XElement.Parse(targetDataXml);
                var file = root.Name.LocalName == "File" ? root : root.Descendants("File").FirstOrDefault();
                var name = file?.Attribute("name")?.Value;
                return string.IsNullOrEmpty(name) ? null : name;
            }
            catch { return null; }
        }

        /// <summary>
        /// Turns the running event_file target_data (<c>&lt;File name="...\base_0_133...xel"&gt;</c>) into a wildcard
        /// read path so file rollovers are followed - SQL appends a <c>_&lt;targetId&gt;_&lt;timestamp&gt;.xel</c>
        /// suffix.  Falls back to the exact file name if the suffix isn't recognised.  Pure - fully unit-testable.
        /// </summary>
        public static string ResolveEventFileReadPath(string targetDataXml)
        {
            var fileName = ResolveEventFileCurrentFile(targetDataXml);
            if (string.IsNullOrEmpty(fileName)) return null;
            return Regex.Replace(fileName, @"_\d+_\d+\.xel$", "*.xel");
        }
    }
}
