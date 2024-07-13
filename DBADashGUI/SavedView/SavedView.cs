using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DBADashGUI
{
    /// <summary>
    /// A class inheriting SavedView is used to persist the state of a page.
    /// </summary>
    public abstract class SavedView
    {
        public enum ViewTypes
        {
            Metric,
            PerformanceSummary,
            Tree,
            SlowQueryDetail,
            Summary
        }

        [JsonIgnore]
        public string Name { get; set; }

        public abstract ViewTypes Type { get; }

        [JsonIgnore]
        public int UserID { get; set; } = DBADashUser.UserID;

        public virtual string Serialize()
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
            return json;
        }

        [JsonIgnore]
        public static readonly string DefaultViewName = "Default";

        public void Save()
        {
            if (UserID == DBADashUser.SystemUserID && !DBADashUser.HasManageGlobalViews)
            {
                throw new Exception("User doesn't have permissions to manage global views");
            }
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("DBADash.SavedViews_Upd", cn) { CommandType = System.Data.CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("Name", Name);
                cmd.Parameters.AddWithValue("ViewType", Type);
                cmd.Parameters.AddWithValue("SavedObject", Serialize());
                cmd.Parameters.AddWithValue("UserID", UserID);
                cmd.ExecuteNonQuery();
            }
        }

        public void Delete()
        {
            if (UserID == DBADashUser.SystemUserID && !DBADashUser.HasManageGlobalViews)
            {
                throw new Exception("User doesn't have permissions to manage global views");
            }
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("DBADash.SavedViews_Del", cn) { CommandType = System.Data.CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("Name", Name);
                cmd.Parameters.AddWithValue("ViewType", Type);
                cmd.Parameters.AddWithValue("UserID", UserID);
                cmd.ExecuteNonQuery();
            }
        }

        public static Dictionary<string, string> GetSavedViews(ViewTypes type, int UserID)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("DBADash.SavedViews_Get", cn) { CommandType = System.Data.CommandType.StoredProcedure })
            {
                cn.Open();
                cmd.Parameters.AddWithValue("UserID", UserID);
                cmd.Parameters.AddWithValue("ViewType", type);
                var savedViews = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        savedViews.Add((string)rdr["Name"], (string)rdr["SavedObject"]);
                    }
                }
                return savedViews;
            }
        }
    }
}