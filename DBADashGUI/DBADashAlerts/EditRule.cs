using DBADashGUI.DBADashAlerts.Rules;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashGUI.DBADashAlerts
{
    public partial class EditRule : Form
    {
        public EditRule()
        {
            InitializeComponent();
            cboType.DataSource = Enum.GetNames(typeof(AlertRuleBase.RuleTypes)).Union<string>(new string[] { string.Empty }).OrderBy(s => s).ToList();
            this.ApplyTheme();
        }

        public string ConnectionID { get; set; }

        public AlertRuleBase AlertRule { get; set; }

        private void EditRule_Load(object sender, EventArgs e)
        {
            cboType.Text = AlertRule?.RuleType.ToString() ?? string.Empty;
            if (AlertRule != null)
            {
                propertyGrid1.SelectedObject = AlertRule;
                cboType.Enabled = AlertRule.RuleID == null;
            }

            this.Text = AlertRule?.RuleID == null ? @"Add Rule" : "Edit Rule";

            cboType.SelectedIndexChanged += cboType_SelectedIndexChanged;
        }

        private void cboType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (AlertRule?.RuleID > 0) return;
            if (string.IsNullOrEmpty(cboType.Text)) return;
            var ruleType = Enum.Parse<AlertRuleBase.RuleTypes>(cboType.Text, true);
            AlertRule = AlertRuleBase.CreateRule(ruleType);
            try
            {
                AlertRule.ApplyToInstance = ConnectionID;
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }

            propertyGrid1.SelectedObject = AlertRule;
        }

        private void bttnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void bttnSave_Click(object sender, EventArgs e)
        {
            try
            {
                AlertRule.Save();
                this.DialogResult = DialogResult.OK;
            }
            catch (SqlException ex) when (ex.Number == 2601)
            {
                CommonShared.ShowExceptionDialog(ex, "A duplicate rule exists");
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }
    }
}