namespace DBADashGUI.Performance
{
    partial class ObjectExecutionSummary
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.dgv = new System.Windows.Forms.DataGridView();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.tsTime = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.ts30Min = new System.Windows.Forms.ToolStripMenuItem();
            this.ts1Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts2Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts3Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts6Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.ts12Hr = new System.Windows.Forms.ToolStripMenuItem();
            this.dayToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.days7ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsCustom = new System.Windows.Forms.ToolStripMenuItem();
            this.tsCompare = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsTimeOffset = new System.Windows.Forms.ToolStripMenuItem();
            this.ts24Hrs = new System.Windows.Forms.ToolStripMenuItem();
            this.ts7Days = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsCustomCompare = new System.Windows.Forms.ToolStripMenuItem();
            this.tsNoCompare = new System.Windows.Forms.ToolStripMenuItem();
            this.tsColumns = new System.Windows.Forms.ToolStripDropDownButton();
            this.tsType = new System.Windows.Forms.ToolStripDropDownButton();
            this.procedureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.triggerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cLRProcedureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cLRTriggerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.scalarFunctionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extendedStoredProcedureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 27);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.Size = new System.Drawing.Size(1262, 888);
            this.dgv.TabIndex = 0;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy,
            this.tsTime,
            this.tsCompare,
            this.tsColumns,
            this.tsType});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1262, 27);
            this.toolStrip1.TabIndex = 4;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBADashGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 24);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.tsRefresh_Click);
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBADashGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 24);
            this.tsCopy.Text = "Copy";
            this.tsCopy.Click += new System.EventHandler(this.tsCopy_Click);
            // 
            // tsTime
            // 
            this.tsTime.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem2,
            this.toolStripMenuItem1,
            this.ts30Min,
            this.ts1Hr,
            this.ts2Hr,
            this.ts3Hr,
            this.ts6Hr,
            this.ts12Hr,
            this.dayToolStripMenuItem,
            this.days7ToolStripMenuItem,
            this.toolStripSeparator1,
            this.tsCustom});
            this.tsTime.Image = global::DBADashGUI.Properties.Resources.Time_16x;
            this.tsTime.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsTime.Name = "tsTime";
            this.tsTime.Size = new System.Drawing.Size(76, 24);
            this.tsTime.Text = "Time";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(143, 26);
            this.toolStripMenuItem2.Tag = "5";
            this.toolStripMenuItem2.Text = "5 Mins";
            this.toolStripMenuItem2.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Checked = true;
            this.toolStripMenuItem1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(143, 26);
            this.toolStripMenuItem1.Tag = "15";
            this.toolStripMenuItem1.Text = "15 Mins";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts30Min
            // 
            this.ts30Min.CheckOnClick = true;
            this.ts30Min.Name = "ts30Min";
            this.ts30Min.Size = new System.Drawing.Size(143, 26);
            this.ts30Min.Tag = "30";
            this.ts30Min.Text = "30 Mins";
            this.ts30Min.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts1Hr
            // 
            this.ts1Hr.Name = "ts1Hr";
            this.ts1Hr.Size = new System.Drawing.Size(143, 26);
            this.ts1Hr.Tag = "60";
            this.ts1Hr.Text = "1Hr";
            this.ts1Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts2Hr
            // 
            this.ts2Hr.CheckOnClick = true;
            this.ts2Hr.Name = "ts2Hr";
            this.ts2Hr.Size = new System.Drawing.Size(143, 26);
            this.ts2Hr.Tag = "120";
            this.ts2Hr.Text = "2Hr";
            this.ts2Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts3Hr
            // 
            this.ts3Hr.CheckOnClick = true;
            this.ts3Hr.Name = "ts3Hr";
            this.ts3Hr.Size = new System.Drawing.Size(143, 26);
            this.ts3Hr.Tag = "180";
            this.ts3Hr.Text = "3Hr";
            this.ts3Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts6Hr
            // 
            this.ts6Hr.CheckOnClick = true;
            this.ts6Hr.Name = "ts6Hr";
            this.ts6Hr.Size = new System.Drawing.Size(143, 26);
            this.ts6Hr.Tag = "360";
            this.ts6Hr.Text = "6Hr";
            this.ts6Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // ts12Hr
            // 
            this.ts12Hr.CheckOnClick = true;
            this.ts12Hr.Name = "ts12Hr";
            this.ts12Hr.Size = new System.Drawing.Size(143, 26);
            this.ts12Hr.Tag = "720";
            this.ts12Hr.Text = "12Hr";
            this.ts12Hr.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // dayToolStripMenuItem
            // 
            this.dayToolStripMenuItem.Name = "dayToolStripMenuItem";
            this.dayToolStripMenuItem.Size = new System.Drawing.Size(143, 26);
            this.dayToolStripMenuItem.Tag = "1440";
            this.dayToolStripMenuItem.Text = "1 Day";
            this.dayToolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // days7ToolStripMenuItem
            // 
            this.days7ToolStripMenuItem.Name = "days7ToolStripMenuItem";
            this.days7ToolStripMenuItem.Size = new System.Drawing.Size(143, 26);
            this.days7ToolStripMenuItem.Tag = "10080";
            this.days7ToolStripMenuItem.Text = "7 Days";
            this.days7ToolStripMenuItem.Click += new System.EventHandler(this.tsTime_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(140, 6);
            // 
            // tsCustom
            // 
            this.tsCustom.Name = "tsCustom";
            this.tsCustom.Size = new System.Drawing.Size(143, 26);
            this.tsCustom.Tag = "-1";
            this.tsCustom.Text = "Custom";
            this.tsCustom.Click += new System.EventHandler(this.tsCustom_Click);
            // 
            // tsCompare
            // 
            this.tsCompare.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsTimeOffset,
            this.ts24Hrs,
            this.ts7Days,
            this.toolStripSeparator2,
            this.tsCustomCompare,
            this.tsNoCompare});
            this.tsCompare.Image = global::DBADashGUI.Properties.Resources.Time_16x;
            this.tsCompare.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCompare.Name = "tsCompare";
            this.tsCompare.Size = new System.Drawing.Size(124, 24);
            this.tsCompare.Text = "Compare To";
            // 
            // tsTimeOffset
            // 
            this.tsTimeOffset.Name = "tsTimeOffset";
            this.tsTimeOffset.Size = new System.Drawing.Size(224, 26);
            this.tsTimeOffset.Tag = "60";
            this.tsTimeOffset.Text = "Previous Period";
            this.tsTimeOffset.Click += new System.EventHandler(this.tsSetOffset_Click);
            // 
            // ts24Hrs
            // 
            this.ts24Hrs.Name = "ts24Hrs";
            this.ts24Hrs.Size = new System.Drawing.Size(224, 26);
            this.ts24Hrs.Tag = "1440";
            this.ts24Hrs.Text = "-24hrs offset";
            this.ts24Hrs.Click += new System.EventHandler(this.tsSetOffset_Click);
            // 
            // ts7Days
            // 
            this.ts7Days.Name = "ts7Days";
            this.ts7Days.Size = new System.Drawing.Size(224, 26);
            this.ts7Days.Tag = "10080";
            this.ts7Days.Text = "-7 days offset";
            this.ts7Days.Click += new System.EventHandler(this.tsSetOffset_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(221, 6);
            // 
            // tsCustomCompare
            // 
            this.tsCustomCompare.Name = "tsCustomCompare";
            this.tsCustomCompare.Size = new System.Drawing.Size(224, 26);
            this.tsCustomCompare.Tag = "-1";
            this.tsCustomCompare.Text = "Custom";
            this.tsCustomCompare.Click += new System.EventHandler(this.tsCustomCompare_Click);
            // 
            // tsNoCompare
            // 
            this.tsNoCompare.Checked = true;
            this.tsNoCompare.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsNoCompare.Name = "tsNoCompare";
            this.tsNoCompare.Size = new System.Drawing.Size(224, 26);
            this.tsNoCompare.Tag = "0";
            this.tsNoCompare.Text = "None";
            this.tsNoCompare.Click += new System.EventHandler(this.tsSetOffset_Click);
            // 
            // tsColumns
            // 
            this.tsColumns.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsColumns.Image = global::DBADashGUI.Properties.Resources.Column_16x;
            this.tsColumns.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsColumns.Name = "tsColumns";
            this.tsColumns.Size = new System.Drawing.Size(34, 24);
            this.tsColumns.Text = "Columns";
            // 
            // tsType
            // 
            this.tsType.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.procedureToolStripMenuItem,
            this.triggerToolStripMenuItem,
            this.cLRProcedureToolStripMenuItem,
            this.cLRTriggerToolStripMenuItem,
            this.scalarFunctionToolStripMenuItem,
            this.extendedStoredProcedureToolStripMenuItem});
            this.tsType.Image = global::DBADashGUI.Properties.Resources.FilterDropdown_16x;
            this.tsType.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsType.Name = "tsType";
            this.tsType.Size = new System.Drawing.Size(74, 24);
            this.tsType.Text = "Type";
            // 
            // procedureToolStripMenuItem
            // 
            this.procedureToolStripMenuItem.CheckOnClick = true;
            this.procedureToolStripMenuItem.Name = "procedureToolStripMenuItem";
            this.procedureToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.procedureToolStripMenuItem.Tag = "P";
            this.procedureToolStripMenuItem.Text = "Procedure";
            this.procedureToolStripMenuItem.Click += new System.EventHandler(this.tsType_Click);
            // 
            // triggerToolStripMenuItem
            // 
            this.triggerToolStripMenuItem.CheckOnClick = true;
            this.triggerToolStripMenuItem.Name = "triggerToolStripMenuItem";
            this.triggerToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.triggerToolStripMenuItem.Tag = "TR";
            this.triggerToolStripMenuItem.Text = "Trigger";
            this.triggerToolStripMenuItem.Click += new System.EventHandler(this.tsType_Click);
            // 
            // cLRProcedureToolStripMenuItem
            // 
            this.cLRProcedureToolStripMenuItem.CheckOnClick = true;
            this.cLRProcedureToolStripMenuItem.Name = "cLRProcedureToolStripMenuItem";
            this.cLRProcedureToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.cLRProcedureToolStripMenuItem.Tag = "PC";
            this.cLRProcedureToolStripMenuItem.Text = "CLR Procedure";
            this.cLRProcedureToolStripMenuItem.Click += new System.EventHandler(this.tsType_Click);
            // 
            // cLRTriggerToolStripMenuItem
            // 
            this.cLRTriggerToolStripMenuItem.CheckOnClick = true;
            this.cLRTriggerToolStripMenuItem.Name = "cLRTriggerToolStripMenuItem";
            this.cLRTriggerToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.cLRTriggerToolStripMenuItem.Tag = "TA";
            this.cLRTriggerToolStripMenuItem.Text = "CLR Trigger";
            this.cLRTriggerToolStripMenuItem.Click += new System.EventHandler(this.tsType_Click);
            // 
            // scalarFunctionToolStripMenuItem
            // 
            this.scalarFunctionToolStripMenuItem.CheckOnClick = true;
            this.scalarFunctionToolStripMenuItem.Name = "scalarFunctionToolStripMenuItem";
            this.scalarFunctionToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.scalarFunctionToolStripMenuItem.Tag = "FN";
            this.scalarFunctionToolStripMenuItem.Text = "Scalar Function";
            this.scalarFunctionToolStripMenuItem.Click += new System.EventHandler(this.tsType_Click);
            // 
            // extendedStoredProcedureToolStripMenuItem
            // 
            this.extendedStoredProcedureToolStripMenuItem.CheckOnClick = true;
            this.extendedStoredProcedureToolStripMenuItem.Name = "extendedStoredProcedureToolStripMenuItem";
            this.extendedStoredProcedureToolStripMenuItem.Size = new System.Drawing.Size(273, 26);
            this.extendedStoredProcedureToolStripMenuItem.Tag = "X";
            this.extendedStoredProcedureToolStripMenuItem.Text = "Extended Stored Procedure";
            this.extendedStoredProcedureToolStripMenuItem.Click += new System.EventHandler(this.tsType_Click);
            // 
            // ObjectExecutionSummary
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.toolStrip1);
            this.Name = "ObjectExecutionSummary";
            this.Size = new System.Drawing.Size(1262, 915);
            this.Load += new System.EventHandler(this.ObjectExecutionSummary_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripDropDownButton tsTime;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem ts30Min;
        private System.Windows.Forms.ToolStripMenuItem ts1Hr;
        private System.Windows.Forms.ToolStripMenuItem ts2Hr;
        private System.Windows.Forms.ToolStripMenuItem ts3Hr;
        private System.Windows.Forms.ToolStripMenuItem ts6Hr;
        private System.Windows.Forms.ToolStripMenuItem ts12Hr;
        private System.Windows.Forms.ToolStripMenuItem dayToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem days7ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem tsCustom;
        private System.Windows.Forms.ToolStripDropDownButton tsCompare;
        private System.Windows.Forms.ToolStripMenuItem ts24Hrs;
        private System.Windows.Forms.ToolStripMenuItem ts7Days;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem tsCustomCompare;
        private System.Windows.Forms.ToolStripDropDownButton tsColumns;
        private System.Windows.Forms.ToolStripMenuItem tsNoCompare;
        private System.Windows.Forms.ToolStripMenuItem tsTimeOffset;
        private System.Windows.Forms.ToolStripDropDownButton tsType;
        private System.Windows.Forms.ToolStripMenuItem procedureToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem triggerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cLRProcedureToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cLRTriggerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem scalarFunctionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem extendedStoredProcedureToolStripMenuItem;
    }
}
