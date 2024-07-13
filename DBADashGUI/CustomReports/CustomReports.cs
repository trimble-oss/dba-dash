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

        public static CustomReports GetCustomReports(bool forceRefresh = false)
        {
            if (connectionId != Common.ConnectionGUID || forceRefresh) // Check if connection has changed
            {
                _customReports = null;
                connectionId = Common.ConnectionGUID;
            }
            if (_customReports != null) return _customReports;

            try
            {
                _customReports = GetCustomReportsFromDb();
            }
            catch (Exception ex)
            {
                _customReports = new CustomReports();
                MessageBox.Show("Error getting custom reports:" + ex.Message, "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            return _customReports;
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