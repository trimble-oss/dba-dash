
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
            this.dgv = new System.Windows.Forms.DataGridView();
            this.bttnOK = new System.Windows.Forms.Button();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.lnkCron = new System.Windows.Forms.LinkLabel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsPaste = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsSetDefault = new System.Windows.Forms.ToolStripButton();
            this.tsDisable = new System.Windows.Forms.ToolStripButton();
            this.panel1 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 27);
            this.dgv.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgv.Name = "dgv";
            this.dgv.RowHeadersWidth = 51;
            this.dgv.Size = new System.Drawing.Size(729, 687);
            this.dgv.TabIndex = 0;
            this.dgv.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.Dgv_CellContentClick);
            this.dgv.CellValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.Dgv_CellValidated);
            this.dgv.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.Dgv_CellValueChanged);
            this.dgv.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.Dgv_RowsAdded);
            this.dgv.RowValidating += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.Dgv_RowValidating);
            this.dgv.SelectionChanged += new System.EventHandler(this.Dgv_SelectionChanged);
            // 
            // bttnOK
            // 
            this.bttnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnOK.Location = new System.Drawing.Point(644, 36);
            this.bttnOK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnOK.Name = "bttnOK";
            this.bttnOK.Size = new System.Drawing.Size(75, 29);
            this.bttnOK.TabIndex = 2;
            this.bttnOK.Text = "OK";
            this.bttnOK.UseVisualStyleBackColor = true;
            this.bttnOK.Click += new System.EventHandler(this.BttnOK_Click);
            // 
            // bttnCancel
            // 
            this.bttnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bttnCancel.Location = new System.Drawing.Point(554, 36);
            this.bttnCancel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(75, 29);
            this.bttnCancel.TabIndex = 3;
            this.bttnCancel.Text = "Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            this.bttnCancel.Click += new System.EventHandler(this.BttnCancel_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(449, 20);
            this.label1.TabIndex = 4;
            this.label1.Text = "Schedule can be a period defined in seconds or a cron expression. ";
            // 
            // lnkCron
            // 
            this.lnkCron.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lnkCron.Image = global::DBADashServiceConfig.Properties.Resources.Information_blue_6227_16x16_cyan;
            this.lnkCron.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lnkCron.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(79)))), ((int)(((byte)(131)))));
            this.lnkCron.Location = new System.Drawing.Point(2, 47);
            this.lnkCron.Name = "lnkCron";
            this.lnkCron.Size = new System.Drawing.Size(68, 23);
            this.lnkCron.TabIndex = 5;
            this.lnkCron.TabStop = true;
            this.lnkCron.Text = "Help";
            this.lnkCron.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.lnkCron.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkCron_LinkClicked);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsCopy,
            this.tsPaste,
            this.toolStripSeparator1,
            this.tsSetDefault,
            this.tsDisable});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(729, 27);
            this.toolStrip1.TabIndex = 6;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Enabled = false;
            this.tsCopy.Image = global::DBADashServiceConfig.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 24);
            this.tsCopy.Text = "Copy row.  Use the row header to select a single row to copy.  You can then apply" +
    " the same configuration to other rows with the paste button.";
            this.tsCopy.Click += new System.EventHandler(this.TsCopy_Click);
            // 
            // tsPaste
            // 
            this.tsPaste.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsPaste.Enabled = false;
            this.tsPaste.Image = global::DBADashServiceConfig.Properties.Resources.ASX_Paste_blue_16x;
            this.tsPaste.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsPaste.Name = "tsPaste";
            this.tsPaste.Size = new System.Drawing.Size(29, 24);
            this.tsPaste.Text = "Paste row.  First copy a row then select rows to paste using the row header.";
            this.tsPaste.Click += new System.EventHandler(this.TsPaste_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
            // 
            // tsSetDefault
            // 
            this.tsSetDefault.Image = global::DBADashServiceConfig.Properties.Resources.StatusAnnotations_Complete_and_ok_16xMD_color;
            this.tsSetDefault.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsSetDefault.Name = "tsSetDefault";
            this.tsSetDefault.Size = new System.Drawing.Size(107, 24);
            this.tsSetDefault.Text = "Set Default";
            this.tsSetDefault.ToolTipText = "Select rows using the row header and use this button to reset back to defaults.";
            this.tsSetDefault.Click += new System.EventHandler(this.TsSetDefault_Click);
            // 
            // tsDisable
            // 
            this.tsDisable.Image = global::DBADashServiceConfig.Properties.Resources.Stop_16x;
            this.tsDisable.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsDisable.Name = "tsDisable";
            this.tsDisable.Size = new System.Drawing.Size(83, 24);
            this.tsDisable.Text = "Disable";
            this.tsDisable.ToolTipText = "Select rows using the row header and use this button to disable the collections";
            this.tsDisable.Click += new System.EventHandler(this.TsDisable_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.bttnOK);
            this.panel1.Controls.Add(this.lnkCron);
            this.panel1.Controls.Add(this.bttnCancel);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 714);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(729, 77);
            this.panel1.TabIndex = 7;
            // 
            // ScheduleConfig
            // 
            this.AcceptButton = this.bttnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttnCancel;
            this.ClientSize = new System.Drawing.Size(729, 791);
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "ScheduleConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Schedule";
            this.Load += new System.EventHandler(this.ScheduleConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

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