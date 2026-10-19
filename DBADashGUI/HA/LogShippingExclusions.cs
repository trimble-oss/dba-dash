using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADashGUI.LogShipping
{
    /// <summary>
    /// Log shipping database exclusions at root (-1) or secondary instance level.  Excluded databases are reported with
    /// N/A status - this applies to restoring/standby databases and to databases missing from the secondary (log shipping pairs).
    /// % can be used as a wildcard.
    /// </summary>
    public static class LogShippingExclusions
    {
        public static List<string> Get(int instanceId)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.LogShippingExclusions_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", instanceId);
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            var exclusions = new List<string>();
            while (rdr.Read())
            {
                exclusions.Add(rdr.GetString(rdr.GetOrdinal("DatabaseName")));
            }
            return exclusions;
        }

        public static void Save(int instanceId, IEnumerable<string> exclusions)
        {
            var dtExclusions = new DataTable();
            dtExclusions.Columns.Add("DatabaseName", typeof(string));
            foreach (var name in exclusions.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                dtExclusions.Rows.Add(name);
            }

            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.LogShippingExclusions_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", instanceId);
            cmd.Parameters.Add(new SqlParameter("Exclusions", SqlDbType.Structured) { TypeName = "dbo.LogShippingExclusions", Value = dtExclusions });
            cn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}
