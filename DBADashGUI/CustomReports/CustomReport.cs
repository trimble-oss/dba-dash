using DBADash;
using DBADashGUI.Charts;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DBADashGUI.Performance;

namespace DBADashGUI.CustomReports
{
    public class CustomReport
    {
        public enum ChartLocations
        {
            Top,
            Bottom,
            Left,
            Right
        }

        /// <summary>
        /// Indicates which instance types (by engine edition) a report is relevant to.  For example SQL Patching is not
        /// relevant to Azure SQL DB and uses <see cref="InstanceApplicability.RegularOnly"/>.
        /// </summary>
        [Flags]
        public enum InstanceApplicability
        {
            None = 0,

            /// <summary>SQL Server instances (on-premises/VM), excluding Managed Instance</summary>
            Regular = 1,

            /// <summary>Azure SQL Managed Instance (including Azure Arc)</summary>
            ManagedInstance = 2,

            /// <summary>Azure SQL DB</summary>
            AzureSQLDB = 4,

            /// <summary>Report applies to regular (non-Azure SQL DB) instances only.  Name retained for compatibility.</summary>
            RegularOnly = Regular | ManagedInstance,

            /// <summary>Report applies to Azure SQL DB instances only.  Name retained for compatibility.</summary>
            AzureOnly = AzureSQLDB,

            /// <summary>Report applies to all instances (default).</summary>
            All = Regular | ManagedInstance | AzureSQLDB
        }

        /// <summary>
        /// Instance types the report applies to.  Instance and database level reports are hidden in the tree for
        /// instances that don't apply and root level reports only receive the applicable instances in @InstanceIDs.
        /// Defaults to <see cref="InstanceApplicability.All"/>.
        /// </summary>
        [System.ComponentModel.DefaultValue(InstanceApplicability.All)]
        public InstanceApplicability AppliesTo { get; set; } = InstanceApplicability.All;

        /// <summary>
        /// Only show the report for instances with these tags.  All tag names must match, with any of the values for
        /// each name (same as the main tag filter).  Empty for no tag rule.
        /// </summary>
        public List<ReportTag> VisibleTags { get; set; } = new();

        /// <summary>Show the report for these instances (by ConnectionID) in addition to any matching <see cref="VisibleTags"/>.</summary>
        public List<string> IncludeConnectionIDs { get; set; } = new();

        /// <summary>Never show the report for these instances (by ConnectionID).</summary>
        public List<string> ExcludeConnectionIDs { get; set; } = new();

        // Keep serialized metadata tidy for reports without visibility rules
        public bool ShouldSerializeVisibleTags() => VisibleTags?.Count > 0;

        public bool ShouldSerializeIncludeConnectionIDs() => IncludeConnectionIDs?.Count > 0;

        public bool ShouldSerializeExcludeConnectionIDs() => ExcludeConnectionIDs?.Count > 0;

        /// <summary>True if the report has any rule that limits the instances it applies to.</summary>
        [JsonIgnore]
        public bool HasVisibilityRules => (AppliesTo & InstanceApplicability.All) != InstanceApplicability.All
                                          || VisibleTags?.Count > 0 || IncludeConnectionIDs?.Count > 0 || ExcludeConnectionIDs?.Count > 0;

        /// <summary>True if the report applies to the specified instance based on <see cref="AppliesTo"/>, tags and included/excluded instances.</summary>
        public bool AppliesToInstance(int instanceID)
        {
            if (!HasVisibilityRules) return true;
            return ReportVisibility.AppliesTo(AppliesTo, VisibleTags, IncludeConnectionIDs, ExcludeConnectionIDs,
                ReportInstanceInfo.GetEngineEdition(instanceID), ReportInstanceInfo.GetConnectionID(instanceID),
                () => ReportInstanceInfo.GetTags(instanceID));
        }

        /// <summary>
        /// Folder path within the Reports folder of the tree.  Nested folders are separated with "/".  Stored in
        /// dbo.CustomReportFolder rather than with the report metadata so system reports can be organized too.
        /// </summary>
        [JsonIgnore]
        public string Folder { get; set; }

        public const char FolderSeparator = '/';

        /// <summary>Normalize a folder path - trims each part and removes empty parts.  Returns null for the top level.</summary>
        public static string NormalizeFolder(string folder)
        {
            var parts = (folder ?? string.Empty).Split(FolderSeparator, '\\').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
            return parts.Length == 0 ? null : string.Join(FolderSeparator, parts);
        }

        /// <summary>Save the folder for the report.  Null or empty moves the report to the top level of the Reports folder.</summary>
        public void UpdateFolder(string folder)
        {
            folder = NormalizeFolder(folder);
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.CustomReportFolder_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("SchemaName", SchemaName);
            cmd.Parameters.AddWithValue("ProcedureName", ProcedureName);
            cmd.Parameters.AddWithValue("FolderPath", (object)folder ?? DBNull.Value);
            cn.Open();
            cmd.ExecuteNonQuery();
            Folder = folder;
        }

