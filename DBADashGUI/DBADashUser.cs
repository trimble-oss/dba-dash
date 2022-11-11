using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace DBADashGUI
{
    internal static class DBADashUser
    {
        public static readonly int SystemUserID = -1;

        private static int _UserID = 0;
        public static int UserID { get=> _UserID; }

        public static bool HasManageGlobalViews = false;

        public static void GetUser()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("DBADash.User_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("UserName", Environment.UserName);
                var pUserID = new SqlParameter("UserID", SqlDbType.Int) { Direction = ParameterDirection.Output };
                var pManageGlobalViews = new SqlParameter("ManageGlobalViews", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pUserID);
                cmd.Parameters.Add(pManageGlobalViews);
                cmd.ExecuteNonQuery();
                int id = Convert.ToInt32(pUserID.Value);
                if (id > 0)
                {
                    _UserID = (int)pUserID.Value;
                    HasManageGlobalViews = (bool)pManageGlobalViews.Value;
                }
                else{
                    throw new Exception("Invalid UserID");
                }
            }
        }
    }
}
