using DBADash.Messaging;
using DBADash.XE;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// A saved QuickXETrace configuration the user can reload later (e.g. "trace batch/rpc completed for a given app",
    /// "statement events for a SPID").  Everything except the <see cref="Name"/> (a storage key) is serialized to the
    /// <c>Definition</c> column; the row's UserID scopes it to one user or, when it is
    /// <see cref="DBADashUser.SystemUserID"/>, to everyone (a "global" template).
    /// </summary>
    public class XETraceTemplate
    {
        /// <summary>Template name.  Held in its own column, so it is not part of the serialized definition.</summary>
        [JsonIgnore]
        public string Name { get; set; }

        public XETraceEventType Events { get; set; } = XETraceEventType.RpcCompleted | XETraceEventType.SqlBatchCompleted;

        /// <summary>Arbitrary extra events (beyond the RPC/Batch/Error shortcuts) chosen from the catalog.</summary>
        public List<XETraceEventDef> ExtraEvents { get; set; } = new();

        /// <summary>Filters, each optionally flagged to prompt the user for its value when the template is loaded.</summary>
        public List<XETraceFilterTemplate> Filters { get; set; } = new();

        public List<XEActionDef> GlobalActions { get; set; } = new(XETraceDefinition.DefaultGlobalActions);

        public Dictionary<string, List<XECustomization>> EventCustomizations { get; set; } = new();

        public XETraceTargetPreference Target { get; set; } = XETraceTargetPreference.Auto;

        public int MaxDurationSeconds { get; set; } = 300;

        public bool CaptureXel { get; set; }

        /// <summary>
        /// Reload with "Include AG replicas" ticked, so the trace fans out to every monitored replica of the current
        /// instance's availability group(s).  The resolved replica list is NOT stored (membership can change) - only
        /// the intent; it is re-resolved against the current instance on load.
        /// </summary>
        public bool IncludeAgReplicas { get; set; }

        public string Serialize() => JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    /// <summary>
    /// A filter within a template.  When <see cref="Prompt"/> is set, loading the template asks the user for the value
    /// (defaulting to the stored <see cref="XEFilter.Value"/>) instead of applying the stored value directly - handy for
    /// per-session things like a SPID, application name or user name.
    /// </summary>
    public class XETraceFilterTemplate
    {
        public XEFilter Filter { get; set; }

        /// <summary>Prompt the user for this filter's value on load rather than using the stored value.</summary>
        public bool Prompt { get; set; }

        /// <summary>Optional label for the prompt (e.g. "Enter the SPID to trace").  Falls back to the field name.</summary>
        public string PromptText { get; set; }
    }

    /// <summary>Reads / writes <see cref="XETraceTemplate"/> rows via the XE.XETraceTemplate_* procs.</summary>
    internal static class XETraceTemplateStore
    {
        /// <summary>Loads the templates for a user (pass <see cref="DBADashUser.SystemUserID"/> for the global set).</summary>
        public static List<XETraceTemplate> Get(int userID)
        {
            var result = new List<XETraceTemplate>();
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("XE.XETraceTemplate_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("UserID", userID);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                XETraceTemplate template;
                try
                {
                    template = JsonConvert.DeserializeObject<XETraceTemplate>((string)rdr["Definition"]) ?? new XETraceTemplate();
                }
                catch
                {
                    continue; // skip a template that no longer deserializes (e.g. a future/newer format)
                }
                template.Name = (string)rdr["Name"];
                result.Add(template);
            }
            return result;
        }

        /// <summary>Saves (inserts or updates) a template under the given scope.</summary>
        public static void Save(int userID, XETraceTemplate template)
        {
            GuardGlobal(userID);
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("XE.XETraceTemplate_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("UserID", userID);
            cmd.Parameters.AddWithValue("Name", template.Name);
            cmd.Parameters.AddWithValue("Definition", template.Serialize());
            cmd.ExecuteNonQuery();
        }

        public static void Delete(int userID, string name)
        {
            GuardGlobal(userID);
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("XE.XETraceTemplate_Del", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            cmd.Parameters.AddWithValue("UserID", userID);
            cmd.Parameters.AddWithValue("Name", name);
            cmd.ExecuteNonQuery();
        }

        private static void GuardGlobal(int userID)
        {
            if (userID == DBADashUser.SystemUserID && !DBADashUser.HasManageGlobalViews)
            {
                throw new Exception("You don't have permission to manage global templates.");
            }
        }
    }
}
