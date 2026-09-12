using System;
using System.Data;

namespace DBADash.QueryStats
{
    /// <summary>
    /// The shape of the query stats collection as it travels from the collector to the repository.
    ///
    /// <para>Each factory here mirrors the matching table type in the repository database (dbo.QueryStats,
    /// dbo.QueryStatsCollection).  <b>Column order matters</b>: SqlClient binds a DataTable to a
    /// table-valued parameter by ordinal, not by name, so a column added here must be added in the same
    /// position in the CREATE TYPE, and vice versa.</para>
    ///
    /// <para>Unlike most collections, what travels is not what the source returned.  The agent reads the
    /// plan cache at its own grain - one row per cached plan per statement - diffs it against the previous
    /// snapshot, and sends only the deltas that survived ranking plus the rollups that account for the
    /// rest.  See <see cref="QueryStatsProcessor"/>.</para>
    /// </summary>
    public static class QueryStatsTables
    {
        /// <summary>DataSet table names.  DBImporter keys off these, so they are also the wire contract.</summary>
        public const string QueryStatsTableName = "QueryStats";

        public const string CollectionTableName = "QueryStatsCollection";

        /// <summary>
        /// What a fact row identifies.  Ad hoc and module statements are identified differently because
        /// their handles behave differently: an ad hoc sql_handle is a hash of the batch text, so every
        /// literal value in it makes a different handle, while a module handle contains the object_id and
        /// moves whenever the object is dropped and recreated.  Ad hoc statements are therefore identified by
        /// their shape - database and query_hash - and module statements by database, schema, object name
        /// and offsets.  Either way the handle is kept only as an attribute, for fetching text.
        ///
        /// <para>The rollup types are how the collection stays bounded without under-reporting the period:
        /// whatever ranking discards is summed into one of them rather than dropped.</para>
        /// </summary>
        public enum StatementTypes : byte
        {
            /// <summary>An ad hoc or prepared statement with no query_hash, identified by sql_handle and
            /// offsets.  Every other ad hoc statement is an <see cref="AdHocShape"/>.</summary>
            AdHoc = 0,

            /// <summary>A statement inside a procedure, function or trigger, identified by the object.</summary>
            Module = 1,

            /// <summary>Every query family that missed the ranking cut, summed per database and interval.</summary>
            OtherQueries = 2,

            /// <summary>The statements of a kept family beyond the per-family statement cap: the same query in
            /// more procedures than the cap keeps.</summary>
            OtherVariants = 3,

            /// <summary>
            /// The <see cref="OtherQueries"/> rollups of the databases past the cap on them, summed into one row per
            /// interval.  An instance hosting thousands of databases would otherwise write a rollup row for each
            /// of them every interval, however little each one did.
            /// </summary>
            OtherDatabases = 4,

            /// <summary>
            /// An ad hoc or prepared statement identified by its database and query_hash, so every literal variant
            /// of a query is one statement with one history rather than a new statement for every value it was
            /// sent with.  Its sql_handle and offsets are one variant's, kept as an example along with the
            /// template the statement is shown as - see <see cref="QueryTemplate"/>.
            /// </summary>
            AdHocShape = 5
        }

        /// <summary>
        /// The delta table.  One row per statement per plan shape per interval, plus the rollup rows.
        /// </summary>
        public static DataTable GetQueryStatsSchema()
        {
            var dt = new DataTable(QueryStatsTableName);
            dt.Columns.Add("StatementType", typeof(byte));
            dt.Columns.Add("database_name", typeof(string));
            dt.Columns.Add("schema_name", typeof(string));
            dt.Columns.Add("object_name", typeof(string));
            dt.Columns.Add("object_id", typeof(int));
            dt.Columns.Add("sql_handle", typeof(byte[]));
            dt.Columns.Add("statement_start_offset", typeof(int));
            dt.Columns.Add("statement_end_offset", typeof(int));
            dt.Columns.Add("query_hash", typeof(byte[]));
            dt.Columns.Add("query_plan_hash", typeof(byte[]));
            dt.Columns.Add("SnapshotDate", typeof(DateTime));
            dt.Columns.Add("PeriodTime", typeof(long));
            dt.Columns.Add("execution_count", typeof(long));
            dt.Columns.Add("total_worker_time", typeof(long));
            dt.Columns.Add("total_elapsed_time", typeof(long));
            dt.Columns.Add("total_logical_reads", typeof(long));
            dt.Columns.Add("total_logical_writes", typeof(long));
            dt.Columns.Add("total_physical_reads", typeof(long));
            dt.Columns.Add("total_clr_time", typeof(long));
            dt.Columns.Add("total_rows", typeof(long));
            dt.Columns.Add("total_dop", typeof(long));
            dt.Columns.Add("total_grant_kb", typeof(long));
            dt.Columns.Add("total_used_grant_kb", typeof(long));
            dt.Columns.Add("total_spills", typeof(long));
            dt.Columns.Add("PlanCount", typeof(int));
            dt.Columns.Add("IsCompile", typeof(bool));
            dt.Columns.Add("IsOtherPlans", typeof(bool));
            // An ad hoc shape's template and the batch of the variant it was made from.  Set on one row of the
            // shape, and only by a collection that fetched them: the repository keeps the first it is sent.
            dt.Columns.Add("StatementTemplate", typeof(string));
            dt.Columns.Add("ExampleBatchText", typeof(string));
            // The plan of the row's plan shape, GZip compressed.  Set only by a collection that fetched it, and only
            // on the row of the plan shape it is the plan for: the repository keeps the first it is sent.
            dt.Columns.Add("query_plan_compressed", typeof(byte[]));
            return dt;
        }

        /// <summary>
        /// One row per collection, recording what the interval saw and what it could not account for.
        /// This is what makes "is this window complete" answerable rather than assumed, so it is written
        /// even when the collection produced no fact rows at all.
        /// </summary>
        public static DataTable GetCollectionSchema()
        {
            var dt = new DataTable(CollectionTableName);
            dt.Columns.Add("SnapshotDate", typeof(DateTime));
            dt.Columns.Add("PeriodTime", typeof(long));
            dt.Columns.Add("DMVRowCount", typeof(int));
            dt.Columns.Add("RowsWithDelta", typeof(int));
            dt.Columns.Add("RowsUnattributed", typeof(int));
            dt.Columns.Add("UnattributedWorkerTime", typeof(long));
            dt.Columns.Add("UnattributedElapsedTime", typeof(long));
            dt.Columns.Add("UnattributedExecutions", typeof(long));
            dt.Columns.Add("FamilyCount", typeof(int));
            dt.Columns.Add("FamiliesKept", typeof(int));
            dt.Columns.Add("StatementsKept", typeof(int));
            dt.Columns.Add("RowsPersisted", typeof(int));
            dt.Columns.Add("BaselineCount", typeof(int));
            dt.Columns.Add("BaselineEvictions", typeof(int));
            dt.Columns.Add("ReadDurationMs", typeof(int));
            dt.Columns.Add("ProcessingDurationMs", typeof(int));
            dt.Columns.Add("IsFirstCollection", typeof(bool));
            dt.Columns.Add("IsSkipped", typeof(bool));
            return dt;
        }
    }
}
