using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows;

namespace DBADashGUI
{
    internal static class DBADashUser
    {
        public static readonly int SystemUserID = -1;

        private static int _UserID = 0;
        public static int UserID { get => _UserID; }

        public static bool HasManageGlobalViews = false;

        public static void SetUserTimeZone(TimeZoneInfo timeZone)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("DBADash.User_TimeZone_Upd", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("UserID", UserID);
                if (timeZone == TimeZoneInfo.Local)
                {
                    cmd.Parameters.AddWithValue("TimeZone", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("TimeZone", timeZone.Id);
                }
                cmd.ExecuteNonQuery();
            }
        }

        public static void GetUser()
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("DBADash.User_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("UserName", Environment.UserName);
                var pUserID = new SqlParameter("UserID", SqlDbType.Int) { Direction = ParameterDirection.Output };
                var pManageGlobalViews = new SqlParameter("ManageGlobalViews", SqlDbType.Bit) { Direction = ParameterDirection.Output };
                var pTZ = new SqlParameter("TimeZone", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pUserID);
                cmd.Parameters.Add(pManageGlobalViews);
                cmd.Parameters.Add(pTZ);
                cmd.ExecuteNonQuery();
                int id = Convert.ToInt32(pUserID.Value);
                if (id > 0)
                {
                    _UserID = (int)pUserID.Value;
                    HasManageGlobalViews = (bool)pManageGlobalViews.Value;
                    if (pTZ.Value != DBNull.Value)
                    {
                        string tzID = (string)pTZ.Value;
                        try
                        {
                            DateHelper.AppTimeZone = TimeZoneInfo.FindSystemTimeZoneById(tzID);
                        }
                        catch
                        {
                            MessageBox.Show("Time zone not found " + tzID, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            DateHelper.AppTimeZone = TimeZoneInfo.Local;
                        }
                    }
                }
                else
                {
                    throw new Exception("Invalid UserID");
                }
            }
        }
    }
}