using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace DBADashGUI.Checks
{
    /// <summary>
    /// Configure Service Broker thresholds at root, instance, database or queue level.  Each level inherits from the one above unless
    /// configured.  Transmission queue and conversation thresholds are per database, so they can't be set for a queue.
    /// </summary>
    public class ServiceBrokerThresholdConfig : Form
    {
        private readonly Label lblObject = new() { AutoSize = true, Font = new Font(DefaultFont, FontStyle.Bold), Margin = new Padding(3, 8, 3, 8) };
        private readonly CheckBox chkInherit = new() { Text = "Inherit", AutoSize = true };
        private readonly Button bttnUp = new() { Text = "Up", AutoSize = true };
        private readonly Button bttnUpdate = new() { Text = "&Update", AutoSize = true };
        private readonly Button bttnCancel = new() { Text = "&Cancel", AutoSize = true };
        private readonly TableLayoutPanel pnlConfig = new() { ColumnCount = 3, AutoSize = true, Dock = DockStyle.Fill };
        private readonly ToolTip toolTip = new();

        private readonly ThresholdInput queueDepthWarning = new("Queue Depth Warning", "messages", 10000);
        private readonly ThresholdInput queueDepthCritical = new("Queue Depth Critical", "messages", 100000);
        private readonly ThresholdInput queueAgeWarning = new("Queue Age Warning", "mins", 15, int.MaxValue);
        private readonly ThresholdInput queueAgeCritical = new("Queue Age Critical", "mins", 60, int.MaxValue);
        private readonly ThresholdInput transmissionAgeWarning = new("Transmission Queue Age Warning", "mins", 15, int.MaxValue);
        private readonly ThresholdInput transmissionAgeCritical = new("Transmission Queue Age Critical", "mins", 60, int.MaxValue);
        private readonly ThresholdInput conversationsWarning = new("Open Conversations Warning", "conversations", 100000);
        private readonly ThresholdInput conversationsCritical = new("Open Conversations Critical", "conversations", 1000000);

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int InstanceID { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int DatabaseID { get; set; }

        /// <summary>Schema of the queue - queue names are only unique within a schema.  Empty above queue level.</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SchemaName { get; set; } = "";

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string QueueName { get; set; } = "";

        private bool IsRoot => InstanceID == -1 && DatabaseID == -1 && QueueName == "";

        public ServiceBrokerThresholdConfig()
        {
            Text = "Service Broker Thresholds";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(10);
            AcceptButton = bttnUpdate;
            CancelButton = bttnCancel;

            foreach (var input in new[] { queueDepthWarning, queueDepthCritical, queueAgeWarning, queueAgeCritical, transmissionAgeWarning, transmissionAgeCritical, conversationsWarning, conversationsCritical })
            {
                input.AddTo(pnlConfig);
            }
            toolTip.SetToolTip(queueDepthWarning.Label, "Messages waiting in a queue");
            toolTip.SetToolTip(queueAgeWarning.Label, "Age of the oldest message waiting in a queue to be received.  Shows a queue that isn't being processed.");
            toolTip.SetToolTip(transmissionAgeWarning.Label, "Age of the oldest message waiting in the transmission queue to be sent.  Messages are usually sent in seconds.");
            toolTip.SetToolTip(conversationsWarning.Label, "Conversations that are not closed.  A large number usually means conversations are not being ended.");

            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
            buttons.Controls.AddRange(new Control[] { bttnUpdate, bttnCancel });
            var footer = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
            footer.Controls.Add(chkInherit, 0, 0);
            footer.Controls.Add(buttons, 1, 0);

            var header = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            header.Controls.AddRange(new Control[] { bttnUp, lblObject });
            toolTip.SetToolTip(bttnUp, "Go up a level");

            var layout = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Dock = DockStyle.Fill };
            layout.Controls.Add(header);
            layout.Controls.Add(pnlConfig);
            layout.Controls.Add(footer);
            Controls.Add(layout);

            chkInherit.CheckedChanged += (_, _) => pnlConfig.Enabled = !chkInherit.Checked;
            bttnUp.Click += (_, _) => Up();
            bttnUpdate.Click += (_, _) => Save();
            bttnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
            Load += (_, _) => GetThresholds();

            this.ApplyTheme();
        }

        private void GetThresholds()
        {
            using SqlConnection cn = new(Common.ConnectionString);
            using SqlCommand cmd = new("dbo.ServiceBrokerThresholds_Get", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
            cmd.Parameters.AddWithValue("schema_name", SchemaName);
            cmd.Parameters.AddWithValue("queue_name", QueueName);
            cn.Open();
            using SqlDataReader rdr = cmd.ExecuteReader();
            chkInherit.Enabled = !IsRoot;
            bttnUp.Enabled = !IsRoot;
            // Transmission queue and conversations are per database
            var isQueue = QueueName != "";
            foreach (var input in new[] { transmissionAgeWarning, transmissionAgeCritical, conversationsWarning, conversationsCritical })
            {
                input.SetAvailable(!isQueue);
            }
            if (!rdr.Read())
            {
                chkInherit.Checked = chkInherit.Enabled;
                return;
            }
            lblObject.Text = rdr.GetString("SelectedObject");
            chkInherit.Checked = rdr.GetBoolean("IsInherited");
            queueDepthWarning.Value = rdr["QueueDepthWarningThreshold"].DBNullToNull() as long?;
            queueDepthCritical.Value = rdr["QueueDepthCriticalThreshold"].DBNullToNull() as long?;
            queueAgeWarning.Value = rdr["QueueAgeWarningThreshold"].DBNullToNull() as int?;
            queueAgeCritical.Value = rdr["QueueAgeCriticalThreshold"].DBNullToNull() as int?;
            transmissionAgeWarning.Value = rdr["TransmissionQueueAgeWarningThreshold"].DBNullToNull() as int?;
            transmissionAgeCritical.Value = rdr["TransmissionQueueAgeCriticalThreshold"].DBNullToNull() as int?;
            conversationsWarning.Value = rdr["ConversationCountWarningThreshold"].DBNullToNull() as long?;
            conversationsCritical.Value = rdr["ConversationCountCriticalThreshold"].DBNullToNull() as long?;
            pnlConfig.Enabled = !chkInherit.Checked;
        }

        private void UpdateThresholds()
        {
            using SqlConnection cn = new(Common.ConnectionString);
            using SqlCommand cmd = new("dbo.ServiceBrokerThresholds_Upd", cn) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            cmd.Parameters.AddWithValue("DatabaseID", DatabaseID);
            cmd.Parameters.AddWithValue("schema_name", SchemaName);
            cmd.Parameters.AddWithValue("queue_name", QueueName);
            cmd.Parameters.AddWithValue("QueueDepthWarningThreshold", (object)queueDepthWarning.Value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("QueueDepthCriticalThreshold", (object)queueDepthCritical.Value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("QueueAgeWarningThreshold", (object)(int?)queueAgeWarning.Value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("QueueAgeCriticalThreshold", (object)(int?)queueAgeCritical.Value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("TransmissionQueueAgeWarningThreshold", (object)(int?)transmissionAgeWarning.Value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("TransmissionQueueAgeCriticalThreshold", (object)(int?)transmissionAgeCritical.Value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ConversationCountWarningThreshold", (object)conversationsWarning.Value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ConversationCountCriticalThreshold", (object)conversationsCritical.Value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("Inherit", chkInherit.Checked);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        private void Save()
        {
            try
            {
                UpdateThresholds();
            }
            catch (SqlException ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error updating thresholds");
                return;
            }
            DialogResult = DialogResult.OK;
        }

        private void Up()
        {
            if (QueueName != "")
            {
                SchemaName = "";
                QueueName = "";
            }
            else if (DatabaseID != -1)
            {
                DatabaseID = -1;
            }
            else if (InstanceID != -1)
            {
                InstanceID = -1;
            }
            GetThresholds();
        }

        /// <summary>A threshold value with a checkbox to switch it on.  Unchecked is no threshold (NULL).</summary>
        private sealed class ThresholdInput
        {
            public readonly Label Label;
            private readonly NumericUpDown num = new() { Maximum = long.MaxValue, Minimum = 0, ThousandsSeparator = true, Width = 150, Enabled = false };
            private readonly CheckBox chk = new() { AutoSize = true, Text = "", Anchor = AnchorStyles.Left };
            private readonly Label lblUnit;
            private readonly long defaultValue;

            // Whether the threshold applies at this level.  Kept separately from chk.Enabled, which is also false while the panel is
            // disabled for an inherited configuration - basing num.Enabled on it left the inputs disabled after unchecking Inherit.
            private bool available = true;

            public ThresholdInput(string label, string unit, long defaultValue, long maximum = long.MaxValue)
            {
                this.defaultValue = defaultValue;
                num.Maximum = maximum;
                Label = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left };
                lblUnit = new Label { Text = unit, AutoSize = true, Anchor = AnchorStyles.Left };
                chk.CheckedChanged += (_, _) =>
                {
                    num.Enabled = available && chk.Checked;
                    if (chk.Checked && num.Value == 0) num.Value = this.defaultValue;
                };
            }

            public void AddTo(TableLayoutPanel panel)
            {
                var row = panel.RowCount++;
                var valuePanel = new FlowLayoutPanel { AutoSize = true, Margin = Padding.Empty };
                valuePanel.Controls.AddRange(new Control[] { chk, num });
                panel.Controls.Add(Label, 0, row);
                panel.Controls.Add(valuePanel, 1, row);
                panel.Controls.Add(lblUnit, 2, row);
            }

            public void SetAvailable(bool available)
            {
                this.available = available;
                Label.Enabled = available;
                chk.Enabled = available;
                lblUnit.Enabled = available;
                num.Enabled = available && chk.Checked;
            }

            public long? Value
            {
                get => chk.Checked && available ? (long)num.Value : null;
                set
                {
                    chk.Checked = value.HasValue;
                    num.Value = value ?? 0;
                }
            }
        }
    }
}
