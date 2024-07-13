using Microsoft.Data.SqlClient;
using System;

namespace DBADashGUI.Performance
{
    internal class CounterThreshold
    {
        public int CounterID { get; set; }
        public string CounterName { get; set; }
        public string CounterInstance { get; set; }
        public string ObjectName { get; set; }
        public int? InstanceID { get; set; }
        public decimal? WarningFrom { get; set; }
        public decimal? WarningTo { get; set; }
        public decimal? CriticalFrom { get; set; }
        public decimal? CriticalTo { get; set; }
        public decimal? GoodFrom { get; set; }
        public decimal? GoodTo { get; set; }
        public decimal? SystemWarningFrom { get; set; }
        public decimal? SystemWarningTo { get; set; }
        public decimal? SystemCriticalFrom { get; set; }
        public decimal? SystemCriticalTo { get; set; }
        public decimal? SystemGoodFrom { get; set; }
        public decimal? SystemGoodTo { get; set; }

        public void Update(bool updateAllInstances)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("CounterThresholds_Upd", cn) { CommandType = System.Data.CommandType.StoredProcedure })
            {
                cn.Open();
                if (InstanceID.HasValue)
                {
                    cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                }
                cmd.Parameters.AddWithValue("object_name", ObjectName);
                cmd.Parameters.AddWithValue("counter_name", CounterName);
                if (!updateAllInstances)
                {
                    cmd.Parameters.AddWithValue("instance_name", CounterInstance);
                }
                cmd.Parameters.AddWithValue("CriticalFrom", CriticalFrom.HasValue ? CriticalFrom : DBNull.Value);
                cmd.Parameters.AddWithValue("CriticalTo", CriticalTo.HasValue ? CriticalTo : DBNull.Value);
                cmd.Parameters.AddWithValue("WarningFrom", WarningFrom.HasValue ? WarningFrom : DBNull.Value);
                cmd.Parameters.AddWithValue("WarningTo", WarningTo.HasValue ? WarningTo : DBNull.Value);
                cmd.Parameters.AddWithValue("GoodFrom", GoodFrom.HasValue ? GoodFrom : DBNull.Value);
                cmd.Parameters.AddWithValue("GoodTo", GoodTo.HasValue ? GoodTo : DBNull.Value);
                cmd.ExecuteNonQuery();
            }

        }

        public static CounterThreshold GetCounterThreshold(string ObjectName, string CounterName, string CounterInstance, int? InstanceID)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("CounterThresholds_Get", cn) { CommandType = System.Data.CommandType.StoredProcedure })
            {
                cn.Open();
                if (InstanceID.HasValue)
                {
                    cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                }
                cmd.Parameters.AddWithValue("object_name", ObjectName);
                cmd.Parameters.AddWithValue("counter_name", CounterName);
                cmd.Parameters.AddWithValue("instance_name", CounterInstance);
                using (var rdr = cmd.ExecuteReader())
                {

                    if (rdr.Read())
                    {
                        var threshold = new CounterThreshold
                        {
                            InstanceID = InstanceID,
                            ObjectName = ObjectName,
                            CounterName = CounterName,
                            CounterInstance = CounterInstance,
                            CriticalFrom = rdr["CriticalFrom"] == DBNull.Value ? null : (decimal)rdr["CriticalFrom"],
                            CriticalTo = rdr["CriticalTo"] == DBNull.Value ? null : (decimal)rdr["CriticalTo"],
                            WarningFrom = rdr["WarningFrom"] == DBNull.Value ? null : (decimal)rdr["WarningFrom"],
                            WarningTo = rdr["WarningTo"] == DBNull.Value ? null : (decimal)rdr["WarningTo"],
                            GoodFrom = rdr["GoodFrom"] == DBNull.Value ? null : (decimal)rdr["GoodFrom"],
                            GoodTo = rdr["GoodTo"] == DBNull.Value ? null : (decimal)rdr["GoodTo"],
                            SystemCriticalFrom = rdr["SystemCriticalFrom"] == DBNull.Value ? null : (decimal)rdr["SystemCriticalFrom"],
                            SystemCriticalTo = rdr["SystemCriticalTo"] == DBNull.Value ? null : (decimal)rdr["SystemCriticalTo"],
                            SystemWarningFrom = rdr["SystemWarningFrom"] == DBNull.Value ? null : (decimal)rdr["SystemWarningFrom"],
                            SystemWarningTo = rdr["SystemWarningTo"] == DBNull.Value ? null : (decimal)rdr["SystemWarningTo"],
                            SystemGoodFrom = rdr["SystemGoodFrom"] == DBNull.Value ? null : (decimal)rdr["SystemGoodFrom"],
                            SystemGoodTo = rdr["SystemGoodTo"] == DBNull.Value ? null : (decimal)rdr["SystemGoodTo"]
                        };
                        return threshold;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

    }

}



