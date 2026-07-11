using DBADash;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DBADashService
{
    /// <summary>
    /// Reports a lightweight "collection ran, nothing changed" heartbeat to the repository for
    /// change-detection collections (currently Jobs).  Such collections only write data when the source
    /// actually changed, so without a heartbeat their dbo.CollectionDates entry would stop advancing and
    /// CollectionDatesStatus would flag them as overdue even though collection is working normally.
    ///
    /// The heartbeat advances only CollectionDates.HeartbeatDate - SnapshotDate keeps tracking the last time
    /// data was really collected, so the two can be told apart (see CollectionDatesHeartbeat_Upd).  Mirrors
    /// <see cref="ScheduleInfoReporter"/>: the data comes from the service, so it's written straight to the
    /// destinations rather than going through a full instance collection.
    /// </summary>
    public static class CollectionHeartbeatReporter
    {
        public static async Task ReportAsync(DBADashSource source, IEnumerable<string> references, CollectionConfig config)
        {
            // A heartbeat only makes sense for an instance that has already collected (so its CollectionDates
            // row exists and its ConnectionID is known) - CollectionDatesHeartbeat_Upd is a no-op otherwise.
            if (string.IsNullOrEmpty(source.ConnectionID)) return;

            var refs = references?.Where(r => !string.IsNullOrEmpty(r)).Distinct().ToArray() ?? Array.Empty<string>();
            if (refs.Length == 0) return;

            var ds = BuildDataSet(source, refs);
            var fileName = DBADashSource.GenerateFileName(source.SourceConnection.ConnectionForFileName + "_Heartbeat");
            try
            {
                await DestinationHandling.WriteAllDestinationsAsync(ds, fileName, config);
            }
            catch (Exception ex)
            {
                // A missed heartbeat isn't worth failing the collection over - it just means the status may
                // briefly show as overdue until the next heartbeat or a real collection.
                Log.Warning(ex, "Error writing collection heartbeat for {references} on {instance}",
                    string.Join(", ", refs), source.SourceConnection.ConnectionForPrint);
            }
        }

        private static DataSet BuildDataSet(DBADashSource source, string[] references)
        {
            var ds = new DataSet("CollectionDatesHeartbeat");

            var dtAgent = new DataTable("DBADash");
            DBCollector.AddDBADashServiceMetadata(ref dtAgent);
            dtAgent.Columns.Add("SnapshotDateUTC", typeof(DateTime));
            // Folder/bucket destinations identify a deserialized file by Tables["DBADash"].Rows[0]["Instance"]/
            // ["DBName"] before importing it - these must be present or that lookup throws and serializes
            // unrelated instances' file processing together (see the same note in ScheduleInfoReporter).
            dtAgent.Columns.Add("Instance", typeof(string));
            dtAgent.Columns.Add("DBName", typeof(string));
            dtAgent.Rows[0]["ConnectionID"] = source.ConnectionID;
            dtAgent.Rows[0]["SnapshotDateUTC"] = DateTime.UtcNow;
            dtAgent.Rows[0]["Instance"] = source.ConnectionID;
            dtAgent.Rows[0]["DBName"] = "CollectionDatesHeartbeat";
            ds.Tables.Add(dtAgent);

            // Table name must match the TVP type dbo.CollectionDatesHeartbeat (the importer infers the type
            // name from DataTable.TableName when passing it as a structured parameter).
            var dt = new DataTable("CollectionDatesHeartbeat");
            dt.Columns.Add("Reference", typeof(string));
            foreach (var reference in references)
            {
                var row = dt.NewRow();
                row["Reference"] = reference;
                dt.Rows.Add(row);
            }
            ds.Tables.Add(dt);
            return ds;
        }
    }
}
