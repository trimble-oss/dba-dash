using DBADashGUI.Theme;

namespace DBADashGUI.DBFiles
{
    partial class FileThresholdConfig
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
            this.components = new System.ComponentModel.Container();
            DBADashGUI.DBFiles.FileThreshold fileThreshold3 = new DBADashGUI.DBFiles.FileThreshold();
            DBADashGUI.DBFiles.FileThreshold fileThreshold4 = new DBADashGUI.DBFiles.FileThreshold();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.bttnUpdate = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tab1 = new ThemedTabControl();
            this.tabData = new System.Windows.Forms.TabPage();
            this.dataConfig = new DBADashGUI.DBFiles.FileThresholdConfigControl();
            this.tabLog = new System.Windows.Forms.TabPage();
            this.logConfig = new DBADashGUI.DBFiles.FileThresholdConfigControl();
            this.cboLevel = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tab1.SuspendLayout();
            this.tabData.SuspendLayout();
            this.tabLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // bttnCancel
            // 
            this.bttnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnCancel.Location = new System.Drawing.Point(310, 425);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(91, 23);
            this.bttnCancel.TabIndex = 41;
            this.bttnCancel.Text = "Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            this.bttnCancel.Click += new System.EventHandler(this.BttnCancel_Click);
            // 
            // bttnUpdate
            // 
            this.bttnUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.bttnUpdate.Location = new System.Drawing.Point(421, 425);
            this.bttnUpdate.Name = "bttnUpdate";
            this.bttnUpdate.Size = new System.Drawing.Size(91, 23);
            this.bttnUpdate.TabIndex = 40;
            this.bttnUpdate.Text = "Update";
            this.bttnUpdate.UseVisualStyleBackColor = true;
            this.bttnUpdate.Click += new System.EventHandler(this.BttnUpdate_Click);
            // 
            // tab1
            // 
            this.tab1.Controls.Add(this.tabData);
            this.tab1.Controls.Add(this.tabLog);
            this.tab1.Location = new System.Drawing.Point(12, 12);
            this.tab1.Name = "tab1";
            this.tab1.SelectedIndex = 0;
            this.tab1.Size = new System.Drawing.Size(503, 390);
            this.tab1.TabIndex = 42;
            // 
            // tabData
            // 
            this.tabData.Controls.Add(this.dataConfig);
            this.tabData.Location = new System.Drawing.Point(4, 25);
            this.tabData.Name = "tabData";
            this.tabData.Padding = new System.Windows.Forms.Padding(3);
            this.tabData.Size = new System.Drawing.Size(495, 361);
            this.tabData.TabIndex = 0;
            this.tabData.Text = "Data";
            this.tabData.UseVisualStyleBackColor = true;
            // 
            // dataConfig
            // 
            this.dataConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            fileThreshold3.CriticalThreshold = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            fileThreshold3.DatabaseID = 0;
            fileThreshold3.DataSpaceID = 0;
            fileThreshold3.FileCheckType = DBADashGUI.DBFiles.FileThreshold.FileCheckTypeEnum.Percent;
            fileThreshold3.FileCheckTypeChar = '%';
            fileThreshold3.Inherited = false;
            fileThreshold3.InstanceID = 0;
            fileThreshold3.PctMaxCheckEnabled = true;
            fileThreshold3.PctMaxSizeCriticalThreshold = new decimal(new int[] {
            9,
            0,
            0,
            65536});
            fileThreshold3.PctMaxSizeWarningThreshold = new decimal(new int[] {
            8,
            0,
            0,
            65536});
            fileThreshold3.WarningThreshold = new decimal(new int[] {
            2,
            0,
            0,
            65536});
            fileThreshold3.ZeroAuthgrowthOnly = false;
            this.dataConfig.FileThreshold = fileThreshold3;
            this.dataConfig.Location = new System.Drawing.Point(3, 3);
            this.dataConfig.Name = "dataConfig";
            this.dataConfig.Size = new System.Drawing.Size(489, 355);
            this.dataConfig.TabIndex = 0;
            // 
            // tabLog
            // 
            this.tabLog.Controls.Add(this.logConfig);
            this.tabLog.Location = new System.Drawing.Point(4, 25);
            this.tabLog.Name = "tabLog";
            this.tabLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabLog.Size = new System.Drawing.Size(495, 361);
            this.tabLog.TabIndex = 1;
            this.tabLog.Text = "Log";
            this.tabLog.UseVisualStyleBackColor = true;
            // 
            // logConfig
            // 
            this.logConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            fileThreshold4.CriticalThreshold = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            fileThreshold4.DatabaseID = 0;
            fileThreshold4.DataSpaceID = 0;
            fileThreshold4.FileCheckType = DBADashGUI.DBFiles.FileThreshold.FileCheckTypeEnum.Percent;
            fileThreshold4.FileCheckTypeChar = '%';
            fileThreshold4.Inherited = false;
            fileThreshold4.InstanceID = 0;
            fileThreshold4.PctMaxCheckEnabled = true;
            fileThreshold4.PctMaxSizeCriticalThreshold = new decimal(new int[] {
            9,
            0,
            0,
            65536});
            fileThreshold4.PctMaxSizeWarningThreshold = new decimal(new int[] {
            8,
            0,
            0,
            65536});
            fileThreshold4.WarningThreshold = new decimal(new int[] {
            2,
            0,
            0,
            65536});
            fileThreshold4.ZeroAuthgrowthOnly = false;
            this.logConfig.FileThreshold = fileThreshold4;
            this.logConfig.Location = new System.Drawing.Point(3, 3);
            this.logConfig.Name = "logConfig";
            this.logConfig.Size = new System.Drawing.Size(489, 355);
            this.logConfig.TabIndex = 0;
            // 
            // cboLevel
            // 
            this.cboLevel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLevel.FormattingEnabled = true;
            this.cboLevel.Location = new System.Drawing.Point(86, 425);
            this.cboLevel.Name = "cboLevel";
            this.cboLevel.Size = new System.Drawing.Size(121, 24);
            this.cboLevel.TabIndex = 43;
            this.cboLevel.SelectedIndexChanged += new System.EventHandler(this.CboLevel_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 425);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 17);
            this.label1.TabIndex = 44;
            this.label1.Text = "Level:";
            // 
            // FileThresholdConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(524, 460);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cboLevel);
            this.Controls.Add(this.tab1);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.bttnUpdate);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "FileThresholdConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "File Threshold Config";
            this.Load += new System.EventHandler(this.FileThresholdConfig_Load);
            this.tab1.ResumeLayout(false);
            this.tabData.ResumeLayout(false);
            this.tabLog.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Button bttnUpdate;
        private System.Windows.Forms.ToolTip toolTip1;
        private ThemedTabControl tab1;
        private System.Windows.Forms.TabPage tabData;
        private FileThresholdConfigControl dataConfig;
        private System.Windows.Forms.TabPage tabLog;
        private FileThresholdConfigControl logConfig;
        private System.Windows.Forms.ComboBox cboLevel;
        private System.Windows.Forms.Label label1;
    }
}