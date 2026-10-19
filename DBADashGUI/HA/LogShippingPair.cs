using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;

namespace DBADashGUI.LogShipping
{
    /// <summary>
    /// A log shipping primary/secondary instance pair.  Databases on the primary that are not restoring/in standby on the
    /// secondary are reported as missing.  Without any pairs configured, log shipping monitoring works as before -
    /// restoring/standby databases are detected automatically.  Exclusions are configured separately (<see cref="LogShippingExclusions"/>).
    /// </summary>
    public class LogShippingPair
    {
        public int PrimaryInstanceID { get; set; }
        public string PrimaryInstance { get; set; }
        public int SecondaryInstanceID { get; set; }
        public string SecondaryInstance { get; set; }

        public static List<LogShippingPair> GetPairs()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.LogShippingPairs_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            var pairs = new List<LogShippingPair>();
            while (rdr.Read())
            {
                pairs.Add(new LogShippingPair
                {
                    PrimaryInstanceID = rdr.GetInt32(rdr.GetOrdinal("PrimaryInstanceID")),
                    PrimaryInstance = rdr.GetString(rdr.GetOrdinal("PrimaryInstance")),
                    SecondaryInstanceID = rdr.GetInt32(rdr.GetOrdinal("SecondaryInstanceID")),
                    SecondaryInstance = rdr.GetString(rdr.GetOrdinal("SecondaryInstance"))
                });
            }
            return pairs;
        }

        public void Save() => Update(false);

        public void Delete() => Update(true);

        private void Update(bool delete)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.LogShippingPair_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("PrimaryInstanceID", PrimaryInstanceID);
            cmd.Parameters.AddWithValue("SecondaryInstanceID", SecondaryInstanceID);
            cmd.Parameters.AddWithValue("Delete", delete);
            cn.Open();
            cmd.ExecuteNonQuery();
        }
    }
}
