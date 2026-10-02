using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace DBADashGUI.CustomReports
{
    internal class CustomReports : List<CustomReport>
    {
        public IEnumerable<CustomReport> RootLevelReports => this.Where(x => x.IsRootLevel).Union(SystemReports.Where(r => r.IsRootLevel));
        public IEnumerable<CustomReport> InstanceLevelReports => this.Where(x => x.IsInstanceLevel).Union(SystemReports.Where(r => r.IsInstanceLevel));
        public IEnumerable<CustomReport> DatabaseLevelReports => this.Where(x => x.IsDatabaseLevel).Union(SystemReports.Where(r => r.IsDatabaseLevel));

        private static CustomReports _customReports;

        private static Guid connectionId = Guid.Empty;

        public static SystemReports SystemReports { get; } = new();

        /// <summary>True if the user can organize reports into folders (db_owner or db_ddladmin).  Applies to system reports as well as user custom reports.</summary>
        public static bool CanOrganizeReports { get; private set; }

        /// <summary>Raised when a report's folder or visibility rules change so the tree can be updated.</summary>
        public static event EventHandler<CustomReport> ReportPlacementChanged;

        public static void OnReportPlacementChanged(CustomReport report) => ReportPlacementChanged?.Invoke(null, report);

        /// <summary>Distinct folder paths in use, including parent folders.</summary>
        public IEnumerable<string> FolderPaths => this.Union(SystemReports)
            .Select(r => r.Folder)
            .Where(f => !string.IsNullOrEmpty(f))
            .SelectMany(f =>
            {
                var parts = f.Split(CustomReport.FolderSeparator);
                return Enumerable.Range(1, parts.Length).Select(i => string.Join(CustomReport.FolderSeparator, parts.Take(i)));
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

        public static CustomReports GetCustomReports(bool forceRefresh = false)
        {
            if (connectionId != Common.ConnectionGUID || forceRefresh) // Check if connection has changed
            {
                _customReports = null;
                connectionId = Common.ConnectionGUID;
            }
            if (_customReports != null) return _customReports;

            ReportInstanceInfo.ClearCache();
            try
            {
                _customReports = GetCustomReportsFromDb();
            }
            catch (Exception ex)
            {
                _customReports = new CustomReports();
                CommonShared.ShowExceptionDialog(ex, "Error getting custom reports");
            }

            try
            {
                LoadFolders(_customReports);
            }
            catch (Exception ex)
            {
                // Reports are still usable without folders
                CanOrganizeReports = false;
                CommonShared.ShowExceptionDialog(ex, "Error getting report folders");
            }

            return _customReports;
        }

        /// <summary>Assign folders to user custom reports and system reports from dbo.CustomReportFolder</summary>
        private static void LoadFolders(CustomReports customReports)
        {
            var folders = new Dictionary<(string Schema, string Proc), string>();
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("dbo.CustomReportFolder_Get", cn) { CommandType = CommandType.StoredProcedure })
            {
                var pCanEdit = cmd.Parameters.Add("CanEditReport", SqlDbType.Bit);
                pCanEdit.Direction = ParameterDirection.Output;
                cn.Open();
                using (var rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        folders[(rdr.GetString(0).ToUpperInvariant(), rdr.GetString(1).ToUpperInvariant())] = CustomReport.NormalizeFolder(rdr.GetString(2));
                    }
                }
                CanOrganizeReports = pCanEdit.Value is true;
            }

            foreach (var report in customReports.Union(SystemReports))
            {
                report.Folder = folders.GetValueOrDefault((report.SchemaName.ToUpperInvariant(), report.ProcedureName?.ToUpperInvariant() ?? string.Empty));
            }
        }

        private static CustomReports GetCustomReportsFromDb()
        {
            var deserializer = new XmlSerializer(typeof(Params));
            var customReports = new CustomReports();
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.CustomReport_Get", cn)
            { CommandType = CommandType.StoredProcedure };
            cn.Open();
            var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                var strParams = rdr["Params"].ToString();
                if (string.IsNullOrEmpty(strParams)) continue;

                var stringReader = new StringReader(strParams);
                var proc = (string)rdr["ProcedureName"];
                var schema = (string)rdr["SchemaName"];
                var qualifiedName = (string)rdr["QualifiedName"];
                var meta = (string)rdr["MetaData"].DBNullToNull();
                var canEdit = (bool)rdr["CanEditReport"];
                var reportParams = (Params)deserializer.Deserialize(stringReader);
                CustomReport customReport = null;
                if (!string.IsNullOrEmpty(meta))
                {
                    try
                    {
                        customReport = JsonConvert.DeserializeObject<CustomReport>(meta, new JsonSerializerSettings() { SerializationBinder = new SimpleBinder(), TypeNameHandling = TypeNameHandling.Auto });
                    }
                    catch (Exception ex)
                    {
                        customReport ??= new CustomReport();
                        customReport.DeserializationException = ex;
                        Debug.WriteLine(ex.ToString());
                    }
                }

                customReport ??= new CustomReport();
                customReport.ReportName ??= proc;
                customReport.ProcedureName = proc;
                customReport.SchemaName = schema;
                customReport.QualifiedProcedureName = qualifiedName;
                customReport.CanEditReport = canEdit;
                customReport.Params = reportParams;
                customReports.Add(customReport);
            }

            return customReports;
        }
    }
}