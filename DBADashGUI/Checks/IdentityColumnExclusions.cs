using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADashGUI.Checks
{
    /// <summary>
    /// Identity column table exclusions at root (-1,-1), instance (InstanceID,-1) or database (InstanceID,DatabaseID) level.
    /// Excluded tables are reported with N/A status.  Matched against the table name and schema.table.  % can be used as a wildcard.
    /// </summary>
    public static class IdentityColumnExclusions
    {
        public static List<string> Get(int instanceId, int databaseId)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.IdentityColumnExclusions_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", instanceId);
            cmd.Parameters.AddWithValue("DatabaseID", databaseId);
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            var exclusions = new List<string>();
            while (rdr.Read())
            {
                exclusions.Add(rdr.GetString(rdr.GetOrdinal("ObjectName")));
            }
            return exclusions;
        }

        public static void Save(int instanceId, int databaseId, IEnumerable<string> exclusions)
        {
            var dtExclusions = new DataTable();
            dtExclusions.Columns.Add("ObjectName", typeof(string));
            foreach (var name in exclusions.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                dtExclusions.Rows.Add(name);
            }

            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.IdentityColumnExclusions_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", instanceId);
            cmd.Parameters.AddWithValue("DatabaseID", databaseId);
            cmd.Parameters.Add(new SqlParameter("Exclusions", SqlDbType.Structured) { TypeName = "dbo.IdentityColumnExclusions", Value = dtExclusions });
            cn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}