        public ChartLocations ChartLocation { get; set; } = ChartLocations.Top;

        public double ChartSplitPercentage { get; set; } = 0.6;

        /// <summary>
        /// Optional hint to control chart layout. When > 0 this value will be used as the maximum
        /// number of columns to arrange charts into. If 0 the automatic layout calculation is used.
        /// </summary>
        public int ChartLayoutColumns { get; set; } = 0;

        /// <summary>
        /// Optional hint to control chart layout. When > 0 this value will be used as the maximum
        /// number of rows to arrange charts into. If 0 the automatic layout calculation is used.
        /// Mutually exclusive with ChartLayoutColumns - if ChartLayoutColumns is set > 0, ChartLayoutRows will be ignored and treated as 0 (automatic).
        /// </summary>
        public int ChartLayoutRows { get => ChartLayoutColumns > 0 ? 0 : field; set; } = 0;

        [JsonIgnore]
        private string _schemaName;

        public virtual string SchemaName { get => string.IsNullOrEmpty(_schemaName) ? "dbo" : _schemaName; set => _schemaName = value; }

        [JsonIgnore]
        public virtual string ProcedureName { get; set; }

        public string ReportVisibilityRole { get; set; } = "public";

        [JsonIgnore]
        public string QualifiedProcedureName { get; set; }

        public string ReportName { get; set; }

        public string Description { get; set; }

        public string URL { get; set; }

        public List<string> TriggerCollectionTypes { get; set; } = new();

        public string CancellationMessageWarning { get; set; }

        public bool ChartVisible { get; set; } = true;

        /// <summary>
        /// Controls how multiple result sets are laid out.
        /// False (default): result sets expand to their full height and the report scrolls as a page.
        /// True: the result sets are fitted into the available height (a single page), sharing the space fairly,
        /// and only scroll internally when a result set needs more room than its share.
        /// </summary>
        public bool SinglePageLayout { get; set; }

        /// <summary>
        /// When true the report view shows a status filter (Critical / Warning / N/A / OK) in the toolbar and pushes
        /// the selected values into the @IncludeCritical / @IncludeWarning / @IncludeNA / @IncludeOK (and optional
        /// @IncludeACK) parameters before each refresh.  The filter resets on tree navigation: a single instance in
        /// context shows all statuses, multiple instances default to Critical + Warning only.  The generic Parameters
        /// button is hidden because the filter drives those parameters.
        /// </summary>
        public bool ShowStatusFilter { get; set; }

        /// <summary>
        /// When true the generic "Parameters" toolbar button is hidden.  Use for reports whose user parameters are all
        /// driven by dedicated toolbar controls (menu-bar pickers, or a control added by a derived view), so the generic
        /// editor would be redundant or misleading - e.g. a view that supplies a parameter itself in
        /// <see cref="CustomReportView.OnBeforeRefresh"/> and doesn't want the user editing it directly.
        /// </summary>
        public bool HideParametersButton { get; set; }

        private bool _tableVisible = true;

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public bool TableVisible
        {
            get => _tableVisible || !ChartVisible;
            set => _tableVisible = value;
        }

        [JsonIgnore]
        public Params Params { get; set; }

        [JsonIgnore]
        public Exception DeserializationException = null;

        public static readonly string[] SystemParamNames = new[] { "@INSTANCEIDS", "@INSTANCEID", "@DATABASEID", "@FROMDATE", "@TODATE", "@OBJECTID", "@SHOWHIDDEN" };

        private static readonly string[] InstanceLevelSystemParams = new[] { "@INSTANCEIDS", "@INSTANCEID" };

        /// <summary>
        /// Parameters for the stored procedure that won't be supplied automatically based on context
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Param> UserParams => Params?.ParamList == null ? new List<Param>() : Params.ParamList.Where(p =>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    !SystemParamNames.Contains(p.ParamName.ToUpper()));

        /// <summary>
        /// Parameters for the stored procedure that are supplied automatically based on context
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Param> SystemParams => Params?.ParamList == null ? new List<Param>() : Params.ParamList.Where(p =>
                                                                                                                                                                                                                                                                                                                                                                                                                                                                    SystemParamNames.Contains(p.ParamName.ToUpper()));

        [JsonIgnore]
        public virtual bool IsRootLevel => Params != null && Params.ParamList.Any(p => p.ParamName.Equals("@INSTANCEIDS", StringComparison.OrdinalIgnoreCase));

        [JsonIgnore]
        public virtual bool IsDatabaseLevel => Params != null && Params.ParamList.Any(p => p.ParamName.Equals("@DATABASEID", StringComparison.OrdinalIgnoreCase));

        [JsonIgnore]
        public virtual bool IsInstanceLevel => Params != null && Params.ParamList.Any(p => InstanceLevelSystemParams.Contains(p.ParamName.ToUpper()));

        [JsonIgnore]
        public bool CanEditReport { get; set; }

        public Dictionary<int, CustomReportResult> CustomReportResults { get; set; } = new();

