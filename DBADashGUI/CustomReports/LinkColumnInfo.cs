using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static DBADashGUI.SchemaCompare.CodeEditor;

namespace DBADashGUI.CustomReports
{
    public abstract class LinkColumnInfo
    {
        public abstract void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender);
    }

    public class PlaceholderLinkInfo : LinkColumnInfo
    {
        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            // Do nothing. The link click is handled elsewhere
        }
    }

    public class UrlLinkColumnInfo : LinkColumnInfo
    {
        public string TargetColumn { get; set; }

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var url = row.Cells[TargetColumn].Value.DBNullToNull().ToString() ?? string.Empty;
            try
            {
                if (url.StartsWith("smb:")) // Convert to UNC path
                {
                    url = url.Replace("smb:", "").Replace("/", @"\");
                }
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.IsFile)
                {
                    var filePath = uri.LocalPath; // Converts file:// paths
                    CommonShared.OpenFolder(filePath);
                }
                else if (CommonShared.IsValidUrl(url))
                {
                    CommonShared.OpenURL(url);
                }
                else
                {
                    MessageBox.Show($"Invalid URL: {url}", "Invalid URL", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
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

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var text = row.Cells[TargetColumn].Value.DBNullToNull() as string;
            if (string.IsNullOrEmpty(text)) return;
            Common.ShowCodeViewer(text, row.Cells[TargetColumn].OwningColumn.HeaderText, TextHandling);
        }
    }

    public class QueryPlanLinkColumnInfo : LinkColumnInfo
    {
        public string TargetColumn { get; set; }

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
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

    public class DeadlockGraphLinkColumnInfo : LinkColumnInfo
    {
        public string TargetColumn { get; set; }

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var dlGraph = row.Cells[TargetColumn].Value.DBNullToNull() as string;
            if (!Common.IsValidDeadlockGraph(dlGraph))
            {
                MessageBox.Show($"Invalid deadlock graph\n{dlGraph}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Common.ShowDeadlockGraph(dlGraph);
        }
    }

    public class DrillDownLinkColumnInfo : LinkColumnInfo
    {
        public string ReportProcedureName { get; set; }

        public Dictionary<string, string> ColumnToParameterMap { get; set; } = new();

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
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
            CustomReportViewer customReportViewer = new() { Context = newContext, CustomParams = customParams };
            customReportViewer.ShowSingleInstance();
        }
    }

    public class NavigateTreeLinkColumnInfo : LinkColumnInfo
    {
        public string InstanceColumn { get; set; }
        public string DatabaseColumn { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Main.Tabs Tab { get; set; }

        public override void Navigate(DBADashContext context, DataGridViewRow row, int selectedTableIndex, ContainerControl sender)
        {
            var main = Main.MainFormInstance;
            if (main == null) return;
            var ownerSet = false;
            if (sender.ParentForm != main && sender.ParentForm is { Owner: null }) // Setting the owner keeps the window on top of the main form
            {
                sender.ParentForm.Owner = main;
                ownerSet = true;
            }

            var instanceId = row.Cells[InstanceColumn].Value.DBNullToNull() as int?;
            var instanceName = row.Cells[InstanceColumn].Value.DBNullToNull() as string;
            var args = new Main.InstanceSelectedEventArgs()
            {
                InstanceID = instanceId ?? 0,
                Instance = instanceName,
                Database = string.IsNullOrEmpty(DatabaseColumn) ? null : row.Cells[DatabaseColumn].Value.DBNullToNull() as string,
                Tab = Tab,
                SearchFromRoot = true
            };
            main.Instance_Selected(sender, args);
            Application.DoEvents(); // Complete actions that might be triggered by Instance_Selected before we unset the owner
            if (ownerSet) // Undo setting the owner
            {
                sender.ParentForm.Owner = null;
            }
        }
    }
}