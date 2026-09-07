using System;
using System.Data;

namespace DBADash.Deadlocks
{
    /// <summary>
    /// The shape of the deadlock collection as it travels from the collector to the repository.
    ///
    /// <para>Each factory here mirrors the matching table type in the repository database
    /// (dbo.Deadlocks, dbo.DeadlockProcesses, dbo.DeadlockResources).  <b>Column order matters</b>:
    /// SqlClient binds a DataTable to a table-valued parameter by ordinal, not by name, so a column
    /// added here must be added in the same position in the CREATE TYPE, and vice versa.</para>
    ///
    /// <para>The three tables are related by (EventTime, DeadlockHash), which is also the key they are
    /// stored under, so nothing has to be mapped or allocated on import.</para>
    /// </summary>
    public static class DeadlockTables
    {
        /// <summary>
        /// Width of DeadlockHash, in bytes.  The value is a SHA2_256 of the graph XML truncated to this
        /// length: it is the primary key of dbo.Deadlocks and propagates into both child tables, so it is
        /// kept narrower than the full digest while staying far beyond any collision concern.
        ///
        /// <para>Hash the graph as it arrived: normalise whitespace, so that the same event read through
        /// different targets hashes the same, but keep every attribute value.  This has to tell two
        /// occurrences apart, so - unlike DeadlockSignature, whose job is the exact
        /// opposite - nothing volatile may be removed first.  Transaction ids, per-process spid and
        /// wait time, and the graph-internal process and lock ids are what separate two distinct deadlocks
        /// that happen to share a signature and a millisecond.</para>
        /// </summary>
        public const int DeadlockHashBytes = 16;

        /// <summary>DataSet table names.  DBImporter keys off these, so they are also the wire contract.</summary>
        public const string DeadlocksTableName = "Deadlocks";

        public const string ProcessesTableName = "DeadlockProcesses";

        public const string ResourcesTableName = "DeadlockResources";

        /// <summary>
        /// Compresses a deadlock graph for storage.
        ///
        /// <para>UTF-16 then gzip, matching <see cref="SMOBaseClass.Zip"/> and the query plan cache, so
        /// that SQL Server's own DECOMPRESS reads the value back without any help from the application:
        /// <c>CAST(DECOMPRESS(DeadlockXmlCompressed) AS NVARCHAR(MAX))</c>.  Use
        /// <see cref="SMOBaseClass.Unzip"/> to read it in managed code.</para>
        ///
        /// <para>Worth doing beyond the storage saving: graph XML is tag dense, and the collection
        /// payload reaches a file or S3 destination as uncompressed XML, where a raw string column
        /// would also be XML escaped on the way.</para>
        /// </summary>
        public static byte[] CompressGraph(string xml) =>
            string.IsNullOrEmpty(xml) ? null : SMOBaseClass.Zip(xml);

        /// <summary>One row per deadlock, carrying the compressed graph alongside its header.</summary>
        public static DataTable CreateDeadlocksTable()
        {
            var dt = new DataTable(DeadlocksTableName);
            dt.Columns.Add("EventTime", typeof(DateTime));
            dt.Columns.Add("DeadlockHash", typeof(byte[]));
            dt.Columns.Add("Signature", typeof(string));
            dt.Columns.Add("SignatureVersion", typeof(byte));
            dt.Columns.Add("ProcessCount", typeof(short));
            dt.Columns.Add("VictimCount", typeof(short));
            dt.Columns.Add("ResourceCount", typeof(short));
            dt.Columns.Add("IsParallel", typeof(bool));
            dt.Columns.Add("DeadlockXmlCompressed", typeof(byte[]));
            return dt;
        }

        /// <summary>
        /// One row per process in the graph's process-list.  A process is not a session - a parallel
        /// query contributes several entries sharing a SPID and differing by Ecid - so ProcessIndex,
        /// the ordinal within the graph, is what identifies the row.
        /// </summary>
        public static DataTable CreateProcessesTable()
        {
            var dt = new DataTable(ProcessesTableName);
            dt.Columns.Add("EventTime", typeof(DateTime));
            dt.Columns.Add("DeadlockHash", typeof(byte[]));
            dt.Columns.Add("ProcessIndex", typeof(short));
            dt.Columns.Add("IsVictim", typeof(bool));
            // The source instance's database_id.  Resolved to a DBA Dash DatabaseID on import.
            dt.Columns.Add("database_id", typeof(int));
            dt.Columns.Add("SPID", typeof(int));
            dt.Columns.Add("Ecid", typeof(int));
            dt.Columns.Add("LoginName", typeof(string));
            dt.Columns.Add("HostName", typeof(string));
            dt.Columns.Add("ClientApp", typeof(string));
            dt.Columns.Add("ProcedureName", typeof(string));
            dt.Columns.Add("StatementText", typeof(string));
            dt.Columns.Add("IsolationLevel", typeof(string));
            dt.Columns.Add("LockMode", typeof(string));
            dt.Columns.Add("WaitResource", typeof(string));
            dt.Columns.Add("WaitTimeMs", typeof(long));
            dt.Columns.Add("LogUsed", typeof(long));
            dt.Columns.Add("TransactionName", typeof(string));
            dt.Columns.Add("Priority", typeof(short));
            dt.Columns.Add("LastBatchStarted", typeof(DateTime));
            dt.Columns.Add("LastBatchCompleted", typeof(DateTime));
            dt.Columns.Add("LastTransactionStarted", typeof(DateTime));
            dt.Columns.Add("Status", typeof(string));
            dt.Columns.Add("TransactionCount", typeof(int));
            dt.Columns.Add("HostPid", typeof(int));
            dt.Columns.Add("InputBuffer", typeof(string));
            // Raw SET option bitmasks, stored as collected - decoding happens at report time.
            dt.Columns.Add("ClientOption1", typeof(int));
            dt.Columns.Add("ClientOption2", typeof(int));
            return dt;
        }

        /// <summary>One row per resource in the graph's resource-list.</summary>
        public static DataTable CreateResourcesTable()
        {
            var dt = new DataTable(ResourcesTableName);
            dt.Columns.Add("EventTime", typeof(DateTime));
            dt.Columns.Add("DeadlockHash", typeof(byte[]));
            dt.Columns.Add("ResourceIndex", typeof(short));
            dt.Columns.Add("ResourceType", typeof(string));
            dt.Columns.Add("database_id", typeof(int));
            dt.Columns.Add("ObjectName", typeof(string));
            dt.Columns.Add("IndexName", typeof(string));
            dt.Columns.Add("LockMode", typeof(string));
            dt.Columns.Add("OwnerModes", typeof(string));
            dt.Columns.Add("WaiterModes", typeof(string));
            dt.Columns.Add("OwnerCount", typeof(short));
            dt.Columns.Add("WaiterCount", typeof(short));
            dt.Columns.Add("IsParallelismResource", typeof(bool));
            return dt;
        }
    }
}
