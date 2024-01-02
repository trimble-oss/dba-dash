using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using DBADashGUI.SchemaCompare;
using DBADashGUI.Theme;

namespace DBADashGUI.CustomReports
{
    public partial class LinkColumnTypeSelector : Form
    {
        public LinkColumnTypeSelector()
        {
            InitializeComponent();
            tab1.Appearance = TabAppearance.FlatButtons;
            tab1.ItemSize = new Size(0, 1);
            tab1.SizeMode = TabSizeMode.Fixed;

            BindingSource bs = new()
            {
                DataSource = Enum.GetValues(typeof(CodeEditor.CodeEditorModes))
            };
            cboLanguage.DataSource = bs;
            this.ApplyTheme();
            lblWarning.ForeColor = DashColors.Fail;
        }

        public LinkColumnInfo LinkColumnInfo { get; set; }
        public List<string> ColumnList { get; set; }

        public DBADashContext Context { get; set; }

        public string LinkColumn { get; set; }

        private void Opt_CheckedChanged(object sender, EventArgs e)
        {
            SetTab();
        }

        private void SetTab()
        {
            tab1.SelectedTab = optNone.Checked ? tabNone : optURL.Checked ? tabURL : optText.Checked ? tabText : optDrillDown.Checked ? tabDrillDown : optQueryPlan.Checked ? tabQueryPlan : null;
            cboTargetColumn.Enabled = optURL.Checked || optText.Checked || optQueryPlan.Checked;
        }

        private CustomReports customReports;

        private void LinkColumnTypeSelector_Load(object sender, EventArgs e)
        {
            customReports = CustomReports.GetCustomReports();
            cboReport.Items.Clear();
            cboReport.Items.AddRange(customReports.Select(r => r.ProcedureName).ToArray<object>());

            cboTargetColumn.Items.Clear();
            cboTargetColumn.Items.AddRange(ColumnList.ToArray<object>());
            cboReport.Text = Context.Report.ReportName;
            cboTargetColumn.Text = LinkColumn;
            txtLinkColumn.Text = LinkColumn;
            switch (LinkColumnInfo)
            {
                case null:
                    return;

                case UrlLinkColumnInfo url:
                    optURL.Checked = true;
                    cboTargetColumn.Text = url.TargetColumn;
                    SetTab();
                    break;

                case TextLinkColumnInfo text:
                    optText.Checked = true;
                    cboTargetColumn.Text = text.TargetColumn;
                    cboLanguage.SelectedItem = text.TextHandling;
                    SetTab();
                    break;

                case DrillDownLinkColumnInfo drillDown:
                    optDrillDown.Checked = true;
                    cboReport.Text = drillDown.ReportProcedureName;
                    SetTab();
                    break;

                case QueryPlanLinkColumnInfo queryPlan:
                    optQueryPlan.Checked = true;
                    cboTargetColumn.Text = queryPlan.TargetColumn;
                    SetTab();
                    break;
            }
        }

        private void BttnOK_Click(object sender, EventArgs e)
        {
            if (optURL.Checked)
            {
                LinkColumnInfo = new UrlLinkColumnInfo()
                {
                    TargetColumn = cboTargetColumn.Text
                };
            }
            else if (optText.Checked)
            {
                LinkColumnInfo = new TextLinkColumnInfo()
                {
                    TargetColumn = cboTargetColumn.Text,
                    TextHandling = (CodeEditor.CodeEditorModes)cboLanguage.SelectedItem
                };
            }
            else if (optDrillDown.Checked)
            {
                var mapping = dgvMapping.Rows.Cast<DataGridViewRow>().Where(row => !string.IsNullOrEmpty(row.Cells[2].Value as string) && row.Cells[2].Value as string != NotMapped).ToDictionary(row => row.Cells[1].Value.ToString()!, row => row.Cells[2].Value.ToString());

                LinkColumnInfo = new DrillDownLinkColumnInfo()
                {
                    ReportProcedureName = cboReport.Text,
                    ColumnToParameterMap = mapping
                };
            }
            else if (optQueryPlan.Checked)
            {
                LinkColumnInfo = new QueryPlanLinkColumnInfo()
                {
                    TargetColumn = cboTargetColumn.Text
                };
            }
            else
            {
                LinkColumnInfo = null;
            }
            this.DialogResult = DialogResult.OK;
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void CboReport_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadReportParams();
        }

        private const string NotMapped = "<Not Mapped>";

        private void LoadReportParams()
        {
            dgvMapping.Columns.Clear();
            dgvMapping.Columns.Add(new DataGridViewCheckBoxColumn() { Name = "IsSystem", HeaderText = "IsSystem" });
            dgvMapping.Columns.Add("ParamName", "Parameter Name");
            dgvMapping.Columns.Add(new DataGridViewComboBoxColumn()
            {
                HeaderText = "Column Name",
                Name = "Column Name",
                ToolTipText = "Column Name",
                DataSource = new List<object> { NotMapped }.Concat(ColumnList).ToList()
            });
            dgvMapping.Columns[0].ReadOnly = true;

            dgvMapping.Rows.Clear();
            var report = customReports.FirstOrDefault(r => r.ReportName == cboReport.Text);

            var mapping = new Dictionary<string, string>();
            if (LinkColumnInfo is DrillDownLinkColumnInfo drillDown)
            {
                mapping = drillDown.ColumnToParameterMap;
            }

            if (report?.UserParams == null) return;
            foreach (var p in report.Params.ParamList.Where(p => !string.Equals(p.ParamName, "@InstanceIDs", StringComparison.InvariantCultureIgnoreCase)))
            {
                bool isSystem = CustomReport.SystemParamNames.Contains(p.ParamName, StringComparer.OrdinalIgnoreCase);
                var row = new DataGridViewRow();
                row.CreateCells(dgvMapping);
                row.Cells[0].Value = isSystem;
                row.Cells[1].Value = p.ParamName;
                row.Cells[2].Value = mapping.ContainsKey(p.ParamName) ? mapping[p.ParamName] : NotMapped;
                row.DefaultCellStyle.Font = isSystem ? new Font(dgvMapping.DefaultCellStyle.Font, FontStyle.Italic) : new Font(dgvMapping.DefaultCellStyle.Font, FontStyle.Regular);

                dgvMapping.Rows.Add(row);
            }
        }
    }
}