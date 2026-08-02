using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;

namespace DBADashGUI.AgentJobs
{
    /// <summary>
    /// A name/category/description based exclusion that removes matching agent jobs from job-failure monitoring
    /// (the job is forced to N/A status).  Defined at Root (InstanceID = -1) or instance level.
    /// See issue #1175.
    /// </summary>
    public class AgentJobExclusion
    {
        public int AgentJobExclusionID { get; set; }
        public int InstanceID { get; set; }
        public string JobNameFilter { get; set; }
        public string CategoryFilter { get; set; }
        public string DescriptionFilter { get; set; }

        public static List<AgentJobExclusion> GetExclusions(int instanceID, string connectionString)
        {
            var exclusions = new List<AgentJobExclusion>();
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("AgentJobExclusions_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("InstanceID", instanceID);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                exclusions.Add(new AgentJobExclusion
                {
                    AgentJobExclusionID = (int)rdr["AgentJobExclusionID"],
                    InstanceID = (int)rdr["InstanceID"],
                    JobNameFilter = rdr["JobNameFilter"] == System.DBNull.Value ? null : (string)rdr["JobNameFilter"],
                    CategoryFilter = rdr["CategoryFilter"] == System.DBNull.Value ? null : (string)rdr["CategoryFilter"],
                    DescriptionFilter = rdr["DescriptionFilter"] == System.DBNull.Value ? null : (string)rdr["DescriptionFilter"]
                });
            }
            return exclusions;
        }

        public void Save(string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("AgentJobExclusions_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            // Type the output param explicitly - passing DBNull via AddWithValue gives SqlClient no CLR type to
            // infer from, so it defaults to a zero-size NVarChar which is then rejected ("String[0]: ... size of 0").
            var pID = cmd.Parameters.Add("AgentJobExclusionID", SqlDbType.Int);
            pID.Direction = ParameterDirection.InputOutput;
            pID.Value = AgentJobExclusionID == 0 ? (object)System.DBNull.Value : AgentJobExclusionID;
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            // Add string params with an explicit type/size - a null string via AddWithValue(DBNull) produces a
            // zero-size NVarChar parameter which SqlClient rejects ("String[0]: the Size property has an invalid size of 0").
            cmd.Parameters.Add("JobNameFilter", SqlDbType.NVarChar, 128).Value = (object)JobNameFilter ?? System.DBNull.Value;
            cmd.Parameters.Add("CategoryFilter", SqlDbType.NVarChar, 128).Value = (object)CategoryFilter ?? System.DBNull.Value;
            cmd.Parameters.Add("DescriptionFilter", SqlDbType.NVarChar, 512).Value = (object)DescriptionFilter ?? System.DBNull.Value;
            cmd.ExecuteNonQuery();
            AgentJobExclusionID = (int)pID.Value;
        }

        public static void Delete(int agentJobExclusionID, string connectionString)
        {
            using var cn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("AgentJobExclusions_Del", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("AgentJobExclusionID", agentJobExclusionID);
            cmd.ExecuteNonQuery();
        }
    }
}
