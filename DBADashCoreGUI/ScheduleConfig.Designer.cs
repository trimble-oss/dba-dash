
namespace DBADashGUI
{
    partial class ScheduleConfig
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ScheduleConfig));
            dgv = new DBADashGUI.CustomReports.DBADashDataGridView();
            bttnOK = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            lnkCron = new System.Windows.Forms.LinkLabel();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsPaste = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            tsSetDefault = new System.Windows.Forms.ToolStripButton();
            tsDisable = new System.Windows.Forms.ToolStripButton();
            panel1 = new System.Windows.Forms.Panel();
            pnlTemporary = new System.Windows.Forms.Panel();
            lnkEndTemporary = new System.Windows.Forms.LinkLabel();
            lblTemporarySchedules = new System.Windows.Forms.Label();
            dtpTemporaryUntil = new System.Windows.Forms.DateTimePicker();
            chkTemporary = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            toolStrip1.SuspendLayout();
            panel1.SuspendLayout();
            pnlTemporary.SuspendLayout();
            SuspendLayout();
            //
            // dgv
            //
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.BackgroundColor = System.Drawing.Color.White;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            dgv.Location = new System.Drawing.Point(0, 27);
            dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            dgv.Name = "dgv";
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(729, 597);
            dgv.TabIndex = 0;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.CellValueChanged += Dgv_CellValueChanged;
            dgv.RowsAdded += Dgv_RowsAdded;
            dgv.RowValidating += Dgv_RowValidating;
            dgv.SelectionChanged += Dgv_SelectionChanged;
            //
            // bttnOK
            //
            bttnOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnOK.Location = new System.Drawing.Point(644, 36);
            bttnOK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new System.Drawing.Size(75, 29);
            bttnOK.TabIndex = 2;
            bttnOK.Text = "OK";
            bttnOK.UseVisualStyleBackColor = true;
            bttnOK.Click += BttnOK_Click;
            //
            // bttnCancel
            //
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            bttnCancel.Location = new System.Drawing.Point(554, 36);
            bttnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(75, 29);
            bttnCancel.TabIndex = 3;
            bttnCancel.Text = "Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += BttnCancel_Click;
            //
            // label1
            //
            label1.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(3, 16);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(449, 20);
            label1.TabIndex = 4;
            label1.Text = "Schedule can be a period defined in seconds or a cron expression. ";
            //
            // lnkCron
            //
            lnkCron.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            lnkCron.Image = (System.Drawing.Image)resources.GetObject("lnkCron.Image");
            lnkCron.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lnkCron.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkCron.Location = new System.Drawing.Point(2, 47);
            lnkCron.Name = "lnkCron";
            lnkCron.Size = new System.Drawing.Size(68, 23);
            lnkCron.TabIndex = 5;
            lnkCron.TabStop = true;
            lnkCron.Text = "Help";
            lnkCron.TextAlign = System.Drawing.ContentAlignment.TopRight;
            lnkCron.LinkClicked += LnkCron_LinkClicked;
            //
            // toolStrip1
            //
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsCopy, tsPaste, toolStripSeparator1, tsSetDefault, tsDisable });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(729, 27);
            toolStrip1.TabIndex = 6;
            toolStrip1.Text = "toolStrip1";
            //
            // tsCopy
            //
            tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopy.Enabled = false;
            tsCopy.Image = (System.Drawing.Image)resources.GetObject("tsCopy.Image");
            tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopy.Name = "tsCopy";
            tsCopy.Size = new System.Drawing.Size(29, 24);
            tsCopy.Text = "Copy row.  Use the row header to select a single row to copy.  You can then apply the same configuration to other rows with the paste button.";
            tsCopy.Click += TsCopy_Click;
            //
            // tsPaste
            //
            tsPaste.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsPaste.Enabled = false;
            tsPaste.Image = (System.Drawing.Image)resources.GetObject("tsPaste.Image");
            tsPaste.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsPaste.Name = "tsPaste";
            tsPaste.Size = new System.Drawing.Size(29, 24);
            tsPaste.Text = "Paste row.  First copy a row then select rows to paste using the row header.";
            tsPaste.Click += TsPaste_Click;
            //
            // toolStripSeparator1
            //
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
            //
            // tsSetDefault
            //
            tsSetDefault.Image = (System.Drawing.Image)resources.GetObject("tsSetDefault.Image");
            tsSetDefault.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsSetDefault.Name = "tsSetDefault";
            tsSetDefault.Size = new System.Drawing.Size(107, 24);
            tsSetDefault.Text = "Set Default";
            tsSetDefault.ToolTipText = "Select rows using the row header and use this button to reset back to defaults.";
            tsSetDefault.Click += TsSetDefault_Click;
            //
            // tsDisable
            //
            tsDisable.Image = (System.Drawing.Image)resources.GetObject("tsDisable.Image");
            tsDisable.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsDisable.Name = "tsDisable";
            tsDisable.Size = new System.Drawing.Size(83, 24);
            tsDisable.Text = "Disable";
            tsDisable.ToolTipText = "Select rows using the row header and use this button to disable the collections";
            tsDisable.Click += TsDisable_Click;
            //
            // panel1
            //
            panel1.Controls.Add(label1);
            panel1.Controls.Add(bttnOK);
            panel1.Controls.Add(lnkCron);
            panel1.Controls.Add(bttnCancel);
            panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            panel1.Location = new System.Drawing.Point(0, 714);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(729, 77);
            panel1.TabIndex = 7;
            //
            // pnlTemporary
            //
            pnlTemporary.Controls.Add(lnkEndTemporary);
            pnlTemporary.Controls.Add(lblTemporarySchedules);
            pnlTemporary.Controls.Add(dtpTemporaryUntil);
            pnlTemporary.Controls.Add(chkTemporary);
            pnlTemporary.Dock = System.Windows.Forms.DockStyle.Bottom;
            pnlTemporary.Location = new System.Drawing.Point(0, 624);
            pnlTemporary.Name = "pnlTemporary";
            pnlTemporary.Size = new System.Drawing.Size(729, 90);
            pnlTemporary.TabIndex = 8;
            pnlTemporary.Visible = false;
            //
            // lnkEndTemporary
            //
            lnkEndTemporary.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lnkEndTemporary.AutoSize = true;
            lnkEndTemporary.LinkColor = System.Drawing.Color.FromArgb(0, 79, 131);
            lnkEndTemporary.Location = new System.Drawing.Point(560, 48);
            lnkEndTemporary.Name = "lnkEndTemporary";
            lnkEndTemporary.Size = new System.Drawing.Size(159, 20);
            lnkEndTemporary.TabIndex = 3;
            lnkEndTemporary.TabStop = true;
            lnkEndTemporary.Text = "End temporary now";
            lnkEndTemporary.Visible = false;
            lnkEndTemporary.LinkClicked += LnkEndTemporary_LinkClicked;
            //
            // lblTemporarySchedules
            //
            lblTemporarySchedules.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lblTemporarySchedules.Location = new System.Drawing.Point(3, 44);
            lblTemporarySchedules.Name = "lblTemporarySchedules";
            lblTemporarySchedules.Size = new System.Drawing.Size(550, 44);
            lblTemporarySchedules.TabIndex = 2;
            //
            // dtpTemporaryUntil
            //
            dtpTemporaryUntil.CustomFormat = "yyyy-MM-dd HH:mm";
            dtpTemporaryUntil.Enabled = false;
            dtpTemporaryUntil.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            dtpTemporaryUntil.Location = new System.Drawing.Point(410, 8);
            dtpTemporaryUntil.Name = "dtpTemporaryUntil";
            dtpTemporaryUntil.Size = new System.Drawing.Size(180, 27);
            dtpTemporaryUntil.TabIndex = 1;
            //
            // chkTemporary
            //
            chkTemporary.AutoSize = true;
            chkTemporary.Location = new System.Drawing.Point(6, 10);
            chkTemporary.Name = "chkTemporary";
            chkTemporary.Size = new System.Drawing.Size(398, 24);
            chkTemporary.TabIndex = 0;
            chkTemporary.Text = "Apply my changes temporarily, reverting automatically at";
            chkTemporary.UseVisualStyleBackColor = true;
            chkTemporary.CheckedChanged += ChkTemporary_CheckedChanged;
            //
            // ScheduleConfig
            //
            AcceptButton = bttnOK;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new System.Drawing.Size(729, 791);
            Controls.Add(dgv);
            Controls.Add(toolStrip1);
            Controls.Add(pnlTemporary);
            Controls.Add(panel1);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "ScheduleConfig";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Schedule";
            Load += ScheduleConfig_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            pnlTemporary.ResumeLayout(false);
            pnlTemporary.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private DBADashGUI.CustomReports.DBADashDataGridView dgv;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel lnkCron;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsPaste;
        private System.Windows.Forms.ToolStripButton tsSetDefault;
        private System.Windows.Forms.ToolStripButton tsDisable;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel pnlTemporary;
        private System.Windows.Forms.LinkLabel lnkEndTemporary;
        private System.Windows.Forms.Label lblTemporarySchedules;
        private System.Windows.Forms.DateTimePicker dtpTemporaryUntil;
        private System.Windows.Forms.CheckBox chkTemporary;
    }
}
