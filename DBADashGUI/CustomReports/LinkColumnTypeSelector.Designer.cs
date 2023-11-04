namespace DBADashGUI.CustomReports
{
    partial class LinkColumnTypeSelector
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LinkColumnTypeSelector));
            optNone = new System.Windows.Forms.RadioButton();
            optURL = new System.Windows.Forms.RadioButton();
            optText = new System.Windows.Forms.RadioButton();
            optDrillDown = new System.Windows.Forms.RadioButton();
            tab1 = new System.Windows.Forms.TabControl();
            tabNone = new System.Windows.Forms.TabPage();
            label2 = new System.Windows.Forms.Label();
            tabURL = new System.Windows.Forms.TabPage();
            lblWarning = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            tabText = new System.Windows.Forms.TabPage();
            label7 = new System.Windows.Forms.Label();
            cboLanguage = new System.Windows.Forms.ComboBox();
            label3 = new System.Windows.Forms.Label();
            tabDrillDown = new System.Windows.Forms.TabPage();
            dgvMapping = new System.Windows.Forms.DataGridView();
            label8 = new System.Windows.Forms.Label();
            cboReport = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            tabQueryPlan = new System.Windows.Forms.TabPage();
            label10 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            bttnOK = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            cboTargetColumn = new System.Windows.Forms.ComboBox();
            label9 = new System.Windows.Forms.Label();
            txtLinkColumn = new System.Windows.Forms.TextBox();
            optQueryPlan = new System.Windows.Forms.RadioButton();
            tab1.SuspendLayout();
            tabNone.SuspendLayout();
            tabURL.SuspendLayout();
            tabText.SuspendLayout();
            tabDrillDown.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvMapping).BeginInit();
            tabQueryPlan.SuspendLayout();
            SuspendLayout();
            // 
            // optNone
            // 
            optNone.AutoSize = true;
            optNone.Checked = true;
            optNone.Location = new System.Drawing.Point(14, 12);
            optNone.Name = "optNone";
            optNone.Size = new System.Drawing.Size(66, 24);
            optNone.TabIndex = 0;
            optNone.TabStop = true;
            optNone.Text = "None";
            optNone.UseVisualStyleBackColor = true;
            optNone.CheckedChanged += Opt_CheckedChanged;
            // 
            // optURL
            // 
            optURL.AutoSize = true;
            optURL.Location = new System.Drawing.Point(94, 12);
            optURL.Name = "optURL";
            optURL.Size = new System.Drawing.Size(56, 24);
            optURL.TabIndex = 1;
            optURL.Text = "URL";
            optURL.UseVisualStyleBackColor = true;
            optURL.CheckedChanged += Opt_CheckedChanged;
            // 
            // optText
            // 
            optText.AutoSize = true;
            optText.Location = new System.Drawing.Point(164, 12);
            optText.Name = "optText";
            optText.Size = new System.Drawing.Size(57, 24);
            optText.TabIndex = 2;
            optText.Text = "Text";
            optText.UseVisualStyleBackColor = true;
            optText.CheckedChanged += Opt_CheckedChanged;
            // 
            // optDrillDown
            // 
            optDrillDown.AutoSize = true;
            optDrillDown.Location = new System.Drawing.Point(350, 12);
            optDrillDown.Name = "optDrillDown";
            optDrillDown.Size = new System.Drawing.Size(101, 24);
            optDrillDown.TabIndex = 4;
            optDrillDown.Text = "Drill Down";
            optDrillDown.UseVisualStyleBackColor = true;
            optDrillDown.CheckedChanged += Opt_CheckedChanged;
            // 
            // tab1
            // 
            tab1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tab1.Controls.Add(tabNone);
            tab1.Controls.Add(tabURL);
            tab1.Controls.Add(tabText);
            tab1.Controls.Add(tabDrillDown);
            tab1.Controls.Add(tabQueryPlan);
            tab1.Location = new System.Drawing.Point(14, 127);
            tab1.Name = "tab1";
            tab1.SelectedIndex = 0;
            tab1.Size = new System.Drawing.Size(453, 292);
            tab1.TabIndex = 7;
            // 
            // tabNone
            // 
            tabNone.Controls.Add(label2);
            tabNone.Location = new System.Drawing.Point(4, 29);
            tabNone.Name = "tabNone";
            tabNone.Padding = new System.Windows.Forms.Padding(3);
            tabNone.Size = new System.Drawing.Size(445, 259);
            tabNone.TabIndex = 0;
            tabNone.Text = "None";
            tabNone.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            label2.Dock = System.Windows.Forms.DockStyle.Fill;
            label2.Location = new System.Drawing.Point(3, 3);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(439, 253);
            label2.TabIndex = 0;
            label2.Text = "No link column";
            // 
            // tabURL
            // 
            tabURL.Controls.Add(lblWarning);
            tabURL.Controls.Add(label6);
            tabURL.Location = new System.Drawing.Point(4, 29);
            tabURL.Name = "tabURL";
            tabURL.Padding = new System.Windows.Forms.Padding(3);
            tabURL.Size = new System.Drawing.Size(445, 259);
            tabURL.TabIndex = 1;
            tabURL.Text = "URL";
            tabURL.UseVisualStyleBackColor = true;
            // 
            // lblWarning
            // 
            lblWarning.Dock = System.Windows.Forms.DockStyle.Bottom;
            lblWarning.ForeColor = System.Drawing.Color.Red;
            lblWarning.Location = new System.Drawing.Point(3, 201);
            lblWarning.Name = "lblWarning";
            lblWarning.Size = new System.Drawing.Size(439, 55);
            lblWarning.TabIndex = 1;
            lblWarning.Text = "Warning: Please use a trusted source for the link URL.  DBA Dash provides no guarantee that links are safe.";
            // 
            // label6
            // 
            label6.Dock = System.Windows.Forms.DockStyle.Top;
            label6.Location = new System.Drawing.Point(3, 3);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(439, 81);
            label6.TabIndex = 2;
            label6.Text = "Link will navigate to a URL\r\nEnsure the column specified as the Target Column provides a valid URL.";
            // 
            // tabText
            // 
            tabText.Controls.Add(label7);
            tabText.Controls.Add(cboLanguage);
            tabText.Controls.Add(label3);
            tabText.Location = new System.Drawing.Point(4, 29);
            tabText.Name = "tabText";
            tabText.Size = new System.Drawing.Size(445, 259);
            tabText.TabIndex = 2;
            tabText.Text = "Text";
            tabText.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            label7.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            label7.Location = new System.Drawing.Point(0, 45);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(412, 42);
            label7.TabIndex = 12;
            label7.Text = "Text associated with the Target Column will be loaded into a separate window.  Select a language for syntax highlighting";
            // 
            // cboLanguage
            // 
            cboLanguage.FormattingEnabled = true;
            cboLanguage.Location = new System.Drawing.Point(110, 3);
            cboLanguage.Name = "cboLanguage";
            cboLanguage.Size = new System.Drawing.Size(151, 28);
            cboLanguage.TabIndex = 11;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(0, 6);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(74, 20);
            label3.TabIndex = 10;
            label3.Text = "Text Type:";
            // 
            // tabDrillDown
            // 
            tabDrillDown.Controls.Add(dgvMapping);
            tabDrillDown.Controls.Add(label8);
            tabDrillDown.Controls.Add(cboReport);
            tabDrillDown.Controls.Add(label1);
            tabDrillDown.Location = new System.Drawing.Point(4, 29);
            tabDrillDown.Name = "tabDrillDown";
            tabDrillDown.Padding = new System.Windows.Forms.Padding(3);
            tabDrillDown.Size = new System.Drawing.Size(445, 259);
            tabDrillDown.TabIndex = 3;
            tabDrillDown.Text = "Drill Down";
            tabDrillDown.UseVisualStyleBackColor = true;
            // 
            // dgvMapping
            // 
            dgvMapping.AllowUserToAddRows = false;
            dgvMapping.AllowUserToDeleteRows = false;
            dgvMapping.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dgvMapping.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvMapping.Location = new System.Drawing.Point(110, 49);
            dgvMapping.Name = "dgvMapping";
            dgvMapping.RowHeadersVisible = false;
            dgvMapping.RowHeadersWidth = 51;
            dgvMapping.RowTemplate.Height = 29;
            dgvMapping.Size = new System.Drawing.Size(329, 133);
            dgvMapping.TabIndex = 14;
            // 
            // label8
            // 
            label8.Dock = System.Windows.Forms.DockStyle.Bottom;
            label8.Location = new System.Drawing.Point(3, 197);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(439, 59);
            label8.TabIndex = 13;
            label8.Text = "Clicking will initiate a drill down action.  Report will be loaded with parameters mapped to column values as specified";
            // 
            // cboReport
            // 
            cboReport.FormattingEnabled = true;
            cboReport.Location = new System.Drawing.Point(110, 6);
            cboReport.Name = "cboReport";
            cboReport.Size = new System.Drawing.Size(151, 28);
            cboReport.TabIndex = 2;
            cboReport.SelectedIndexChanged += CboReport_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(0, 9);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(54, 20);
            label1.TabIndex = 1;
            label1.Text = "Report";
            // 
            // tabQueryPlan
            // 
            tabQueryPlan.Controls.Add(label10);
            tabQueryPlan.Location = new System.Drawing.Point(4, 29);
            tabQueryPlan.Name = "tabQueryPlan";
            tabQueryPlan.Padding = new System.Windows.Forms.Padding(3);
            tabQueryPlan.Size = new System.Drawing.Size(445, 259);
            tabQueryPlan.TabIndex = 4;
            tabQueryPlan.Text = "Query Plan";
            tabQueryPlan.UseVisualStyleBackColor = true;
            // 
            // label10
            // 
            label10.Dock = System.Windows.Forms.DockStyle.Fill;
            label10.Location = new System.Drawing.Point(3, 3);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(439, 253);
            label10.TabIndex = 1;
            label10.Text = "Query plan will be loaded in default app for *.sqlplan\r\n\r\nEnsure that the Target Column selected returns an XML query plan.";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(18, 97);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(108, 20);
            label5.TabIndex = 10;
            label5.Text = "Target Column:";
            // 
            // bttnOK
            // 
            bttnOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnOK.Location = new System.Drawing.Point(369, 425);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new System.Drawing.Size(94, 29);
            bttnOK.TabIndex = 12;
            bttnOK.Text = "&OK";
            bttnOK.UseVisualStyleBackColor = true;
            bttnOK.Click += BttnOK_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.Location = new System.Drawing.Point(269, 425);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 13;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += BttnCancel_Click;
            // 
            // cboTargetColumn
            // 
            cboTargetColumn.FormattingEnabled = true;
            cboTargetColumn.Location = new System.Drawing.Point(128, 93);
            cboTargetColumn.Name = "cboTargetColumn";
            cboTargetColumn.Size = new System.Drawing.Size(151, 28);
            cboTargetColumn.TabIndex = 6;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(18, 53);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(93, 20);
            label9.TabIndex = 15;
            label9.Text = "Link Column:";
            // 
            // txtLinkColumn
            // 
            txtLinkColumn.Enabled = false;
            txtLinkColumn.Location = new System.Drawing.Point(128, 50);
            txtLinkColumn.Name = "txtLinkColumn";
            txtLinkColumn.Size = new System.Drawing.Size(151, 27);
            txtLinkColumn.TabIndex = 5;
            // 
            // optQueryPlan
            // 
            optQueryPlan.AutoSize = true;
            optQueryPlan.Location = new System.Drawing.Point(235, 12);
            optQueryPlan.Name = "optQueryPlan";
            optQueryPlan.Size = new System.Drawing.Size(101, 24);
            optQueryPlan.TabIndex = 3;
            optQueryPlan.TabStop = true;
            optQueryPlan.Text = "Query Plan";
            optQueryPlan.UseVisualStyleBackColor = true;
            // 
            // LinkColumnTypeSelector
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(479, 465);
            Controls.Add(optQueryPlan);
            Controls.Add(txtLinkColumn);
            Controls.Add(label9);
            Controls.Add(cboTargetColumn);
            Controls.Add(bttnCancel);
            Controls.Add(bttnOK);
            Controls.Add(tab1);
            Controls.Add(label5);
            Controls.Add(optDrillDown);
            Controls.Add(optText);
            Controls.Add(optURL);
            Controls.Add(optNone);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MinimumSize = new System.Drawing.Size(475, 450);
            Name = "LinkColumnTypeSelector";
            Text = "Link Type";
            Load += LinkColumnTypeSelector_Load;
            tab1.ResumeLayout(false);
            tabNone.ResumeLayout(false);
            tabURL.ResumeLayout(false);
            tabText.ResumeLayout(false);
            tabText.PerformLayout();
            tabDrillDown.ResumeLayout(false);
            tabDrillDown.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dgvMapping).EndInit();
            tabQueryPlan.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.RadioButton optNone;
        private System.Windows.Forms.RadioButton optURL;
        private System.Windows.Forms.RadioButton optText;
        private System.Windows.Forms.RadioButton optDrillDown;
        private System.Windows.Forms.TabControl tab1;
        private System.Windows.Forms.TabPage tabNone;
        private System.Windows.Forms.TabPage tabURL;
        private System.Windows.Forms.TabPage tabText;
        private System.Windows.Forms.TabPage tabDrillDown;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblWarning;
        private System.Windows.Forms.ComboBox cboLanguage;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cboReport;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox cboTargetColumn;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtLinkColumn;
        private System.Windows.Forms.RadioButton optQueryPlan;
        private System.Windows.Forms.TabPage tabQueryPlan;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.DataGridView dgvMapping;
    }
}