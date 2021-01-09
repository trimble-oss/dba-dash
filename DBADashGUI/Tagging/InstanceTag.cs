using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI
{
    public class InstanceTag
    {
        public Int32 TagID { get; set; }
        public string TagName { get; set; }
        public string TagValue { get; set; }
        public string Instance { get; set; }

        public bool IsTagged { get; set; } = true;

        public override string ToString()
        {
            return TagName + " | " + TagValue;
        }

        public void Save(string connectionString)
        {
            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                string sql = @"InstanceTags_Add";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("Instance", Instance);
                cmd.Parameters.AddWithValue("TagName", TagName);
                cmd.Parameters.AddWithValue("TagValue", TagValue);
                var pTagID = cmd.Parameters.Add("TagID", SqlDbType.SmallInt);
                pTagID.Direction = ParameterDirection.Output;
                cmd.ExecuteNonQuery();
                TagID = (Int16)pTagID.Value;
            }
        }

        public void Delete(string connectionString)
        {

            SqlConnection cn = new SqlConnection(connectionString);
            using (cn)
            {
                cn.Open();
                string sql = @"InstanceTags_Del";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("Instance", Instance);
                cmd.Parameters.AddWithValue("TagName", TagName);
                cmd.Parameters.AddWithValue("TagValue", TagValue);
                cmd.ExecuteNonQuery();
            }
           
        }

        public static List<InstanceTag> GetInstanceTags(string connectionString,string instance)
        {
            var tags = new List<InstanceTag>();
                
            SqlConnection cn = new SqlConnection(connectionString);
        
            using (cn)
            {
                cn.Open();
                SqlCommand cmd = new SqlCommand("InstanceTags_Get", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("Instance", instance);
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        tags.Add(new InstanceTag() { TagID = (Int16)rdr[0], TagName = (string)rdr[1], TagValue = (string)rdr[2], Instance = instance, IsTagged=(bool)rdr[3] });
                    }
                }
            }
            return tags;
            
        }

    }
}
