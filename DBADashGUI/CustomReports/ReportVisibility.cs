using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DBADashGUI.CustomReports
{
    /// <summary>
    /// A tag name/value pair used in report visibility rules.  Stored by name rather than TagID so report metadata
    /// remains valid when scripted to another repository.
    /// </summary>
    public class ReportTag
    {
        public string TagName { get; set; }
        public string TagValue { get; set; }

        public override string ToString() => $"{TagName}: {TagValue}";
    }

    /// <summary>
    /// Instance attributes needed to decide if a report applies to an instance: engine edition, ConnectionID and tags.
    /// Editions and ConnectionIDs come from <see cref="CommonData.Instances"/> (the instances in the tree).  Tags are
    /// loaded on first use as they are only needed when a report has a tag rule.
    /// </summary>
    internal static class ReportInstanceInfo
    {
        private static readonly object Lock = new();
        private static DataTable _instancesSource;
        private static Dictionary<int, (DatabaseEngineEdition Edition, string ConnectionID)> _instances = new();
        private static Dictionary<int, List<ReportTag>> _tags;
        private static Guid _tagsConnection = Guid.Empty;

        public static void ClearCache()
        {
            lock (Lock)
            {
                _tags = null;
                _instancesSource = null;
            }
        }

        private static (DatabaseEngineEdition Edition, string ConnectionID) GetInstance(int instanceID)
        {
            lock (Lock)
            {
                var source = CommonData.Instances;
                if (!ReferenceEquals(source, _instancesSource))
                {
                    _instances = new Dictionary<int, (DatabaseEngineEdition, string)>();
                    if (source != null)
                    {
                        foreach (DataRow row in source.Rows)
                        {
                            var id = (int)row["InstanceID"];
                            DatabaseEngineEdition edition;
                            try { edition = (DatabaseEngineEdition)Convert.ToInt32(row["EngineEdition"]); } catch { edition = DatabaseEngineEdition.Unknown; }
                            _instances[id] = (edition, Convert.ToString(row["ConnectionID"]));
                        }
                    }
                    _instancesSource = source;
                }
                return _instances.TryGetValue(instanceID, out var info) ? info : (DatabaseEngineEdition.Unknown, null);
            }
        }

        public static DatabaseEngineEdition GetEngineEdition(int instanceID) => GetInstance(instanceID).Edition;

        public static string GetConnectionID(int instanceID) => GetInstance(instanceID).ConnectionID;

        public static IReadOnlyList<ReportTag> GetTags(int instanceID)
        {
            lock (Lock)
            {
                if (_tags == null || _tagsConnection != Common.ConnectionGUID)
                {
                    _tags = LoadTags();
                    _tagsConnection = Common.ConnectionGUID;
                }
                return _tags.TryGetValue(instanceID, out var tags) ? tags : Array.Empty<ReportTag>();
            }
        }

        private static Dictionary<int, List<ReportTag>> LoadTags()
        {
            var tags = new Dictionary<int, List<ReportTag>>();
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.InstanceTagValues_Get", cn) { CommandType = CommandType.StoredProcedure };
            cn.Open();
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                var id = rdr.GetInt32(0);
                if (!tags.TryGetValue(id, out var list))
                {
                    list = new List<ReportTag>();
                    tags[id] = list;
                }
                list.Add(new ReportTag { TagName = rdr.GetString(1), TagValue = rdr.GetString(2) });
            }
            return tags;
        }
    }

    internal static class ReportVisibility
    {
        public static CustomReport.InstanceApplicability GetInstanceType(DatabaseEngineEdition edition) => edition switch
        {
            DatabaseEngineEdition.SqlDatabase => CustomReport.InstanceApplicability.AzureSQLDB,
            DatabaseEngineEdition.SqlManagedInstance or DatabaseEngineEdition.SqlAzureArcManagedInstance => CustomReport.InstanceApplicability.ManagedInstance,
            _ => CustomReport.InstanceApplicability.Regular
        };

        /// <summary>
        /// Rules are applied in order:
        /// 1. The instance type must be one the report applies to.
        /// 2. An excluded instance never shows the report.
        /// 3. If tags or included instances are specified, the instance must be in the included list or match the tags.
        ///    Tags match the same way as the tag filter in the main window: all tag names must match, with any of the
        ///    selected values for each name.
        /// </summary>
        public static bool AppliesTo(CustomReport.InstanceApplicability appliesTo, IReadOnlyCollection<ReportTag> visibleTags,
            IReadOnlyCollection<string> includeConnectionIDs, IReadOnlyCollection<string> excludeConnectionIDs,
            DatabaseEngineEdition edition, string connectionID, Func<IReadOnlyList<ReportTag>> getInstanceTags)
        {
            if ((appliesTo & GetInstanceType(edition)) == 0) return false;

            if (connectionID != null && excludeConnectionIDs?.Contains(connectionID, StringComparer.OrdinalIgnoreCase) == true) return false;

            var hasTags = visibleTags?.Count > 0;
            var hasIncludes = includeConnectionIDs?.Count > 0;
            if (!hasTags && !hasIncludes) return true;

            if (hasIncludes && connectionID != null && includeConnectionIDs.Contains(connectionID, StringComparer.OrdinalIgnoreCase)) return true;

            if (!hasTags) return false;

            var instanceTags = getInstanceTags();
            return visibleTags.GroupBy(t => t.TagName, StringComparer.OrdinalIgnoreCase)
                .All(g => g.Any(required => instanceTags.Any(t =>
                    string.Equals(t.TagName, required.TagName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(t.TagValue, required.TagValue, StringComparison.OrdinalIgnoreCase))));
        }
    }
}
