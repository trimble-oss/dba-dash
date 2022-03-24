using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI
{
    public class InstanceTag :DBADashTag
    {
        public string Instance { get; set; }
        public int InstanceID { get; set; }

        public bool IsTagged { get; set; } = true;

        public override string ToString()
        {
            return TagName + " | " + TagValue;
        }

        public void Save()
        {
            if (IsTagged)
            {
                save();
            }
            else
            {
                delete();
            }
        }

        private void save()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.InstanceTags_Add", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
              
                cmd.Parameters.AddWithValue("Instance", Instance);
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("TagName", TagName);
                cmd.Parameters.AddWithValue("TagValue", TagValue);
                var pTagID = cmd.Parameters.Add("TagID", SqlDbType.Int);
                pTagID.Direction = ParameterDirection.Output;
                cmd.ExecuteNonQuery();
                TagID = (int)pTagID.Value;               
            }
        }


       private void delete()
        {

            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.InstanceTags_Del", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
          
                cmd.Parameters.AddWithValue("Instance", Instance);
                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("TagName", TagName);
                cmd.Parameters.AddWithValue("TagValue", TagValue);
                cmd.ExecuteNonQuery();             
            }
           
        }

        public static List<InstanceTag> GetInstanceTags(string instance,int instanceID)
        {
            var tags = new List<InstanceTag>();
                

            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("InstanceTags_Get", cn) { CommandType = CommandType.StoredProcedure }) { 
                cn.Open();
                                
                cmd.Parameters.AddWithValue("Instance", instance);
                cmd.Parameters.AddWithValue("InstanceID", instanceID);
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        tags.Add(new InstanceTag() { TagID = (int)rdr[0], TagName = (string)rdr[1], TagValue = (string)rdr[2], Instance = instance, IsTagged = (bool)rdr[3] });
                    }
                }
                
            }
            return tags;
            
        }

    }
}
