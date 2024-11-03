using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;

namespace DBADashGUI
{
    internal static class DBADashUser
    {
        public static readonly int SystemUserID = -1;

        public static int UserID { get; private set; }

        public static bool HasManageGlobalViews;

        public static bool AllowMessaging;

        public static bool AllowPlanForcing;

        public static bool IsAdmin;

        public static bool CommunityScripts;

        public static TimeZoneInfo UserTimeZone = TimeZoneInfo.Local;

        public static HashSet<string> Roles;

        public static void Update()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("DBADash.User_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("UserID", UserID);
            if (DateHelper.AppTimeZone == TimeZoneInfo.Local)
            {
                cmd.Parameters.AddWithValue("TimeZone", DBNull.Value);
            }
            else
            {
                cmd.Parameters.AddWithValue("TimeZone", UserTimeZone.Id);
            }

            cmd.Parameters.AddWithValue("Theme", SelectedTheme.ThemeIdentifier.ToString());
            cmd.ExecuteNonQuery();
        }

        public static void GetUser()
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("DBADash.User_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("UserName", Environment.UserName);
            var pUserID = new SqlParameter("UserID", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var pManageGlobalViews = new SqlParameter("ManageGlobalViews", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            var pAllowMessaging = new SqlParameter("AllowMessaging", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            var pAllowPlanForcing = new SqlParameter("AllowPlanForcing", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            var pTZ = new SqlParameter("TimeZone", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
            var pTheme = new SqlParameter("Theme", SqlDbType.VarChar, 50) { Direction = ParameterDirection.Output };
            var pIsAdmin = new SqlParameter("IsAdmin", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            cmd.Parameters.AddRange(new[] { pUserID, pManageGlobalViews, pTZ, pTheme, pAllowMessaging, pAllowPlanForcing, pIsAdmin });
            using var rdr = cmd.ExecuteReader();
            Roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (rdr.Read())
            {
                Roles.Add(rdr["name"].ToString());
            }

            rdr.Close();
            var id = Convert.ToInt32(pUserID.Value);
            if (id > 0)
            {
                UserID = (int)pUserID.Value;
                HasManageGlobalViews = (bool)pManageGlobalViews.Value;
                AllowMessaging = (bool)pAllowMessaging.Value;
                AllowPlanForcing = (bool)pAllowPlanForcing.Value;
                IsAdmin = (bool)pIsAdmin.Value;
                CommunityScripts = (Roles.Contains("CommunityScripts") && AllowMessaging) || IsAdmin;
                if (pTZ.Value != DBNull.Value)
                {
                    var tzID = (string)pTZ.Value;
                    try
                    {
                        UserTimeZone = TimeZoneInfo.FindSystemTimeZoneById(tzID);
                        DateHelper.AppTimeZone = UserTimeZone;
                    }
                    catch
                    {
                        MessageBox.Show("Time zone not found " + tzID, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        DateHelper.AppTimeZone = TimeZoneInfo.Local;
                        UserTimeZone = TimeZoneInfo.Local;
                    }
                }
                var themeType = Enum.TryParse(pTheme.Value as string, out ThemeType result) ? result : ThemeType.Default;

                SetTheme(themeType);
            }
            else
            {
                throw new Exception("Invalid UserID");
            }
        }

        public static void SetTheme(ThemeType type)
        {
            SelectedTheme = type switch
            {
                ThemeType.Default => new BaseTheme(),
                ThemeType.Dark => new DarkTheme(),
                ThemeType.White => new WhiteTheme(),
                _ => SelectedTheme
            };
        }

        public static BaseTheme SelectedTheme { get => ThemeExtensions.CurrentTheme; set => ThemeExtensions.CurrentTheme = value; }
    }
}