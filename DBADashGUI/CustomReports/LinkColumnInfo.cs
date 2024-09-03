using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using static DBADashGUI.SchemaCompare.CodeEditor;

namespace DBADashGUI.CustomReports
{
    public abstract class LinkColumnInfo
    {
        public abstract void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex);
    }

    public class UrlLinkColumnInfo : LinkColumnInfo
    {
        public string TargetColumn { get; set; }

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex)
        {
            var url = row.Cells[TargetColumn].Value.DBNullToNull().ToString() ?? string.Empty;
            if (CommonShared.IsValidUrl(url))
            {
                CommonShared.OpenURL(url);
            }
            else
            {
                MessageBox.Show($"Invalid URL: {url}", "Invalid URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    public class TextLinkColumnInfo : LinkColumnInfo
    {
        public string TargetColumn { get; set; }

        [JsonIgnore]
        public CodeEditorModes TextHandling { get; set; } = CodeEditorModes.None;

        [JsonProperty(nameof(TextHandling))]
        public string TextHandlingString
        {
            get => TextHandling.ToString();
            set
            {
                if (Enum.TryParse<CodeEditorModes>(value, out var mode))
                {
                    TextHandling = mode;
                }
            }
        }

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex)
        {
            var text = row.Cells[TargetColumn].Value.DBNullToNull() as string;
            if (string.IsNullOrEmpty(text)) return;
            Common.ShowCodeViewer(text, row.Cells[TargetColumn].OwningColumn.HeaderText, TextHandling);
        }
    }

    public class QueryPlanLinkColumnInfo : LinkColumnInfo
    {
        public string TargetColumn { get; set; }

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex)
        {
            var queryPlan = row.Cells[TargetColumn].Value.DBNullToNull() as string;
            if (!Common.IsValidExecutionPlan(queryPlan))
            {
                MessageBox.Show($"Invalid execution plan\n{queryPlan}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Common.ShowQueryPlan(queryPlan);
        }
    }

    public class DrillDownLinkColumnInfo : LinkColumnInfo
    {
        public string ReportProcedureName { get; set; }

        public Dictionary<string, string> ColumnToParameterMap { get; set; } = new();

        private CustomReportViewer customReportViewer;

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex)
        {
            customReportViewer?.Close();
            var report = context.Report is SystemReport ? CustomReports.SystemReports.FirstOrDefault(r => r.ProcedureName == ReportProcedureName) : CustomReports.GetCustomReports().FirstOrDefault(r => r.ProcedureName == ReportProcedureName);

            if (report == null) return;
            var newContext = (DBADashContext)context.Clone();
            newContext.Report = report;
            var customParams = report.GetCustomSqlParameters();
            foreach (var mapping in ColumnToParameterMap)
            {
                var param = customParams.FirstOrDefault(p => p.Param.ParameterName == mapping.Key);
                if (param == null) continue;
                param.UseDefaultValue = false;
                var value = row.Cells[mapping.Value].Value;
                if (row.Cells[mapping.Value].ValueType == typeof(DateTime) && !context.Report.CustomReportResults[selectedTableIndex].DoNotConvertToLocalTimeZone.Contains(mapping.Value))
                {
                    value = ((DateTime)value).AppTimeZoneToUtc();
                }
                if (string.Equals(mapping.Key, "@INSTANCEIDS", StringComparison.OrdinalIgnoreCase) && value is int intValue)
                {
                    value = (new HashSet<int>() { intValue }).AsDataTable();
                }
                param.Param.Value = value;
            }
            customReportViewer = new CustomReportViewer() { Context = newContext, CustomParams = customParams };
            customReportViewer.FormClosed += (s, e) => customReportViewer = null;
            customReportViewer.Show();
        }
    }
}