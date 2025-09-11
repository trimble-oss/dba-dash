namespace DBADashGUI.CustomReports
{
    partial class Pickers
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
            components = new System.ComponentModel.Container();
            cboParams = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            dgv = new System.Windows.Forms.DataGridView();
            bttnSave = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            txtName = new System.Windows.Forms.TextBox();
            lblName = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            txtDefault = new System.Windows.Forms.TextBox();
            groupBox1 = new System.Windows.Forms.GroupBox();
            chkMenuBar = new System.Windows.Forms.CheckBox();
            optText = new System.Windows.Forms.RadioButton();
            pnlQuery = new System.Windows.Forms.Panel();
            label3 = new System.Windows.Forms.Label();
            label5 = new System.Windows.Forms.Label();
            txtProcedureName = new System.Windows.Forms.TextBox();
            txtDisplayColumn = new System.Windows.Forms.TextBox();
            txtValueColumn = new System.Windows.Forms.TextBox();
            label4 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            optQuery = new System.Windows.Forms.RadioButton();
            optStandard = new System.Windows.Forms.RadioButton();
            groupBox2 = new System.Windows.Forms.GroupBox();
            lblDataType = new System.Windows.Forms.Label();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            groupBox1.SuspendLayout();
            pnlQuery.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // cboParams
            // 
            cboParams.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboParams.FormattingEnabled = true;
            cboParams.Location = new System.Drawing.Point(155, 26);
            cboParams.Name = "cboParams";
            cboParams.Size = new System.Drawing.Size(295, 28);
            cboParams.TabIndex = 0;
            cboParams.SelectedIndexChanged += CboParams_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(16, 29);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(76, 20);
            label1.TabIndex = 1;
            label1.Text = "Parameter";
            // 
            // dgv
            // 
            dgv.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Location = new System.Drawing.Point(16, 162);
            dgv.Name = "dgv";
            dgv.RowHeadersWidth = 51;
            dgv.Size = new System.Drawing.Size(434, 168);
            dgv.TabIndex = 16;
            dgv.CellContentClick += CellContentClick;
            dgv.DataError += DataError;
            dgv.RowValidated += RowValidated;
            dgv.RowValidating += RowValidating;
            // 
            // bttnSave
            // 
            bttnSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnSave.Location = new System.Drawing.Point(368, 456);
            bttnSave.Name = "bttnSave";
            bttnSave.Size = new System.Drawing.Size(94, 29);
            bttnSave.TabIndex = 18;
            bttnSave.Text = "&Save";
            bttnSave.UseVisualStyleBackColor = true;
            bttnSave.Click += Save_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.Location = new System.Drawing.Point(268, 456);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 17;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += Cancel_Click;
            // 
            // txtName
            // 
            txtName.Location = new System.Drawing.Point(155, 30);
            txtName.Name = "txtName";
            txtName.Size = new System.Drawing.Size(295, 27);
            txtName.TabIndex = 5;
            txtName.Validated += TxtName_Validated;
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.Location = new System.Drawing.Point(16, 33);
            lblName.Name = "lblName";
            lblName.Size = new System.Drawing.Size(95, 20);
            lblName.TabIndex = 6;
            lblName.Text = "Picker Name:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(16, 66);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(104, 20);
            label2.TabIndex = 7;
            label2.Text = "Picker Default:";
            // 
            // txtDefault
            // 
            txtDefault.Location = new System.Drawing.Point(155, 63);
            txtDefault.Name = "txtDefault";
            txtDefault.Size = new System.Drawing.Size(295, 27);
            txtDefault.TabIndex = 8;
            txtDefault.Validating += TxtDefault_Validating;
            txtDefault.Validated += TxtDefault_Validated;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox1.Controls.Add(chkMenuBar);
            groupBox1.Controls.Add(optText);
            groupBox1.Controls.Add(pnlQuery);
            groupBox1.Controls.Add(label6);
            groupBox1.Controls.Add(dgv);
            groupBox1.Controls.Add(optQuery);
            groupBox1.Controls.Add(optStandard);
            groupBox1.Controls.Add(txtDefault);
            groupBox1.Controls.Add(txtName);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(lblName);
            groupBox1.Location = new System.Drawing.Point(12, 114);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(469, 336);
            groupBox1.TabIndex = 9;
            groupBox1.TabStop = false;
            groupBox1.Text = "Picker Options";
            // 
            // chkMenuBar
            // 
            chkMenuBar.AutoSize = true;
            chkMenuBar.Location = new System.Drawing.Point(155, 132);
            chkMenuBar.Name = "chkMenuBar";
            chkMenuBar.Size = new System.Drawing.Size(94, 24);
            chkMenuBar.TabIndex = 19;
            chkMenuBar.Tag = "Display picker directly on the menu";
            chkMenuBar.Text = "Menu Bar";
            chkMenuBar.UseVisualStyleBackColor = true;
            chkMenuBar.Click += MenuBar_Click;
            // 
            // optText
            // 
            optText.AutoSize = true;
            optText.Location = new System.Drawing.Point(326, 96);
            optText.Name = "optText";
            optText.Size = new System.Drawing.Size(57, 24);
            optText.TabIndex = 19;
            optText.TabStop = true;
            optText.Text = "Text";
            optText.UseVisualStyleBackColor = true;
            optText.Click += Type_Change;
            // 
            // pnlQuery
            // 
            pnlQuery.Controls.Add(label3);
            pnlQuery.Controls.Add(label5);
            pnlQuery.Controls.Add(txtProcedureName);
            pnlQuery.Controls.Add(txtDisplayColumn);
            pnlQuery.Controls.Add(txtValueColumn);
            pnlQuery.Controls.Add(label4);
            pnlQuery.Location = new System.Drawing.Point(6, 162);
            pnlQuery.Name = "pnlQuery";
            pnlQuery.Size = new System.Drawing.Size(457, 111);
            pnlQuery.TabIndex = 17;
            pnlQuery.Visible = false;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(10, 13);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(123, 20);
            label3.TabIndex = 12;
            label3.Text = "Procedure Name:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(10, 79);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(116, 20);
            label5.TabIndex = 16;
            label5.Text = "Display Column:";
            // 
            // txtProcedureName
            // 
            txtProcedureName.Location = new System.Drawing.Point(149, 10);
            txtProcedureName.Name = "txtProcedureName";
            txtProcedureName.Size = new System.Drawing.Size(295, 27);
            txtProcedureName.TabIndex = 11;
            txtProcedureName.Validated += TxtProcedureName_Validated;
            // 
            // txtDisplayColumn
            // 
            txtDisplayColumn.Location = new System.Drawing.Point(149, 76);
            txtDisplayColumn.Name = "txtDisplayColumn";
            txtDisplayColumn.Size = new System.Drawing.Size(295, 27);
            txtDisplayColumn.TabIndex = 15;
            txtDisplayColumn.Validated += TxtDisplayColumn_Validated;
            // 
            // txtValueColumn
            // 
            txtValueColumn.Location = new System.Drawing.Point(149, 43);
            txtValueColumn.Name = "txtValueColumn";
            txtValueColumn.Size = new System.Drawing.Size(295, 27);
            txtValueColumn.TabIndex = 13;
            txtValueColumn.Validated += TxtValueColumn_Validated;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(10, 46);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(103, 20);
            label4.TabIndex = 14;
            label4.Text = "Value Column:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(16, 98);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(43, 20);
            label6.TabIndex = 18;
            label6.Text = "Type:";
            // 
            // optQuery
            // 
            optQuery.AutoSize = true;
            optQuery.Location = new System.Drawing.Point(251, 96);
            optQuery.Name = "optQuery";
            optQuery.Size = new System.Drawing.Size(69, 24);
            optQuery.TabIndex = 10;
            optQuery.TabStop = true;
            optQuery.Text = "Query";
            optQuery.UseVisualStyleBackColor = true;
            optQuery.Click += Type_Change;
            // 
            // optStandard
            // 
            optStandard.AutoSize = true;
            optStandard.Checked = true;
            optStandard.Location = new System.Drawing.Point(155, 96);
            optStandard.Name = "optStandard";
            optStandard.Size = new System.Drawing.Size(90, 24);
            optStandard.TabIndex = 9;
            optStandard.TabStop = true;
            optStandard.Text = "Standard";
            optStandard.UseVisualStyleBackColor = true;
            optStandard.Click += Type_Change;
            // 
            // groupBox2
            // 
            groupBox2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox2.Controls.Add(lblDataType);
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(cboParams);
            groupBox2.Location = new System.Drawing.Point(12, 13);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(469, 94);
            groupBox2.TabIndex = 10;
            groupBox2.TabStop = false;
            groupBox2.Text = "Select Parameter";
            // 
            // lblDataType
            // 
            lblDataType.AutoSize = true;
            lblDataType.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            lblDataType.Location = new System.Drawing.Point(155, 57);
            lblDataType.Name = "lblDataType";
            lblDataType.Size = new System.Drawing.Size(78, 20);
            lblDataType.TabIndex = 19;
            lblDataType.Text = "Data Type:";
            // 
            // Pickers
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(498, 497);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(bttnCancel);
            Controls.Add(bttnSave);
            MinimumSize = new System.Drawing.Size(380, 450);
            Name = "Pickers";
            ShowIcon = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Pickers";
            Load += Pickers_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            pnlQuery.ResumeLayout(false);
            pnlQuery.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.ComboBox cboParams;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.Button bttnSave;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtDefault;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton optQuery;
        private System.Windows.Forms.RadioButton optStandard;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Panel pnlQuery;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtProcedureName;
        private System.Windows.Forms.TextBox txtDisplayColumn;
        private System.Windows.Forms.TextBox txtValueColumn;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lblDataType;
        private System.Windows.Forms.RadioButton optText;
        private System.Windows.Forms.CheckBox chkMenuBar;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}