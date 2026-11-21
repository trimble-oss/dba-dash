using System;
using System.Data;

namespace DBADash
{
    /// <summary>
    /// DataSet table names for the Elastic Jobs collection, in the order SQLElasticJobs.sql returns them.
    /// DBImporter keys off these, so they are also the wire contract.
    ///
    /// <para>The tables are sent to dbo.ElasticJobs_Upd together as table-valued parameters, which bind by
    /// ordinal rather than by name - a column added to the collection query must be added in the same position
    /// in the matching CREATE TYPE.</para>
    /// </summary>
    public static class ElasticJobsTables
    {
        public const string JobsTableName = "ElasticJobs";

        public const string StepsTableName = "ElasticJobSteps";

        public const string TargetGroupMembersTableName = "ElasticJobTargetGroupMembers";

        public const string ExecutionsTableName = "ElasticJobExecutions";

        /// <summary>Result sets of SQLElasticJobs.sql that are imported, in order.</summary>
        public static readonly string[] TableNames =
        [
            JobsTableName, StepsTableName, TargetGroupMembersTableName, ExecutionsTableName
        ];

        /// <summary>
        /// The last result set: the time on the job database's server when the collection ran, the hash of the definitions and whether
        /// they were returned.  Read by the collector to know where to start the next collection from (<see cref="DBCollector.ElasticJobsFromUtc"/>)
        /// and which definitions it has sent - not imported.  Definitions that weren't returned aren't sent: their tables are left out.
        /// </summary>
        public const int SnapshotDateResultIndex = 4;

        /// <summary>
        /// Executions that changed this long before the previous collection ran are collected again, for rows committed
        /// after they were timestamped.  The repository upserts executions, so a row collected twice does no harm.
        /// </summary>
        public const int OverlapMinutes = 10;

        /// <summary>How far back the first collection goes - the same as for SQL Agent job history.</summary>
        public const int InitialLookbackDays = 7;

        /// <summary>How far back a collection run on demand (from the GUI) goes, with every active execution as well.</summary>
        public const int OnDemandLookbackHours = 24;

        /// <summary>
        /// Unchanged definitions are sent again after this long anyway, so a repository that lost them (restored, or the instance removed and
        /// collected again) gets them back without waiting for a job to change.
        /// </summary>
        public const int DefinitionsRefreshMinutes = 60;

        /// <summary>
        /// The hash to compare the definitions with: the last one sent, unless that was too long ago (<see cref="DefinitionsRefreshMinutes"/>)
        /// or nothing has been sent yet - null, which sends them.
        /// </summary>
        public static byte[] DefinitionsHashToCompare(byte[] lastSentHash, DateTime? lastSentUtc, DateTime nowUtc) =>
            lastSentHash != null && lastSentUtc > nowUtc.AddMinutes(-DefinitionsRefreshMinutes) ? lastSentHash : null;

        /// <summary>
        /// Where the next collection starts from: the time this one ran on the job database's server, less <see cref="OverlapMinutes"/> - or
        /// the creation time of the oldest execution still active, if that's earlier.  An execution is collected again until it's seen complete.
        /// </summary>
        /// <param name="executions">The executions this collection returned</param>
        /// <param name="collectedAtUtc">The time the collection ran on the job database's server</param>
        public static DateTime NextFromUtc(DataTable executions, DateTime collectedAtUtc)
        {
            var nextFrom = collectedAtUtc.AddMinutes(-OverlapMinutes);
            foreach (DataRow r in executions.Rows)
            {
                if ((bool)r["is_active"] && (DateTime)r["create_time"] < nextFrom)
                {
                    nextFrom = (DateTime)r["create_time"];
                }
            }
            return nextFrom;
        }
    }
}
