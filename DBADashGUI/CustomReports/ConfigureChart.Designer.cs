namespace DBADashGUI.CustomReports
{
    partial class ConfigureChart
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigureChart));
            cboResults = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            cboChartType = new System.Windows.Forms.ComboBox();
            label2 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            cboSeries = new System.Windows.Forms.ComboBox();
            label4 = new System.Windows.Forms.Label();
            chkMetrics = new System.Windows.Forms.CheckedListBox();
            label3 = new System.Windows.Forms.Label();
            cboXCol = new System.Windows.Forms.ComboBox();
            pnlChart = new System.Windows.Forms.Panel();
            tabConfig = new System.Windows.Forms.TabControl();
            tabData = new System.Windows.Forms.TabPage();
            tabPieData = new System.Windows.Forms.TabPage();
            numRadius = new System.Windows.Forms.NumericUpDown();
            label16 = new System.Windows.Forms.Label();
            chkDoughnut = new System.Windows.Forms.CheckBox();
            label7 = new System.Windows.Forms.Label();
            cboPieValueColumn = new System.Windows.Forms.ComboBox();
            label6 = new System.Windows.Forms.Label();
            cboPieCategory = new System.Windows.Forms.ComboBox();
            tabOptions = new System.Windows.Forms.TabPage();
            txtPointSize = new System.Windows.Forms.TextBox();
            lblPointSize = new System.Windows.Forms.Label();
            chkLineFill = new System.Windows.Forms.CheckBox();
            label9 = new System.Windows.Forms.Label();
            cboLegend = new System.Windows.Forms.ComboBox();
            label8 = new System.Windows.Forms.Label();
            txtTitle = new System.Windows.Forms.TextBox();
            tabAxis = new System.Windows.Forms.TabPage();
            label14 = new System.Windows.Forms.Label();
            txtYFormat = new System.Windows.Forms.TextBox();
            label13 = new System.Windows.Forms.Label();
            txtYMax = new System.Windows.Forms.TextBox();
            label12 = new System.Windows.Forms.Label();
            txtYMin = new System.Windows.Forms.TextBox();
            label11 = new System.Windows.Forms.Label();
            label10 = new System.Windows.Forms.Label();
            txtYLabel = new System.Windows.Forms.TextBox();
            txtXLabel = new System.Windows.Forms.TextBox();
            bttnAdd = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            tabConfig.SuspendLayout();
            tabData.SuspendLayout();
            tabPieData.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numRadius).BeginInit();
            tabOptions.SuspendLayout();
            tabAxis.SuspendLayout();
            SuspendLayout();
            // 
            // cboResults
            // 
            cboResults.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboResults.FormattingEnabled = true;
            cboResults.Location = new System.Drawing.Point(234, 42);
            cboResults.Name = "cboResults";
            cboResults.Size = new System.Drawing.Size(151, 28);
            cboResults.TabIndex = 0;
            cboResults.SelectedIndexChanged += Results_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(25, 45);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(83, 20);
            label1.TabIndex = 1;
            label1.Text = "Chart Data:";
            // 
            // cboChartType
            // 
            cboChartType.FormattingEnabled = true;
            cboChartType.Location = new System.Drawing.Point(234, 76);
            cboChartType.Name = "cboChartType";
            cboChartType.Size = new System.Drawing.Size(151, 28);
            cboChartType.TabIndex = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(26, 79);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(82, 20);
            label2.TabIndex = 3;
            label2.Text = "Chart Type:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(3, 43);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(173, 20);
            label5.TabIndex = 5;
            label5.Text = "Series Column (optional)";
            // 
            // cboSeries
            // 
            cboSeries.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboSeries.FormattingEnabled = true;
            cboSeries.Location = new System.Drawing.Point(205, 40);
            cboSeries.Name = "cboSeries";
            cboSeries.Size = new System.Drawing.Size(151, 28);
            cboSeries.TabIndex = 4;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(3, 74);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(60, 20);
            label4.TabIndex = 3;
            label4.Text = "Metrics:";
            // 
            // chkMetrics
            // 
            chkMetrics.FormattingEnabled = true;
            chkMetrics.Location = new System.Drawing.Point(205, 74);
            chkMetrics.Name = "chkMetrics";
            chkMetrics.Size = new System.Drawing.Size(151, 114);
            chkMetrics.TabIndex = 2;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(3, 9);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(76, 20);
            label3.TabIndex = 1;
            label3.Text = "X Column:";
            // 
            // cboXCol
            // 
            cboXCol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboXCol.FormattingEnabled = true;
            cboXCol.Location = new System.Drawing.Point(205, 6);
            cboXCol.Name = "cboXCol";
            cboXCol.Size = new System.Drawing.Size(151, 28);
            cboXCol.TabIndex = 0;
            // 
            // pnlChart
            // 
            pnlChart.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            pnlChart.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pnlChart.Location = new System.Drawing.Point(525, 40);
            pnlChart.Name = "pnlChart";
            pnlChart.Size = new System.Drawing.Size(584, 363);
            pnlChart.TabIndex = 5;
            // 
            // tabConfig
            // 
            tabConfig.Controls.Add(tabData);
            tabConfig.Controls.Add(tabPieData);
            tabConfig.Controls.Add(tabOptions);
            tabConfig.Controls.Add(tabAxis);
            tabConfig.Location = new System.Drawing.Point(25, 123);
            tabConfig.Name = "tabConfig";
            tabConfig.SelectedIndex = 0;
            tabConfig.Size = new System.Drawing.Size(453, 280);
            tabConfig.TabIndex = 7;
            // 
            // tabData
            // 
            tabData.Controls.Add(label5);
            tabData.Controls.Add(label3);
            tabData.Controls.Add(cboSeries);
            tabData.Controls.Add(cboXCol);
            tabData.Controls.Add(label4);
            tabData.Controls.Add(chkMetrics);
            tabData.Location = new System.Drawing.Point(4, 29);
            tabData.Name = "tabData";
            tabData.Padding = new System.Windows.Forms.Padding(3);
            tabData.Size = new System.Drawing.Size(445, 247);
            tabData.TabIndex = 0;
            tabData.Text = "Data";
            tabData.UseVisualStyleBackColor = true;
            // 
            // tabPieData
            // 
            tabPieData.Controls.Add(numRadius);
            tabPieData.Controls.Add(label16);
            tabPieData.Controls.Add(chkDoughnut);
            tabPieData.Controls.Add(label7);
            tabPieData.Controls.Add(cboPieValueColumn);
            tabPieData.Controls.Add(label6);
            tabPieData.Controls.Add(cboPieCategory);
            tabPieData.Location = new System.Drawing.Point(4, 29);
            tabPieData.Name = "tabPieData";
            tabPieData.Padding = new System.Windows.Forms.Padding(3);
            tabPieData.Size = new System.Drawing.Size(445, 247);
            tabPieData.TabIndex = 1;
            tabPieData.Text = "Pie";
            tabPieData.UseVisualStyleBackColor = true;
            // 
            // numRadius
            // 
            numRadius.Location = new System.Drawing.Point(204, 104);
            numRadius.Name = "numRadius";
            numRadius.Size = new System.Drawing.Size(150, 27);
            numRadius.TabIndex = 8;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new System.Drawing.Point(3, 106);
            label16.Name = "label16";
            label16.Size = new System.Drawing.Size(69, 20);
            label16.TabIndex = 7;
            label16.Text = "Radius %";
            // 
            // chkDoughnut
            // 
            chkDoughnut.AutoSize = true;
            chkDoughnut.Location = new System.Drawing.Point(204, 74);
            chkDoughnut.Name = "chkDoughnut";
            chkDoughnut.Size = new System.Drawing.Size(97, 24);
            chkDoughnut.TabIndex = 5;
            chkDoughnut.Text = "Doughnut";
            chkDoughnut.UseVisualStyleBackColor = true;
            chkDoughnut.CheckedChanged += chkDoughnut_CheckedChanged;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(3, 43);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(100, 20);
            label7.TabIndex = 4;
            label7.Text = "Value Column";
            // 
            // cboPieValueColumn
            // 
            cboPieValueColumn.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboPieValueColumn.FormattingEnabled = true;
            cboPieValueColumn.Location = new System.Drawing.Point(204, 40);
            cboPieValueColumn.Name = "cboPieValueColumn";
            cboPieValueColumn.Size = new System.Drawing.Size(151, 28);
            cboPieValueColumn.TabIndex = 3;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(3, 9);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(124, 20);
            label6.TabIndex = 2;
            label6.Text = "Category Column";
            // 
            // cboPieCategory
            // 
            cboPieCategory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboPieCategory.FormattingEnabled = true;
            cboPieCategory.Location = new System.Drawing.Point(205, 6);
            cboPieCategory.Name = "cboPieCategory";
            cboPieCategory.Size = new System.Drawing.Size(151, 28);
            cboPieCategory.TabIndex = 1;
            // 
            // tabOptions
            // 
            tabOptions.Controls.Add(txtPointSize);
            tabOptions.Controls.Add(lblPointSize);
            tabOptions.Controls.Add(chkLineFill);
            tabOptions.Controls.Add(label9);
            tabOptions.Controls.Add(cboLegend);
            tabOptions.Controls.Add(label8);
            tabOptions.Controls.Add(txtTitle);
            tabOptions.Location = new System.Drawing.Point(4, 29);
            tabOptions.Name = "tabOptions";
            tabOptions.Padding = new System.Windows.Forms.Padding(3);
            tabOptions.Size = new System.Drawing.Size(445, 247);
            tabOptions.TabIndex = 2;
            tabOptions.Text = "Options";
            tabOptions.UseVisualStyleBackColor = true;
            // 
            // txtPointSize
            // 
            txtPointSize.Location = new System.Drawing.Point(205, 103);
            txtPointSize.Name = "txtPointSize";
            txtPointSize.Size = new System.Drawing.Size(151, 27);
            txtPointSize.TabIndex = 6;
            // 
            // lblPointSize
            // 
            lblPointSize.AutoSize = true;
            lblPointSize.Location = new System.Drawing.Point(6, 106);
            lblPointSize.Name = "lblPointSize";
            lblPointSize.Size = new System.Drawing.Size(73, 20);
            lblPointSize.TabIndex = 5;
            lblPointSize.Text = "Point Size";
            // 
            // chkLineFill
            // 
            chkLineFill.AutoSize = true;
            chkLineFill.Location = new System.Drawing.Point(205, 73);
            chkLineFill.Name = "chkLineFill";
            chkLineFill.Size = new System.Drawing.Size(81, 24);
            chkLineFill.TabIndex = 4;
            chkLineFill.Text = "Line Fill";
            chkLineFill.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(6, 42);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(61, 20);
            label9.TabIndex = 3;
            label9.Text = "Legend:";
            // 
            // cboLegend
            // 
            cboLegend.FormattingEnabled = true;
            cboLegend.Location = new System.Drawing.Point(205, 39);
            cboLegend.Name = "cboLegend";
            cboLegend.Size = new System.Drawing.Size(151, 28);
            cboLegend.TabIndex = 2;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new System.Drawing.Point(6, 9);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(41, 20);
            label8.TabIndex = 1;
            label8.Text = "Title:";
            // 
            // txtTitle
            // 
            txtTitle.Location = new System.Drawing.Point(205, 6);
            txtTitle.Name = "txtTitle";
            txtTitle.Size = new System.Drawing.Size(151, 27);
            txtTitle.TabIndex = 0;
            // 
            // tabAxis
            // 
            tabAxis.Controls.Add(label14);
            tabAxis.Controls.Add(txtYFormat);
            tabAxis.Controls.Add(label13);
            tabAxis.Controls.Add(txtYMax);
            tabAxis.Controls.Add(label12);
            tabAxis.Controls.Add(txtYMin);
            tabAxis.Controls.Add(label11);
            tabAxis.Controls.Add(label10);
            tabAxis.Controls.Add(txtYLabel);
            tabAxis.Controls.Add(txtXLabel);
            tabAxis.Location = new System.Drawing.Point(4, 29);
            tabAxis.Name = "tabAxis";
            tabAxis.Padding = new System.Windows.Forms.Padding(3);
            tabAxis.Size = new System.Drawing.Size(445, 247);
            tabAxis.TabIndex = 3;
            tabAxis.Text = "Axis";
            tabAxis.UseVisualStyleBackColor = true;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new System.Drawing.Point(3, 142);
            label14.Name = "label14";
            label14.Size = new System.Drawing.Size(182, 20);
            label14.TabIndex = 9;
            label14.Text = "Y Axis Format (e.g. N1, P1)";
            // 
            // txtYFormat
            // 
            txtYFormat.Location = new System.Drawing.Point(205, 139);
            txtYFormat.Name = "txtYFormat";
            txtYFormat.Size = new System.Drawing.Size(151, 27);
            txtYFormat.TabIndex = 8;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new System.Drawing.Point(3, 109);
            label13.Name = "label13";
            label13.Size = new System.Drawing.Size(80, 20);
            label13.TabIndex = 7;
            label13.Text = "Y Axis Max";
            // 
            // txtYMax
            // 
            txtYMax.Location = new System.Drawing.Point(205, 106);
            txtYMax.Name = "txtYMax";
            txtYMax.Size = new System.Drawing.Size(151, 27);
            txtYMax.TabIndex = 6;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new System.Drawing.Point(3, 76);
            label12.Name = "label12";
            label12.Size = new System.Drawing.Size(77, 20);
            label12.TabIndex = 5;
            label12.Text = "Y Axis Min";
            // 
            // txtYMin
            // 
            txtYMin.Location = new System.Drawing.Point(205, 73);
            txtYMin.Name = "txtYMin";
            txtYMin.Size = new System.Drawing.Size(151, 27);
            txtYMin.TabIndex = 4;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new System.Drawing.Point(3, 42);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(88, 20);
            label11.TabIndex = 3;
            label11.Text = "Y Axis Label";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(3, 9);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(89, 20);
            label10.TabIndex = 2;
            label10.Text = "X Axis Label";
            // 
            // txtYLabel
            // 
            txtYLabel.Location = new System.Drawing.Point(205, 39);
            txtYLabel.Name = "txtYLabel";
            txtYLabel.Size = new System.Drawing.Size(151, 27);
            txtYLabel.TabIndex = 1;
            // 
            // txtXLabel
            // 
            txtXLabel.Location = new System.Drawing.Point(205, 6);
            txtXLabel.Name = "txtXLabel";
            txtXLabel.Size = new System.Drawing.Size(151, 27);
            txtXLabel.TabIndex = 0;
            // 
            // bttnAdd
            // 
            bttnAdd.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnAdd.Location = new System.Drawing.Point(1015, 409);
            bttnAdd.Name = "bttnAdd";
            bttnAdd.Size = new System.Drawing.Size(94, 29);
            bttnAdd.TabIndex = 8;
            bttnAdd.Text = "&Add";
            bttnAdd.UseVisualStyleBackColor = true;
            bttnAdd.Click += Add_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.Location = new System.Drawing.Point(915, 409);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 9;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += Cancel_Click;
            // 
            // ConfigureChart
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1136, 450);
            Controls.Add(bttnCancel);
            Controls.Add(bttnAdd);
            Controls.Add(tabConfig);
            Controls.Add(pnlChart);
            Controls.Add(label2);
            Controls.Add(cboChartType);
            Controls.Add(label1);
            Controls.Add(cboResults);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "ConfigureChart";
            Text = "Configure Chart";
            Load += ConfigureChart_Load;
            tabConfig.ResumeLayout(false);
            tabData.ResumeLayout(false);
            tabData.PerformLayout();
            tabPieData.ResumeLayout(false);
            tabPieData.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numRadius).EndInit();
            tabOptions.ResumeLayout(false);
            tabOptions.PerformLayout();
            tabAxis.ResumeLayout(false);
            tabAxis.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ComboBox cboResults;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cboChartType;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cboXCol;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckedListBox chkMetrics;
        private System.Windows.Forms.Panel pnlChart;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cboSeries;
        private System.Windows.Forms.TabControl tabConfig;
        private System.Windows.Forms.TabPage tabData;
        private System.Windows.Forms.TabPage tabPieData;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cboPieValueColumn;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cboPieCategory;
        private System.Windows.Forms.TabPage tabOptions;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtTitle;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox cboLegend;
        private System.Windows.Forms.TabPage tabAxis;
        private System.Windows.Forms.TextBox txtYLabel;
        private System.Windows.Forms.TextBox txtXLabel;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtYMax;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtYMin;
        private System.Windows.Forms.TextBox txtYFormat;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.CheckBox chkLineFill;
        private System.Windows.Forms.TextBox txtPointSize;
        private System.Windows.Forms.Label lblPointSize;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.CheckBox chkDoughnut;
        private System.Windows.Forms.NumericUpDown numRadius;
        private System.Windows.Forms.Button bttnAdd;
        private System.Windows.Forms.Button bttnCancel;
    }
}