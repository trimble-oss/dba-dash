using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DBADash;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DBADashGUI.CustomReports
{
    public class CustomReport
    {
        [JsonIgnore]
        public string SchemaName { get; set; }

        [JsonIgnore]
        public string ProcedureName { get; set; }

        public string ReportVisibilityRole { get; set; } = "public";

        [JsonIgnore]
        public string QualifiedProcedureName { get; set; }

        public string ReportName { get; set; }

        public string Description { get; set; }

        public string URL { get; set; }

        public List<string> TriggerCollectionTypes { get; set; } = new();

        [JsonIgnore]
        public Params Params { get; set; }

        [JsonIgnore]
        public Exception DeserializationException = null;

        public static readonly string[] SystemParamNames = new[] { "@INSTANCEIDS", "@INSTANCEID", "@DATABASEID", "@FROMDATE", "@TODATE", "@OBJECTID" };

        /// <summary>
        /// Parameters for the stored procedure that won't be supplied automatically based on context
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Param> UserParams => Params == null ? new List<Param>() : Params.ParamList.Where(p =>
                                                                                                                                                                                                                            !SystemParamNames.Contains(p.ParamName.ToUpper()));

        /// <summary>
        /// Parameters for the stored procedure that are supplied automatically based on context
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Param> SystemParams => Params == null ? new List<Param>() : Params.ParamList.Where(p =>
                                                                                                                            SystemParamNames.Contains(p.ParamName.ToUpper()));

        [JsonIgnore]
        public bool IsRootLevel => Params != null && Params.ParamList.Any(p => p.ParamName.ToUpper() == "@INSTANCEIDS");

        [JsonIgnore]
        public bool IsDatabaseLevel => Params != null && Params.ParamList.Any(p => p.ParamName.ToUpper() == "@DATABASEID");

        [JsonIgnore]
        public bool IsInstanceLevel => Params != null && Params.ParamList.Any(p => (new[] { "@INSTANCEIDS", "@INSTANCEID" }).Contains(p.ParamName.ToUpper()));

        [JsonIgnore]
        public bool CanEditReport { get; set; }

        public Dictionary<int, CustomReportResult> CustomReportResults { get; set; } = new();

        /// <summary>
        /// If report has @FromDate & @ToDate parameters, the global date/time filter should be visible and date range supplied to the report
        /// </summary>
        [JsonIgnore]
        public bool TimeFilterSupported => Params.ParamList.Any(p =>
                                                                                                                                                                                                                            p.ParamName.Equals("@FromDate", StringComparison.CurrentCultureIgnoreCase) ||
                                                                                                                                                                                                                            p.ParamName.Equals("@ToDate", StringComparison.CurrentCultureIgnoreCase));

        /// <summary>
        /// Save customizations
        /// </summary>
        public void Update()
        {
            var meta = Serialize();
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.CustomReport_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("ProcedureName", ProcedureName);
            cmd.Parameters.AddWithValue("SchemaName", SchemaName);
            cmd.Parameters.AddWithValue("MetaData", meta);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        public string Serialize() => JsonConvert.SerializeObject(this, Formatting.Indented,
            new JsonSerializerSettings
            { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore, TypeNameHandling = TypeNameHandling.Auto, SerializationBinder = new SimpleBinder() });

        /// <summary>
        /// Convert list of parameters for the report to list of CustomSqlParameters
        /// </summary>
        /// <returns></returns>
        public List<CustomSqlParameter> GetCustomSqlParameters() => Params?.ParamList.Select(p => p.CreateParameter()).ToList() ?? new();

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
            var currentNamespace = GetType().Namespace;
            var type = currentAssembly.GetType($"{currentNamespace}.{typeName}") ?? base.BindToType(assemblyName, typeName);
            return type;
        }
    }
}