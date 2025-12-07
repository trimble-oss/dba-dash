using DBADash;
using DBADashGUI.Theme;
using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace DBADashServiceConfig
{
    public partial class TimeoutConfig : Form
    {
        public TimeoutConfig()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        private readonly CollectionCommandTimeout.CommandTimeoutSettings defaultTimeouts = new();

        private DataTable customTimeoutsDT;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int? ImportTimeout
        {
            get =>
                chkImportTimeout.Checked && int.TryParse(txtImportCommandTimeout.Text, out var timeout)
                    ? timeout
                    : null;

            set
            {
                txtImportCommandTimeout.Text = value?.ToString() ?? CollectionConfig.DefaultImportCommandTimeout.ToString();
                chkImportTimeout.Checked = value.HasValue;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int? PurgeTimeout
        {
            get =>
                chkPurgeTimeout.Checked && int.TryParse(txtPurgeDataCommandTimeout.Text, out var timeout)
                    ? timeout
                    : null;

            set
            {
                txtPurgeDataCommandTimeout.Text = value?.ToString() ?? CollectionConfig.DefaultPurgeDataCommandTimeout.ToString();
                chkPurgeTimeout.Checked = value.HasValue;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int? AddPartitionsTimeout
        {
            get =>
              chkAddPartitionsTimeout.Checked && int.TryParse(txtAddPartitionsCommandTimeout.Text, out var timeout)
                    ? timeout
                    : null;

            set
            {
                txtAddPartitionsCommandTimeout.Text = value?.ToString() ?? CollectionConfig.DefaultAddPartitionsCommandTimeout.ToString();
                chkAddPartitionsTimeout.Checked = value.HasValue;
            }
        }

        private void TimeoutConfig_Load(object sender, EventArgs e)
        {
            var customTimeouts = CollectionCommandTimeout.GetCustomTimeouts();
            customTimeoutsDT = customTimeouts.CollectionCommandTimeoutsAsDataTable();
            chkDefaultTimeout.Checked = customTimeouts.DefaultCommandTimeout.HasValue;
            txtDefaultTimeout.Text =
                (customTimeouts.DefaultCommandTimeout ?? defaultTimeouts.DefaultCommandTimeout).ToString();
            txtDefaultTimeout.Enabled = customTimeouts.DefaultCommandTimeout.HasValue;
            cboCollection.DataSource = Enum.GetValues(typeof(CollectionType)).Cast<CollectionType>().OrderBy(t => t.ToString()).ToList();
            dgv.DataSource = customTimeoutsDT;
            dgv.AutoResizeColumns();
        }

        private void AllowOnlyDigits_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow digits (0-9)
            // Allow control keys (like Backspace, Delete, Arrows)
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true; // Block the character
            }
        }

        private void Collection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboCollection.SelectedValue is null) return;
            txtTimeout.Text = defaultTimeouts.CollectionCommandTimeouts.TryGetValue(
                (CollectionType)cboCollection.SelectedValue,
                out var timeout)
                ? timeout.ToString()
                : defaultTimeouts.DefaultCommandTimeout.ToString();
        }

        private void Add_Click(object sender, EventArgs e)
        {
            try
            {
                if (cboCollection.SelectedValue is null)
                {
                    throw new Exception("Selected collection is null");
                }

                var collectionType = (CollectionType)cboCollection.SelectedValue;
                if (int.TryParse(txtTimeout.Text, out var timeout))
                {
                    var row = customTimeoutsDT.Rows.Cast<DataRow>()
                        .FirstOrDefault(r => Enum.Parse<CollectionType>(r["CollectionType"].ToString()!) == collectionType);
                    if (row != null)
                    {
                        row["Timeout"] = timeout;
                    }
                    else
                    {
                        customTimeoutsDT.Rows.Add(new[] { cboCollection.SelectedValue, txtTimeout.Text });
                        dgv.AutoResizeColumns();
                    }
                }
                else
                {
                    CommonShared.ShowExceptionDialog("Invalid timeout",
                        $"'{txtTimeout.Text}' could not be converted to an integer", "");
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error adding custom timeout");
            }
        }

        private void Save_Click(object sender, EventArgs e)
        {
            try
            {
                var customSettings = GetCustomSettings();
                customSettings.Save();
                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error saving custom timeouts");
            }
        }

        private CollectionCommandTimeout.CommandTimeoutSettingsBase GetCustomSettings()
        {
            if (!int.TryParse(txtDefaultTimeout.Text, out var timeout))
            {
                throw new Exception($"Invalid timeout. '{txtTimeout.Text}' could not be converted to an integer");
            }

            var customSettings = new CollectionCommandTimeout.CommandTimeoutSettingsBase();
            customSettings.SetFromDataTable(customTimeoutsDT);
            customSettings.DefaultCommandTimeout = chkDefaultTimeout.Checked ? timeout : null;
            return customSettings;
        }

        private CollectionCommandTimeout.CommandTimeoutSettingsBase GetTimeoutResult()
        {
            var custom = GetCustomSettings();
            var timeouts = new CollectionCommandTimeout.CommandTimeoutSettingsBase();
            timeouts.DefaultCommandTimeout = custom.DefaultCommandTimeout ?? defaultTimeouts.DefaultCommandTimeout;
            // Start with custom timeouts as a base
            timeouts.CollectionCommandTimeouts = custom.CollectionCommandTimeouts;
            // Add app default timeouts for collections with a custom timeout
            foreach (var timeout in defaultTimeouts.CollectionCommandTimeouts.Where(timeout =>
                         !timeouts.CollectionCommandTimeouts.ContainsKey(timeout.Key)))
            {
                timeouts.CollectionCommandTimeouts.Add(timeout.Key, timeout.Value);
            }
            // Add timeouts for collections without a defined timeout
            foreach (var collectionType in Enum.GetValues<CollectionType>())
            {
                if (!timeouts.CollectionCommandTimeouts.ContainsKey(collectionType))
                {
                    timeouts.CollectionCommandTimeouts.Add(collectionType,
                        timeouts.DefaultCommandTimeout.Value);
                }
            }
            return timeouts;
        }

        private void DefaultTimeout_CheckChanged(object sender, EventArgs e)
        {
            txtDefaultTimeout.Enabled = chkDefaultTimeout.Checked;
            if (!chkDefaultTimeout.Checked)
            {
                txtDefaultTimeout.Text = defaultTimeouts.DefaultCommandTimeout.ToString();
            }
        }

        private void CustomTimeouts_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex != colDelete.Index) return;
            dgv.Rows.RemoveAt(e.RowIndex);
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }

        private void Preview_Click(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                var frm = new Form() { Width = this.Width, Height = this.Height, Text = "Effective Timeouts", Icon = this.Icon };
                var grid = new DataGridView() { Dock = DockStyle.Fill, ReadOnly = true, RowHeadersVisible = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false };
                grid.DataSource = new DataView(GetTimeoutResult().CollectionCommandTimeoutsAsDataTable(), null, "CollectionType", DataViewRowState.CurrentRows);
                frm.Controls.Add(grid);
                frm.ApplyTheme();
                frm.Load += (_, _) => { grid.AutoResizeColumns(); };
                frm.ShowSingleInstance();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex);
            }
        }

        private void ImportTimeout_CheckChanged(object sender, EventArgs e)
        {
            txtImportCommandTimeout.Enabled = chkImportTimeout.Checked;
            if (!chkImportTimeout.Checked)
            {
                txtImportCommandTimeout.Text = CollectionConfig.DefaultImportCommandTimeout.ToString();
            }
        }

        private void PurgeTimeout_CheckChanged(object sender, EventArgs e)
        {
            txtPurgeDataCommandTimeout.Enabled = chkPurgeTimeout.Checked;
            if (!chkPurgeTimeout.Checked)
            {
                txtPurgeDataCommandTimeout.Text = CollectionConfig.DefaultPurgeDataCommandTimeout.ToString();
            }
        }

        private void AddPartitionsTimeout_CheckChanged(object sender, EventArgs e)
        {
            txtAddPartitionsCommandTimeout.Enabled = chkAddPartitionsTimeout.Checked;
            if (!chkAddPartitionsTimeout.Checked)
            {
                txtAddPartitionsCommandTimeout.Text = CollectionConfig.DefaultAddPartitionsCommandTimeout.ToString();
            }
        }
    }
}