
namespace DBADashServiceConfig
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
            dgv = new System.Windows.Forms.DataGridView();
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
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            toolStrip1.SuspendLayout();
            panel1.SuspendLayout();
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
            dgv.Size = new System.Drawing.Size(729, 687);
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
            lnkCron.Image = Properties.Resources.Information_blue_6227_16x16_cyan;
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
            tsCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
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
            tsPaste.Image = Properties.Resources.ASX_Paste_blue_16x;
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
            tsSetDefault.Image = Properties.Resources.StatusAnnotations_Complete_and_ok_16xMD_color;
            tsSetDefault.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsSetDefault.Name = "tsSetDefault";
            tsSetDefault.Size = new System.Drawing.Size(107, 24);
            tsSetDefault.Text = "Set Default";
            tsSetDefault.ToolTipText = "Select rows using the row header and use this button to reset back to defaults.";
            tsSetDefault.Click += TsSetDefault_Click;
            // 
            // tsDisable
            // 
            tsDisable.Image = Properties.Resources.Stop_16x;
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
            // ScheduleConfig
            // 
            AcceptButton = bttnOK;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new System.Drawing.Size(729, 791);
            Controls.Add(dgv);
            Controls.Add(toolStrip1);
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
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
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
    }
}