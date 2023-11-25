using DBADashGUI.Theme;

namespace DBADashGUI.CustomReports
{
    partial class CellHighlightingRulesConfig
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CellHighlightingRulesConfig));
            dgv = new System.Windows.Forms.DataGridView();
            cboTargetColumn = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            txtColumn = new System.Windows.Forms.TextBox();
            label9 = new System.Windows.Forms.Label();
            txtValue1 = new System.Windows.Forms.TextBox();
            cboConditionType = new System.Windows.Forms.ComboBox();
            txtValue2 = new System.Windows.Forms.TextBox();
            lblAnd = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            bttnAdd = new System.Windows.Forms.Button();
            bttnUpdate = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            grpRule = new System.Windows.Forms.GroupBox();
            chkCaseSensitive = new System.Windows.Forms.CheckBox();
            groupBox2 = new System.Windows.Forms.GroupBox();
            IsStatusColumn = new System.Windows.Forms.CheckBox();
            bttnBackColorDarkIncrease = new System.Windows.Forms.Button();
            bttnBackColorDarkDecrease = new System.Windows.Forms.Button();
            bttnForeColorDarkIncrease = new System.Windows.Forms.Button();
            bttnForeColorDarkDecrease = new System.Windows.Forms.Button();
            bttnBackColorIncrease = new System.Windows.Forms.Button();
            bttnBackColorDecrease = new System.Windows.Forms.Button();
            bttnForeColorIncrease = new System.Windows.Forms.Button();
            bttnForeColorDecrease = new System.Windows.Forms.Button();
            label2 = new System.Windows.Forms.Label();
            lblFont = new System.Windows.Forms.Label();
            lblForeColorDark = new System.Windows.Forms.Label();
            txtBackColorDark = new System.Windows.Forms.TextBox();
            lblBackColorDark = new System.Windows.Forms.Label();
            txtForeColorDark = new System.Windows.Forms.TextBox();
            pnlForeColorDark = new System.Windows.Forms.Panel();
            pnlBackColorDark = new System.Windows.Forms.Panel();
            bttnClearFont = new System.Windows.Forms.Button();
            bttnSetFont = new System.Windows.Forms.Button();
            label4 = new System.Windows.Forms.Label();
            txtBackColor = new System.Windows.Forms.TextBox();
            label5 = new System.Windows.Forms.Label();
            txtForeColor = new System.Windows.Forms.TextBox();
            pnlForeColor = new System.Windows.Forms.Panel();
            pnlBackColor = new System.Windows.Forms.Panel();
            chkConfigureDark = new System.Windows.Forms.CheckBox();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsCopy = new System.Windows.Forms.ToolStripButton();
            tsPaste = new System.Windows.Forms.ToolStripButton();
            tsGradient = new System.Windows.Forms.ToolStripButton();
            tsClear = new System.Windows.Forms.ToolStripButton();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            cboStatus = new System.Windows.Forms.ComboBox();
            label6 = new System.Windows.Forms.Label();
            label7 = new System.Windows.Forms.Label();
            grpStatus = new System.Windows.Forms.GroupBox();
            tabFormatting = new ThemedTabControl();
            tabStatus = new System.Windows.Forms.TabPage();
            tabCustom = new System.Windows.Forms.TabPage();
            ((System.ComponentModel.ISupportInitialize)dgv).BeginInit();
            grpRule.SuspendLayout();
            groupBox2.SuspendLayout();
            toolStrip1.SuspendLayout();
            grpStatus.SuspendLayout();
            tabFormatting.SuspendLayout();
            tabStatus.SuspendLayout();
            tabCustom.SuspendLayout();
            SuspendLayout();
            // 
            // dgv
            // 
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgv.Location = new System.Drawing.Point(12, 437);
            dgv.Name = "dgv";
            dgv.RowHeadersVisible = false;
            dgv.RowHeadersWidth = 51;
            dgv.RowTemplate.Height = 29;
            dgv.Size = new System.Drawing.Size(884, 274);
            dgv.TabIndex = 0;
            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.CellFormatting += Dgv_CellFormatting;
            dgv.CellMouseMove += Dgv_CellMouseMove;
            dgv.CellPainting += Dgv_CellPainting;
            dgv.RowsAdded += Dgv_RowsAdded;
            // 
            // cboTargetColumn
            // 
            cboTargetColumn.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboTargetColumn.FormattingEnabled = true;
            cboTargetColumn.Location = new System.Drawing.Point(120, 26);
            cboTargetColumn.Name = "cboTargetColumn";
            cboTargetColumn.Size = new System.Drawing.Size(151, 28);
            cboTargetColumn.TabIndex = 1;
            cboTargetColumn.SelectedIndexChanged += CboTargetColumn_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(6, 32);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(108, 20);
            label1.TabIndex = 2;
            label1.Tag = "";
            label1.Text = "Target Column:";
            toolTip1.SetToolTip(label1, "Target column is the column the rule will be evaulated against.  This will usually be the same column you are formatting.");
            // 
            // txtColumn
            // 
            txtColumn.Enabled = false;
            txtColumn.Location = new System.Drawing.Point(419, 29);
            txtColumn.Name = "txtColumn";
            txtColumn.ReadOnly = true;
            txtColumn.Size = new System.Drawing.Size(125, 27);
            txtColumn.TabIndex = 16;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new System.Drawing.Point(277, 32);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(136, 20);
            label9.TabIndex = 17;
            label9.Text = "Formatted Column:";
            toolTip1.SetToolTip(label9, resources.GetString("label9.ToolTip"));
            // 
            // txtValue1
            // 
            txtValue1.Location = new System.Drawing.Point(277, 29);
            txtValue1.Name = "txtValue1";
            txtValue1.Size = new System.Drawing.Size(106, 27);
            txtValue1.TabIndex = 18;
            // 
            // cboConditionType
            // 
            cboConditionType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboConditionType.FormattingEnabled = true;
            cboConditionType.Location = new System.Drawing.Point(120, 29);
            cboConditionType.Name = "cboConditionType";
            cboConditionType.Size = new System.Drawing.Size(151, 28);
            cboConditionType.TabIndex = 19;
            cboConditionType.SelectedIndexChanged += CboConditionType_SelectedIndexChanged;
            // 
            // txtValue2
            // 
            txtValue2.Location = new System.Drawing.Point(436, 29);
            txtValue2.Name = "txtValue2";
            txtValue2.Size = new System.Drawing.Size(106, 27);
            txtValue2.TabIndex = 20;
            txtValue2.Visible = false;
            // 
            // lblAnd
            // 
            lblAnd.AutoSize = true;
            lblAnd.Location = new System.Drawing.Point(389, 33);
            lblAnd.Name = "lblAnd";
            lblAnd.Size = new System.Drawing.Size(34, 20);
            lblAnd.TabIndex = 21;
            lblAnd.Text = "and";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(6, 32);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(38, 20);
            label3.TabIndex = 22;
            label3.Text = "Rule";
            toolTip1.SetToolTip(label3, resources.GetString("label3.ToolTip"));
            // 
            // bttnAdd
            // 
            bttnAdd.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            bttnAdd.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            bttnAdd.Location = new System.Drawing.Point(802, 395);
            bttnAdd.Name = "bttnAdd";
            bttnAdd.Size = new System.Drawing.Size(94, 36);
            bttnAdd.TabIndex = 29;
            bttnAdd.Text = "Add";
            bttnAdd.UseVisualStyleBackColor = true;
            bttnAdd.Click += BttnAdd_Click;
            // 
            // bttnUpdate
            // 
            bttnUpdate.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnUpdate.Location = new System.Drawing.Point(802, 726);
            bttnUpdate.Name = "bttnUpdate";
            bttnUpdate.Size = new System.Drawing.Size(94, 38);
            bttnUpdate.TabIndex = 30;
            bttnUpdate.Text = "&Update";
            bttnUpdate.UseVisualStyleBackColor = true;
            bttnUpdate.Click += BttnUpdate_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.Location = new System.Drawing.Point(702, 726);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 38);
            bttnCancel.TabIndex = 31;
            bttnCancel.Text = "Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += BttnCancel_Click;
            // 
            // grpRule
            // 
            grpRule.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            grpRule.Controls.Add(chkCaseSensitive);
            grpRule.Controls.Add(label3);
            grpRule.Controls.Add(txtValue1);
            grpRule.Controls.Add(cboConditionType);
            grpRule.Controls.Add(txtValue2);
            grpRule.Controls.Add(lblAnd);
            grpRule.Location = new System.Drawing.Point(12, 112);
            grpRule.Name = "grpRule";
            grpRule.Size = new System.Drawing.Size(884, 97);
            grpRule.TabIndex = 32;
            grpRule.TabStop = false;
            grpRule.Text = "Rule";
            // 
            // chkCaseSensitive
            // 
            chkCaseSensitive.AutoSize = true;
            chkCaseSensitive.Location = new System.Drawing.Point(120, 62);
            chkCaseSensitive.Name = "chkCaseSensitive";
            chkCaseSensitive.Size = new System.Drawing.Size(124, 24);
            chkCaseSensitive.TabIndex = 23;
            chkCaseSensitive.Text = "Case Sensitive";
            toolTip1.SetToolTip(chkCaseSensitive, "Option for string comparisons to be case sensitive");
            chkCaseSensitive.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            groupBox2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox2.Controls.Add(IsStatusColumn);
            groupBox2.Controls.Add(label9);
            groupBox2.Controls.Add(cboTargetColumn);
            groupBox2.Controls.Add(label1);
            groupBox2.Controls.Add(txtColumn);
            groupBox2.Location = new System.Drawing.Point(12, 37);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(884, 69);
            groupBox2.TabIndex = 33;
            groupBox2.TabStop = false;
            groupBox2.Text = "Column";
            // 
            // IsStatusColumn
            // 
            IsStatusColumn.AutoSize = true;
            IsStatusColumn.Location = new System.Drawing.Point(596, 32);
            IsStatusColumn.Name = "IsStatusColumn";
            IsStatusColumn.Size = new System.Drawing.Size(218, 24);
            IsStatusColumn.TabIndex = 18;
            IsStatusColumn.Text = "Is DBA Dash Status Column?";
            toolTip1.SetToolTip(IsStatusColumn, resources.GetString("IsStatusColumn.ToolTip"));
            IsStatusColumn.UseVisualStyleBackColor = true;
            IsStatusColumn.CheckedChanged += IsStatusColumn_CheckedChanged;
            // 
            // bttnBackColorDarkIncrease
            // 
            bttnBackColorDarkIncrease.Image = Properties.Resources.IncreaseImageBrightness_16x;
            bttnBackColorDarkIncrease.Location = new System.Drawing.Point(680, 46);
            bttnBackColorDarkIncrease.Name = "bttnBackColorDarkIncrease";
            bttnBackColorDarkIncrease.Size = new System.Drawing.Size(31, 29);
            bttnBackColorDarkIncrease.TabIndex = 57;
            toolTip1.SetToolTip(bttnBackColorDarkIncrease, "Increase Brightness");
            bttnBackColorDarkIncrease.UseVisualStyleBackColor = true;
            bttnBackColorDarkIncrease.Visible = false;
            bttnBackColorDarkIncrease.Click += BttnBackColorDarkIncrease_Click;
            // 
            // bttnBackColorDarkDecrease
            // 
            bttnBackColorDarkDecrease.Image = Properties.Resources.ReduceImageBrightness_16x;
            bttnBackColorDarkDecrease.Location = new System.Drawing.Point(717, 46);
            bttnBackColorDarkDecrease.Name = "bttnBackColorDarkDecrease";
            bttnBackColorDarkDecrease.Size = new System.Drawing.Size(31, 29);
            bttnBackColorDarkDecrease.TabIndex = 56;
            toolTip1.SetToolTip(bttnBackColorDarkDecrease, "Decrease Brightness");
            bttnBackColorDarkDecrease.UseVisualStyleBackColor = true;
            bttnBackColorDarkDecrease.Visible = false;
            bttnBackColorDarkDecrease.Click += BttnBackColorDarkDecrease_Click;
            // 
            // bttnForeColorDarkIncrease
            // 
            bttnForeColorDarkIncrease.Image = Properties.Resources.IncreaseImageBrightness_16x;
            bttnForeColorDarkIncrease.Location = new System.Drawing.Point(680, 14);
            bttnForeColorDarkIncrease.Name = "bttnForeColorDarkIncrease";
            bttnForeColorDarkIncrease.Size = new System.Drawing.Size(31, 29);
            bttnForeColorDarkIncrease.TabIndex = 55;
            toolTip1.SetToolTip(bttnForeColorDarkIncrease, "Increase Brightness");
            bttnForeColorDarkIncrease.UseVisualStyleBackColor = true;
            bttnForeColorDarkIncrease.Visible = false;
            bttnForeColorDarkIncrease.Click += BttnForColorDarkIncrease_Click;
            // 
            // bttnForeColorDarkDecrease
            // 
            bttnForeColorDarkDecrease.Image = Properties.Resources.ReduceImageBrightness_16x;
            bttnForeColorDarkDecrease.Location = new System.Drawing.Point(717, 14);
            bttnForeColorDarkDecrease.Name = "bttnForeColorDarkDecrease";
            bttnForeColorDarkDecrease.Size = new System.Drawing.Size(31, 29);
            bttnForeColorDarkDecrease.TabIndex = 54;
            toolTip1.SetToolTip(bttnForeColorDarkDecrease, "Decrease Brightness");
            bttnForeColorDarkDecrease.UseVisualStyleBackColor = true;
            bttnForeColorDarkDecrease.Visible = false;
            bttnForeColorDarkDecrease.Click += BttnForeColorDarkDecrease_Click;
            // 
            // bttnBackColorIncrease
            // 
            bttnBackColorIncrease.Image = Properties.Resources.IncreaseImageBrightness_16x;
            bttnBackColorIncrease.Location = new System.Drawing.Point(280, 43);
            bttnBackColorIncrease.Name = "bttnBackColorIncrease";
            bttnBackColorIncrease.Size = new System.Drawing.Size(31, 29);
            bttnBackColorIncrease.TabIndex = 53;
            toolTip1.SetToolTip(bttnBackColorIncrease, "Increase Brightness");
            bttnBackColorIncrease.UseVisualStyleBackColor = true;
            bttnBackColorIncrease.Click += BttnBackColorIncrease_Click;
            // 
            // bttnBackColorDecrease
            // 
            bttnBackColorDecrease.Image = Properties.Resources.ReduceImageBrightness_16x;
            bttnBackColorDecrease.Location = new System.Drawing.Point(317, 43);
            bttnBackColorDecrease.Name = "bttnBackColorDecrease";
            bttnBackColorDecrease.Size = new System.Drawing.Size(31, 29);
            bttnBackColorDecrease.TabIndex = 52;
            toolTip1.SetToolTip(bttnBackColorDecrease, "Decrease Brightness");
            bttnBackColorDecrease.UseVisualStyleBackColor = true;
            bttnBackColorDecrease.Click += BttnBackColorDecrease_Click;
            // 
            // bttnForeColorIncrease
            // 
            bttnForeColorIncrease.Image = Properties.Resources.IncreaseImageBrightness_16x;
            bttnForeColorIncrease.Location = new System.Drawing.Point(280, 11);
            bttnForeColorIncrease.Name = "bttnForeColorIncrease";
            bttnForeColorIncrease.Size = new System.Drawing.Size(31, 29);
            bttnForeColorIncrease.TabIndex = 51;
            toolTip1.SetToolTip(bttnForeColorIncrease, "Increase Brightness");
            bttnForeColorIncrease.UseVisualStyleBackColor = true;
            bttnForeColorIncrease.Click += BttnForeColorIncrease_Click;
            // 
            // bttnForeColorDecrease
            // 
            bttnForeColorDecrease.Image = Properties.Resources.ReduceImageBrightness_16x;
            bttnForeColorDecrease.Location = new System.Drawing.Point(317, 11);
            bttnForeColorDecrease.Name = "bttnForeColorDecrease";
            bttnForeColorDecrease.Size = new System.Drawing.Size(31, 29);
            bttnForeColorDecrease.TabIndex = 50;
            toolTip1.SetToolTip(bttnForeColorDecrease, "Decrease Brightness");
            bttnForeColorDecrease.UseVisualStyleBackColor = true;
            bttnForeColorDecrease.Click += BttnForeColorDecrease_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(6, 85);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(38, 20);
            label2.TabIndex = 39;
            label2.Text = "Font";
            // 
            // lblFont
            // 
            lblFont.Location = new System.Drawing.Point(120, 85);
            lblFont.Name = "lblFont";
            lblFont.Size = new System.Drawing.Size(424, 36);
            lblFont.TabIndex = 38;
            lblFont.Text = "{Default}";
            // 
            // lblForeColorDark
            // 
            lblForeColorDark.AutoSize = true;
            lblForeColorDark.Location = new System.Drawing.Point(389, 15);
            lblForeColorDark.Name = "lblForeColorDark";
            lblForeColorDark.Size = new System.Drawing.Size(123, 20);
            lblForeColorDark.TabIndex = 32;
            lblForeColorDark.Text = "Fore Color (Dark)";
            toolTip1.SetToolTip(lblForeColorDark, "Specify a color for Text in dark mode theme or leave blank to use the same colors across themes");
            lblForeColorDark.Visible = false;
            // 
            // txtBackColorDark
            // 
            txtBackColorDark.Location = new System.Drawing.Point(518, 45);
            txtBackColorDark.Name = "txtBackColorDark";
            txtBackColorDark.Size = new System.Drawing.Size(125, 27);
            txtBackColorDark.TabIndex = 34;
            txtBackColorDark.Visible = false;
            txtBackColorDark.TextChanged += TxtBackColorDark_TextChanged;
            // 
            // lblBackColorDark
            // 
            lblBackColorDark.AutoSize = true;
            lblBackColorDark.Location = new System.Drawing.Point(389, 48);
            lblBackColorDark.Name = "lblBackColorDark";
            lblBackColorDark.Size = new System.Drawing.Size(125, 20);
            lblBackColorDark.TabIndex = 33;
            lblBackColorDark.Text = "Back Color (Dark)";
            toolTip1.SetToolTip(lblBackColorDark, "Specify a color for cell background in dark mode theme or leave blank to use the same colors across themes");
            lblBackColorDark.Visible = false;
            // 
            // txtForeColorDark
            // 
            txtForeColorDark.Location = new System.Drawing.Point(518, 12);
            txtForeColorDark.Name = "txtForeColorDark";
            txtForeColorDark.Size = new System.Drawing.Size(125, 27);
            txtForeColorDark.TabIndex = 31;
            txtForeColorDark.Visible = false;
            txtForeColorDark.TextChanged += TxtForeColorDark_TextChanged;
            // 
            // pnlForeColorDark
            // 
            pnlForeColorDark.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pnlForeColorDark.Cursor = System.Windows.Forms.Cursors.Hand;
            pnlForeColorDark.Location = new System.Drawing.Point(651, 16);
            pnlForeColorDark.Name = "pnlForeColorDark";
            pnlForeColorDark.Size = new System.Drawing.Size(23, 23);
            pnlForeColorDark.TabIndex = 35;
            toolTip1.SetToolTip(pnlForeColorDark, "Click to select color");
            pnlForeColorDark.Visible = false;
            pnlForeColorDark.Click += PnlForeColorDark_Click;
            // 
            // pnlBackColorDark
            // 
            pnlBackColorDark.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pnlBackColorDark.Cursor = System.Windows.Forms.Cursors.Hand;
            pnlBackColorDark.Location = new System.Drawing.Point(651, 49);
            pnlBackColorDark.Name = "pnlBackColorDark";
            pnlBackColorDark.Size = new System.Drawing.Size(23, 23);
            pnlBackColorDark.TabIndex = 36;
            toolTip1.SetToolTip(pnlBackColorDark, "Click to select color");
            pnlBackColorDark.Visible = false;
            pnlBackColorDark.Click += PnlBackColorDark_Click;
            // 
            // bttnClearFont
            // 
            bttnClearFont.Enabled = false;
            bttnClearFont.Location = new System.Drawing.Point(650, 81);
            bttnClearFont.Name = "bttnClearFont";
            bttnClearFont.Size = new System.Drawing.Size(98, 29);
            bttnClearFont.TabIndex = 30;
            bttnClearFont.Text = "Clear Font";
            toolTip1.SetToolTip(bttnClearFont, "Clear selected font/font style choice.");
            bttnClearFont.UseVisualStyleBackColor = true;
            bttnClearFont.Click += ClearFont_Click;
            // 
            // bttnSetFont
            // 
            bttnSetFont.Location = new System.Drawing.Point(550, 81);
            bttnSetFont.Name = "bttnSetFont";
            bttnSetFont.Size = new System.Drawing.Size(98, 29);
            bttnSetFont.TabIndex = 29;
            bttnSetFont.Text = "Set Font";
            toolTip1.SetToolTip(bttnSetFont, "Option to use a different font or font style");
            bttnSetFont.UseVisualStyleBackColor = true;
            bttnSetFont.Click += SetFont_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(6, 13);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(78, 20);
            label4.TabIndex = 24;
            label4.Text = "Fore Color";
            toolTip1.SetToolTip(label4, "Specify a color for Text or leave blank for default");
            // 
            // txtBackColor
            // 
            txtBackColor.Location = new System.Drawing.Point(119, 43);
            txtBackColor.Name = "txtBackColor";
            txtBackColor.Size = new System.Drawing.Size(125, 27);
            txtBackColor.TabIndex = 26;
            txtBackColor.TextChanged += TxtBackColor_TextChanged;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(6, 46);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(80, 20);
            label5.TabIndex = 25;
            label5.Text = "Back Color";
            toolTip1.SetToolTip(label5, "Specify a color for cell background or leave blank for default");
            // 
            // txtForeColor
            // 
            txtForeColor.Location = new System.Drawing.Point(120, 10);
            txtForeColor.Name = "txtForeColor";
            txtForeColor.Size = new System.Drawing.Size(125, 27);
            txtForeColor.TabIndex = 23;
            txtForeColor.TextChanged += TxtForeColor_TextChanged;
            // 
            // pnlForeColor
            // 
            pnlForeColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pnlForeColor.Cursor = System.Windows.Forms.Cursors.Hand;
            pnlForeColor.Location = new System.Drawing.Point(251, 14);
            pnlForeColor.Name = "pnlForeColor";
            pnlForeColor.Size = new System.Drawing.Size(23, 23);
            pnlForeColor.TabIndex = 27;
            toolTip1.SetToolTip(pnlForeColor, "Click to select color");
            pnlForeColor.Click += PnlForeColor_Click;
            // 
            // pnlBackColor
            // 
            pnlBackColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pnlBackColor.Cursor = System.Windows.Forms.Cursors.Hand;
            pnlBackColor.Location = new System.Drawing.Point(251, 45);
            pnlBackColor.Name = "pnlBackColor";
            pnlBackColor.Size = new System.Drawing.Size(23, 23);
            pnlBackColor.TabIndex = 28;
            toolTip1.SetToolTip(pnlBackColor, "Click to select color");
            pnlBackColor.Click += PnlBackColor_Click;
            // 
            // chkConfigureDark
            // 
            chkConfigureDark.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            chkConfigureDark.AutoSize = true;
            chkConfigureDark.Location = new System.Drawing.Point(12, 726);
            chkConfigureDark.Name = "chkConfigureDark";
            chkConfigureDark.Size = new System.Drawing.Size(174, 24);
            chkConfigureDark.TabIndex = 37;
            chkConfigureDark.Text = "Configure Dark Mode";
            toolTip1.SetToolTip(chkConfigureDark, "Option to configure separate colors for dark mode theme");
            chkConfigureDark.UseVisualStyleBackColor = true;
            chkConfigureDark.CheckedChanged += ChkConfigureDark_CheckedChanged;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsCopy, tsPaste, tsGradient, tsClear });
            toolStrip1.Location = new System.Drawing.Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(908, 27);
            toolStrip1.TabIndex = 35;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsCopy
            // 
            tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsCopy.Image = Properties.Resources.ASX_Copy_blue_16x;
            tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsCopy.Name = "tsCopy";
            tsCopy.Size = new System.Drawing.Size(29, 24);
            tsCopy.Text = "Copy Rule Set";
            tsCopy.ToolTipText = "Copy the rules and formatting defined here so it can be quickly be applied to another column.";
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
            tsPaste.Text = "Paste Rule Set";
            tsPaste.ToolTipText = "Paste rules and formatting that was previously copied.  This will replace existing rules and formatting.  ";
            tsPaste.Click += TsPaste_Click;
            // 
            // tsGradient
            // 
            tsGradient.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsGradient.Image = Properties.Resources.GradientLinear_18x16;
            tsGradient.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsGradient.Name = "tsGradient";
            tsGradient.Size = new System.Drawing.Size(29, 24);
            tsGradient.Text = "Generate Gradient";
            tsGradient.ToolTipText = "Automatically create rules that will generate a gradient effect";
            tsGradient.Click += TsGradient_Click;
            // 
            // tsClear
            // 
            tsClear.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsClear.Image = Properties.Resources.Eraser_16x;
            tsClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsClear.Name = "tsClear";
            tsClear.Size = new System.Drawing.Size(29, 24);
            tsClear.Text = "Clear Rules";
            tsClear.ToolTipText = "Clear Rules.  This removes all rules and formatting options";
            tsClear.Click += TsClear_Click;
            // 
            // cboStatus
            // 
            cboStatus.FormattingEnabled = true;
            cboStatus.Location = new System.Drawing.Point(119, 41);
            cboStatus.Name = "cboStatus";
            cboStatus.Size = new System.Drawing.Size(152, 28);
            cboStatus.TabIndex = 0;
            cboStatus.SelectedIndexChanged += CboStatus_SelectedIndexChanged;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(14, 47);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(52, 20);
            label6.TabIndex = 1;
            label6.Text = "Status:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            label7.Location = new System.Drawing.Point(120, 111);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(545, 20);
            label7.TabIndex = 2;
            label7.Text = "Use this tab for basic status formatting or select custom formatting for more options";
            // 
            // grpStatus
            // 
            grpStatus.Controls.Add(label7);
            grpStatus.Controls.Add(label6);
            grpStatus.Controls.Add(cboStatus);
            grpStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            grpStatus.Location = new System.Drawing.Point(3, 3);
            grpStatus.Name = "grpStatus";
            grpStatus.Size = new System.Drawing.Size(870, 135);
            grpStatus.TabIndex = 38;
            grpStatus.TabStop = false;
            grpStatus.Text = "Status";
            // 
            // tabFormatting
            // 
            tabFormatting.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            tabFormatting.Controls.Add(tabStatus);
            tabFormatting.Controls.Add(tabCustom);
            tabFormatting.Location = new System.Drawing.Point(12, 215);
            tabFormatting.Name = "tabFormatting";
            tabFormatting.SelectedIndex = 0;
            tabFormatting.Size = new System.Drawing.Size(884, 174);
            tabFormatting.TabIndex = 40;
            // 
            // tabStatus
            // 
            tabStatus.Controls.Add(grpStatus);
            tabStatus.Location = new System.Drawing.Point(4, 29);
            tabStatus.Name = "tabStatus";
            tabStatus.Padding = new System.Windows.Forms.Padding(3);
            tabStatus.Size = new System.Drawing.Size(876, 141);
            tabStatus.TabIndex = 0;
            tabStatus.Text = "Status (Red, Amber, Green)";
            tabStatus.UseVisualStyleBackColor = true;
            // 
            // tabCustom
            // 
            tabCustom.Controls.Add(bttnBackColorDarkIncrease);
            tabCustom.Controls.Add(label4);
            tabCustom.Controls.Add(bttnBackColorDarkDecrease);
            tabCustom.Controls.Add(pnlBackColor);
            tabCustom.Controls.Add(bttnForeColorDarkIncrease);
            tabCustom.Controls.Add(pnlForeColor);
            tabCustom.Controls.Add(bttnForeColorDarkDecrease);
            tabCustom.Controls.Add(txtForeColor);
            tabCustom.Controls.Add(bttnBackColorIncrease);
            tabCustom.Controls.Add(label5);
            tabCustom.Controls.Add(bttnBackColorDecrease);
            tabCustom.Controls.Add(txtBackColor);
            tabCustom.Controls.Add(bttnForeColorIncrease);
            tabCustom.Controls.Add(bttnSetFont);
            tabCustom.Controls.Add(bttnForeColorDecrease);
            tabCustom.Controls.Add(bttnClearFont);
            tabCustom.Controls.Add(label2);
            tabCustom.Controls.Add(pnlBackColorDark);
            tabCustom.Controls.Add(lblFont);
            tabCustom.Controls.Add(pnlForeColorDark);
            tabCustom.Controls.Add(lblForeColorDark);
            tabCustom.Controls.Add(txtForeColorDark);
            tabCustom.Controls.Add(txtBackColorDark);
            tabCustom.Controls.Add(lblBackColorDark);
            tabCustom.Location = new System.Drawing.Point(4, 29);
            tabCustom.Name = "tabCustom";
            tabCustom.Padding = new System.Windows.Forms.Padding(3);
            tabCustom.Size = new System.Drawing.Size(876, 141);
            tabCustom.TabIndex = 1;
            tabCustom.Text = "Custom Formatting";
            tabCustom.UseVisualStyleBackColor = true;
            // 
            // CellHighlightingRulesConfig
            // 
            AcceptButton = bttnUpdate;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new System.Drawing.Size(908, 776);
            Controls.Add(tabFormatting);
            Controls.Add(toolStrip1);
            Controls.Add(groupBox2);
            Controls.Add(grpRule);
            Controls.Add(chkConfigureDark);
            Controls.Add(bttnCancel);
            Controls.Add(bttnAdd);
            Controls.Add(bttnUpdate);
            Controls.Add(dgv);
            MinimumSize = new System.Drawing.Size(844, 641);
            Name = "CellHighlightingRulesConfig";
            Text = "Cell Highlighting Rules";
            Load += CellHighlightingRulesConfig_Load;
            ((System.ComponentModel.ISupportInitialize)dgv).EndInit();
            grpRule.ResumeLayout(false);
            grpRule.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            grpStatus.ResumeLayout(false);
            grpStatus.PerformLayout();
            tabFormatting.ResumeLayout(false);
            tabStatus.ResumeLayout(false);
            tabCustom.ResumeLayout(false);
            tabCustom.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.ComboBox cboTargetColumn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtColumn;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtValue1;
        private System.Windows.Forms.ComboBox cboConditionType;
        private System.Windows.Forms.TextBox txtValue2;
        private System.Windows.Forms.Label lblAnd;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button bttnAdd;
        private System.Windows.Forms.Button bttnUpdate;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.GroupBox grpRule;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox chkCaseSensitive;
        private System.Windows.Forms.Button bttnSetFont;
        private System.Windows.Forms.Button bttnClearFont;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.ToolStripButton tsPaste;
        private System.Windows.Forms.ToolStripButton tsGradient;
        private System.Windows.Forms.ToolStripButton tsClear;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtBackColor;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtForeColor;
        private System.Windows.Forms.Panel pnlForeColor;
        private System.Windows.Forms.Panel pnlBackColor;
        private System.Windows.Forms.Label lblForeColorDark;
        private System.Windows.Forms.TextBox txtBackColorDark;
        private System.Windows.Forms.Label lblBackColorDark;
        private System.Windows.Forms.TextBox txtForeColorDark;
        private System.Windows.Forms.Panel pnlForeColorDark;
        private System.Windows.Forms.Panel pnlBackColorDark;
        private System.Windows.Forms.CheckBox chkConfigureDark;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label lblFont;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bttnBackColorDarkIncrease;
        private System.Windows.Forms.Button bttnBackColorDarkDecrease;
        private System.Windows.Forms.Button bttnForeColorDarkIncrease;
        private System.Windows.Forms.Button bttnForeColorDarkDecrease;
        private System.Windows.Forms.Button bttnBackColorIncrease;
        private System.Windows.Forms.Button bttnBackColorDecrease;
        private System.Windows.Forms.Button bttnForeColorIncrease;
        private System.Windows.Forms.Button bttnForeColorDecrease;
        private System.Windows.Forms.CheckBox IsStatusColumn;
        private System.Windows.Forms.GroupBox grpStatus;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cboStatus;
        private ThemedTabControl tabFormatting;
        private System.Windows.Forms.TabPage tabStatus;
        private System.Windows.Forms.TabPage tabCustom;
    }
}