        /// <summary>
        /// If report has @FromDate & @ToDate parameters, the global date/time filter should be visible and date range supplied to the report
        /// </summary>
        [JsonIgnore]
        public bool TimeFilterSupported => Charts?.Any(c => c.Metric != null) == true || (Params != null && Params.ParamList.Any(p =>
                                                                                                                                                p.ParamName.Equals("@FromDate", StringComparison.CurrentCultureIgnoreCase) ||
                                                                                                                                                p.ParamName.Equals("@ToDate", StringComparison.CurrentCultureIgnoreCase)));

        public bool ForceRefreshWithoutContextChange { get; set; }

        public List<CustomReportChart> Charts { get; set; } = new();

        // CustomReportChart is defined at namespace level to simplify usage across the project

        /// <summary>
        /// Associate report with a view type for rendering.  Most reports will use CustomReportView, but specialized views can be created by deriving from CustomReportView.
        /// Not serialized as user custom reports will all use CustomReportView.
        /// </summary>
        [JsonIgnore]
        public Type ViewType
        {
            get => field;
            set
            {
                var t = value ?? typeof(CustomReportView);
                if (!typeof(CustomReportView).IsAssignableFrom(t))
                {
                    throw new ArgumentException($"ViewType must derive from {nameof(CustomReportView)}. Provided: {t.FullName}");
                }
                field = t;
            }
        }

        /// <summary>
        /// Save customizations
        /// </summary>
        public void Update()
        {
            // Remove any invalid chart entries (neither MetricType nor Config) before persisting
            RemoveInvalidCharts();

            var meta = Serialize();
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.CustomReport_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("ProcedureName", ProcedureName);
            cmd.Parameters.AddWithValue("SchemaName", SchemaName);
            cmd.Parameters.AddWithValue("MetaData", meta);
            cmd.Parameters.AddWithValue("Type", GetType().Name);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        private void RemoveInvalidCharts()
        {
            if (Charts == null || Charts.Count == 0) return;
            // Identify invalid charts (no metric type and no config)
            var invalidIndexes = Charts
                .Select((c, i) => new { Chart = c, Index = i })
                .Where(x => x.Chart.Metric == null && x.Chart.Config == null)
                .Select(x => x.Index)
                .OrderByDescending(i => i)
                .ToList();

            if (invalidIndexes.Count == 0) return;

            Debug.WriteLine($"CustomReport.RemoveInvalidCharts: removing {invalidIndexes.Count} invalid chart(s)");

            foreach (var idx in invalidIndexes)
            {
                try
                {
                    Charts.RemoveAt(idx);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CustomReport.RemoveInvalidCharts: failed removing chart at index {idx}: {ex}");
                }
            }
        }

        public string Serialize() => JsonConvert.SerializeObject(this, Formatting.Indented,
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                SerializationBinder = new SimpleBinder(),
                Converters = new JsonConverter[] { new StringEnumConverter() }
            });

        /// <summary>
        /// Convert list of parameters for the report to list of CustomSqlParameters
        /// </summary>
        /// <returns></returns>
        public List<CustomSqlParameter> GetCustomSqlParameters() => Params?.ParamList?.Select(p => p.CreateParameter())?.ToList() ?? new();

        public bool HasAccess()
        {
            return DBADashUser.Roles.Contains(ReportVisibilityRole) || DBADashUser.IsAdmin;
        }

        public List<Picker> Pickers { get; set; }
    }

    public class SimpleBinder : DefaultSerializationBinder
    {
        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null; // Ignore the assembly name
            typeName = serializedType.Name; // Use only the class name without namespace
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            var currentAssembly = typeof(SimpleBinder).Assembly;

            // Try several likely locations for the type name:
            // 1. Fully qualified name (typeName may already include namespace)
            // 2. Current namespace where SimpleBinder is defined
            // 3. Charts namespace where chart configuration types (e.g., PieChartConfiguration) live
            Type type = null;

            if (!string.IsNullOrWhiteSpace(typeName))
            {
                // If caller provided a namespace-qualified name, this will succeed
                type = currentAssembly.GetType(typeName);

                if (type == null)
                {
                    var currentNamespace = GetType().Namespace;
                    type = currentAssembly.GetType($"{currentNamespace}.{typeName}");
                }

                if (type == null)
                {
                    // Chart-specific types live in the DBADashGUI.Charts namespace
                    type = currentAssembly.GetType($"DBADashGUI.Charts.{typeName}");
                }

                // Also try common namespaces where metric state/type POCOs live so callers
                // can reference types by simple name without namespace qualification.
                if (type == null)
                {
                    type = currentAssembly.GetType($"DBADashGUI.{typeName}");
                }

                if (type == null)
                {
                    type = currentAssembly.GetType($"DBADashGUI.Performance.{typeName}");
                }

                if (type == null)
                {
                    type = currentAssembly.GetType($"DBADashGUI.SavedView.{typeName}");
                }
            }

            return type ?? base.BindToType(assemblyName, typeName);
        }
    }
}