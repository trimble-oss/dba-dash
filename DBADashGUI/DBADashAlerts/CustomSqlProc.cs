using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;

namespace DBADashGUI.DBADashAlerts
{
    /// <summary>
    /// A stored procedure in the UserAlert schema that can back a custom SQL alert rule.
    /// </summary>
    public class CustomSqlProc
    {
        public string ProcName { get; set; }
        public string QualifiedName { get; set; }

        /// <summary>Best-effort check that the proc returns the expected result set contract (InstanceID, AlertKey, Message).</summary>
        public bool IsValidSchema { get; set; }

        /// <summary>True if at least one custom SQL alert rule already references this proc.</summary>
        public bool InUse { get; set; }

        /// <summary>True if the current user can author/edit UserAlert procs (db_ddladmin/db_owner).</summary>
        public bool CanEdit { get; set; }

        public override string ToString() => ProcName;

        private static List<CustomSqlProc> _cached;

        /// <summary>Cached list of procs. Shared by the picker and rule validation. Call <see cref="Invalidate"/> to refresh.</summary>
        public static List<CustomSqlProc> Cached => _cached ??= GetProcs(Common.ConnectionString);

        /// <summary>Force a fresh load next time (e.g. after new procs are created).</summary>
        public static void Invalidate() => _cached = null;

        public static List<CustomSqlProc> GetProcs(string connectionString)
        {
            var procs = new List<CustomSqlProc>();
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("Alert.CustomSqlProcs_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                procs.Add(new CustomSqlProc
                {
                    ProcName = rdr.GetString("ProcName"),
                    QualifiedName = rdr.GetString("QualifiedName"),
                    IsValidSchema = rdr.GetBoolean("IsValidSchema"),
                    InUse = rdr.GetBoolean("InUse"),
                    CanEdit = rdr.GetBoolean("CanEdit")
                });
            }
            return procs;
        }
    }
}
