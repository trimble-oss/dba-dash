using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;

namespace DBADashGUI.CollectionDates
{
    public partial class CollectionDatesThresholds : Form
    {
        public CollectionDatesThresholds()
        {
            InitializeComponent();
            this.ApplyTheme();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int InstanceID { get; set; }

        private string _reference;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Reference
        {
            get => _reference;
            set => _reference = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Inherit
        {
            get => optInherit.Checked; set => optInherit.Checked = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int WarningThreshold
        {
            get => (int)numWarning.Value; set => numWarning.Value = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CriticalThreshold
        {
            get => (int)numCritical.Value; set => numCritical.Value = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Disabled
        {
            get => OptDisabled.Checked; set => OptDisabled.Checked = value;
        }

        /// <summary>
        /// True when thresholds are computed from the collection schedule (interval x multiplier + buffer)
        /// rather than explicitly configured minute values.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Scheduled
        {
            get => optSchedule.Checked; set => optSchedule.Checked = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public decimal WarningMultiplier
        {
            get => numWarningMultiplier.Value; set => numWarningMultiplier.Value = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public decimal CriticalMultiplier
        {
            get => numCriticalMultiplier.Value; set => numCriticalMultiplier.Value = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public decimal WarningBufferMinutes
        {
            get => numWarningBuffer.Value; set => numWarningBuffer.Value = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public decimal CriticalBufferMinutes
        {
            get => numCriticalBuffer.Value; set => numCriticalBuffer.Value = value;
        }

        // What the inherited (fallback) config actually is, so that when Inherit/Default is checked the
        // dialog can still show the right read-only panel (the values loaded above are already the
        // inherited ones) instead of always defaulting to the schedule panel.
        private bool _inheritedIsExplicit;

        private bool _inheritedIsDisabled;

        // App-shipped defaults, not user-configurable (instance/root level already provides the supported
        // way to override). Keep these in sync with dbo.CollectionDatesStatus's Auto CROSS APPLY.
        public const decimal DefaultWarningMultiplier = 2.0m;

        public const decimal DefaultCriticalMultiplier = 4.0m;
        public const decimal DefaultWarningBufferMinutes = 3.0m;
        public const decimal DefaultCriticalBufferMinutes = 6.0m;

        private void GetThreshold()
        {
            // Match the documented app-shipped defaults (also used by dbo.CollectionDatesStatus's Auto
            // CROSS APPLY) so a never-configured reference saves the same values it would otherwise
            // have gotten automatically, rather than whatever the designer happened to set.
            WarningMultiplier = DefaultWarningMultiplier;
            CriticalMultiplier = DefaultCriticalMultiplier;
            WarningBufferMinutes = DefaultWarningBufferMinutes;
            CriticalBufferMinutes = DefaultCriticalBufferMinutes;

            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("CollectionDatesThresholds_Get", cn) { CommandType = CommandType.StoredProcedure };

            cn.Open();
            cmd.Parameters.AddWithValue("InstanceID", InstanceID);
            using var rdr = cmd.ExecuteReader();
            var referenceFound = false;
            while (rdr.Read())
            {
                var reference = Convert.ToString(rdr["Reference"]);

                if (reference == _reference)
                {
                    referenceFound = true;
                    chkReferences.Items.Add(reference!, CheckState.Checked);
                    _inheritedIsExplicit = rdr["WarningThreshold"] != DBNull.Value && rdr["CriticalThreshold"] != DBNull.Value;
                    _inheritedIsDisabled = Convert.ToBoolean(rdr["Disabled"]);
                    if (_inheritedIsExplicit)
                    {
                        WarningThreshold = (int)rdr["WarningThreshold"];
                        CriticalThreshold = (int)rdr["CriticalThreshold"];
                        optExplicit.Checked = true;
                    }
                    else if (_inheritedIsDisabled)
                    {
                        OptDisabled.Checked = true;
                    }
                    else
                    {
                        optSchedule.Checked = true;
                    }
                    WarningMultiplier = rdr["WarningMultiplier"] == DBNull.Value ? DefaultWarningMultiplier : (decimal)rdr["WarningMultiplier"];
                    CriticalMultiplier = rdr["CriticalMultiplier"] == DBNull.Value ? DefaultCriticalMultiplier : (decimal)rdr["CriticalMultiplier"];
                    WarningBufferMinutes = rdr["WarningBufferMinutes"] == DBNull.Value ? DefaultWarningBufferMinutes : (decimal)rdr["WarningBufferMinutes"];
                    CriticalBufferMinutes = rdr["CriticalBufferMinutes"] == DBNull.Value ? DefaultCriticalBufferMinutes : (decimal)rdr["CriticalBufferMinutes"];
                    if (Convert.ToBoolean(rdr["Inherited"]))
                    {
                        optInherit.Checked = true;
                    }
                }
                else
                {
                    chkReferences.Items.Add(reference!, CheckState.Unchecked);
                }
            }

            if (!referenceFound && !string.IsNullOrEmpty(_reference))
            {
                // No threshold row exists yet for this reference at any level (root or instance) - add it
                // as a checked entry anyway so it can still be saved. The dialog's defaults (Scheduled,
                // default multiplier/buffer) apply until the user changes them.
                chkReferences.Items.Add(_reference, CheckState.Checked);
            }

            UpdatePanelVisibility();
        }

        private void BttnUpdate_Click(object sender, EventArgs e)
        {
            foreach (string itm in chkReferences.CheckedItems)
            {
                Update(itm);
            }
        }

        private void Update(string reference)
        {
            using (var cn = new SqlConnection(Common.ConnectionString))
            using (var cmd = new SqlCommand("CollectionDatesThresholds_Upd", cn) { CommandType = CommandType.StoredProcedure })
            {
                cn.Open();

                cmd.Parameters.AddWithValue("InstanceID", InstanceID);
                cmd.Parameters.AddWithValue("Reference", reference);
                cmd.Parameters.AddWithValue("Inherit", Inherit);

                if (Inherit)
                {
                    // Remove any override at this level (the stored proc deletes the row when @Inherit = 1).
                    // Warning/Critical params are still required by the proc signature, so pass NULLs.
                    cmd.Parameters.AddWithValue("WarningThreshold", DBNull.Value);
                    cmd.Parameters.AddWithValue("CriticalThreshold", DBNull.Value);
                    cmd.Parameters.AddWithValue("WarningMultiplier", DBNull.Value);
                    cmd.Parameters.AddWithValue("CriticalMultiplier", DBNull.Value);
                    cmd.Parameters.AddWithValue("WarningBufferMinutes", DBNull.Value);
                    cmd.Parameters.AddWithValue("CriticalBufferMinutes", DBNull.Value);
                    cmd.Parameters.AddWithValue("Disabled", false);
                }
                else
                {
                    if (optExplicit.Checked)
                    {
                        cmd.Parameters.AddWithValue("WarningThreshold", WarningThreshold);
                        cmd.Parameters.AddWithValue("CriticalThreshold", CriticalThreshold);
                    }
                    else
                    {
                        cmd.Parameters.AddWithValue("WarningThreshold", DBNull.Value);
                        cmd.Parameters.AddWithValue("CriticalThreshold", DBNull.Value);
                    }
                    cmd.Parameters.AddWithValue("WarningMultiplier", WarningMultiplier);
                    cmd.Parameters.AddWithValue("CriticalMultiplier", CriticalMultiplier);
                    cmd.Parameters.AddWithValue("WarningBufferMinutes", WarningBufferMinutes);
                    cmd.Parameters.AddWithValue("CriticalBufferMinutes", CriticalBufferMinutes);
                    cmd.Parameters.AddWithValue("Disabled", Disabled);
                }
                cmd.ExecuteNonQuery();
                DialogResult = DialogResult.OK;
            }
        }

        private void CollectionDatesThresholds_Load(object sender, EventArgs e)
        {
            chkCheckAll.Enabled = true;
            if (InstanceID == -1)
            {
                Text += " (Root)";
                // At root there's no parent level to inherit from - this removes the override entirely,
                // reverting to the built-in system default (schedule-based, default multiplier/buffer).
                optInherit.Text = "Default";
            }
            else
            {
                Text += " (Instance)";
                optInherit.Text = "Inherit";
            }
            GetThreshold();
        }

        private void OptInherit_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePanelVisibility();
        }

        private void OptDisabled_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePanelVisibility();
        }

        private void OptExplicit_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePanelVisibility();
        }

        private void OptSchedule_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePanelVisibility();
        }

        private void UpdatePanelVisibility()
        {
            // While Inherit/Default is checked there's no local mode of its own - show whichever panel
            // matches what's actually being inherited (already loaded into the fields by GetThreshold),
            // read-only, so the dialog reflects the effective config rather than always defaulting to
            // the schedule panel.
            var showExplicit = optExplicit.Checked || (optInherit.Checked && _inheritedIsExplicit);
            var showSchedule = optSchedule.Checked || (optInherit.Checked && !_inheritedIsExplicit && !_inheritedIsDisabled);

            pnlThresholds.Visible = showExplicit;
            pnlThresholds.Enabled = optExplicit.Checked;
            pnlSchedule.Visible = showSchedule;
            pnlSchedule.Enabled = optSchedule.Checked;
        }

        private void BttnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void ChkCheckAll_CheckedChanged(object sender, EventArgs e)
        {
            for (var i = 0; i < chkReferences.Items.Count; i++)
            {
                chkReferences.SetItemChecked(i, chkCheckAll.Checked);
            }
        }
    }
}