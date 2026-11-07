using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADashGUI
{
    public class DBADashTag
    {
        public int TagID { get; set; }
        public string TagName { get; set; }
        public string TagValue { get; set; }

        public static DBADashTag AllInstancesTag() => new() { TagID = -1 };

        public static List<DBADashTag> GetTags(string connectionString, string tagFilter = "", int? tagId = null)
        {
            var tags = new List<DBADashTag>();
            if (tagId == -1)
            {
                tags.Add(AllInstancesTag());
                return tags;
            }
            using SqlConnection cn = new(connectionString);
            using SqlCommand cmd = new("Tags_Get", cn) { CommandType = CommandType.StoredProcedure };

            cn.Open();
            if (tagFilter?.Length > 0)
            {
                cmd.Parameters.AddWithValue("TagFilters", tagFilter);
            }

            if (tagId != null)
            {
                cmd.Parameters.AddWithValue("TagID", tagId);
            }
            var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                tags.Add(new DBADashTag { TagID = (int)rdr[0], TagName = (string)rdr[1], TagValue = (string)rdr[2] });
            }

            return tags;
        }

        /// <summary>Tags by ID, whether or not they're assigned to active instances (GetTags only returns tags in use).  Missing tags are left out.</summary>
        public static List<DBADashTag> GetTagsByID(string connectionString, IEnumerable<int> tagIDs)
        {
            var tags = new List<DBADashTag>();
            var ids = string.Join(",", tagIDs.Where(id => id > 0).Distinct());
            if (ids.Length == 0) return tags;
            using SqlConnection cn = new(connectionString);
            using SqlCommand cmd = new("Tags_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("TagIDs", ids);
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                tags.Add(new DBADashTag { TagID = (int)rdr[0], TagName = (string)rdr[1], TagValue = (string)rdr[2] });
            }
            return tags;
        }

        public static DBADashTag GetTag(string connectionString, int tagId)
        {
            var tags = GetTags(connectionString, default, tagId);
            return tags.Count switch
            {
                0 => throw new Exception("Tag not found"),
                1 => tags.First(),
                _ => throw new Exception("Unexpected Tag count")
            };
        }

        public override string ToString()
        {
            return TagID <= 0 ? "{All}" : TagName + ":" + TagValue;
        }
    }
}