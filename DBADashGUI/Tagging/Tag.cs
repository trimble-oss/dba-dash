using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI
{
    public class DBADashTag
    {
        public Int16 TagID { get; set; }
        public string TagName { get; set; }
        public string TagValue { get; set; }

        public static List<DBADashTag> GetTags(string tagFilter= "")
        {
            var tags = new List<DBADashTag>();
            SqlConnection cn = new SqlConnection(Common.ConnectionString);
            using (cn)
            {
                using (SqlCommand cmd = new SqlCommand("Tags_Get", cn) { CommandType = CommandType.StoredProcedure })
                {
                    cn.Open();
                    if (tagFilter.Length > 0)
                    {
                        cmd.Parameters.AddWithValue("TagFilters", tagFilter);
                    }
                    var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        tags.Add(new DBADashTag { TagID = (Int16)rdr[0], TagName = (string)rdr[1], TagValue = (string)rdr[2]});
                    }   
                }         
            }
            return tags;
        }

    }
}
