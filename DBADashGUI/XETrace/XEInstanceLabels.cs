using System;
using System.Data;
using System.Linq;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Resolves an instance's display label from <see cref="CommonData.Instances"/> - "server / database" for an Azure
    /// SQL database, the instance group name otherwise.  The source-instance identity for a trace is the session's
    /// <c>InstanceID</c> (see <c>XE.XETraceSession</c>); the label is resolved here at display time so it is always
    /// populated, unique per database, and reflects the current alias - shared by the instance picker, the live grid's
    /// source-instance stamp and the stored-event expander so every view labels instances identically.
    /// </summary>
    internal static class XEInstanceLabels
    {
        /// <summary>
        /// The label for an instance, or <paramref name="fallback"/> when the row/label can't be resolved (e.g. the
        /// instance was removed from monitoring after the trace ran).
        /// </summary>
        public static string Resolve(int instanceId, string fallback = null)
        {
            var row = CommonData.Instances?.Select($"InstanceID={instanceId}").FirstOrDefault();
            var label = row == null ? null : ToCandidate(row)?.ListLabel;
            return string.IsNullOrEmpty(label) ? fallback : label;
        }

        /// <summary>
        /// Maps a CommonData.Instances row to a pickable candidate.  Each Azure SQL database is its own monitored
        /// instance (its own InstanceID / ConnectionID) sharing a logical server, so Azure rows are exposed per-database
        /// under their server; regular instances are a single leaf.  Returns null for rows with nothing to label.
        /// </summary>
        public static XEInstanceCandidate ToCandidate(DataRow r)
        {
            var id = Convert.ToInt32(r["InstanceID"]);
            if (id <= 0) return null;
            var isAzure = r["IsAzure"] != DBNull.Value && Convert.ToBoolean(r["IsAzure"]);
            var server = r["Instance"] as string;
            if (isAzure)
            {
                var db = r["AzureDBName"] as string;
                if (string.IsNullOrEmpty(db) || string.IsNullOrEmpty(server)) return null;
                // 'master' isn't a useful XE trace target on Azure DB - skip it so the picker only lists user databases.
                if (string.Equals(db, "master", StringComparison.OrdinalIgnoreCase)) return null;
                return new XEInstanceCandidate { InstanceID = id, IsAzure = true, ServerName = server, DatabaseName = db };
            }
            var name = r["InstanceGroupName"] as string ?? server;
            return string.IsNullOrEmpty(name)
                ? null
                : new XEInstanceCandidate { InstanceID = id, IsAzure = false, DisplayName = name };
        }
    }
}
