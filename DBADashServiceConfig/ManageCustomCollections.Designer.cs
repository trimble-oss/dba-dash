namespace DBADashServiceConfig
{
    partial class ManageCustomCollections
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ManageCustomCollections));
            txtName = new System.Windows.Forms.TextBox();
            lblName = new System.Windows.Forms.Label();
            cboProcedureName = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            txtCron = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            numTimeout = new System.Windows.Forms.NumericUpDown();
            chkDefaultTimeout = new System.Windows.Forms.CheckBox();
            label3 = new System.Windows.Forms.Label();
            dgvCustom = new System.Windows.Forms.DataGridView();
            bttnAdd = new System.Windows.Forms.Button();
            chkRunOnStart = new System.Windows.Forms.CheckBox();
            bttnUpdate = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            lnkPreview = new System.Windows.Forms.LinkLabel();
            errorProvider1 = new System.Windows.Forms.ErrorProvider(components);
            lblGetScript = new System.Windows.Forms.Label();
            timer1 = new System.Windows.Forms.Timer(components);
            lnk1min = new System.Windows.Forms.LinkLabel();
            linkLabel1 = new System.Windows.Forms.LinkLabel();
            linkLabel2 = new System.Windows.Forms.LinkLabel();
            lblCron = new System.Windows.Forms.Label();
            linkLabel3 = new System.Windows.Forms.LinkLabel();
            linkLabel4 = new System.Windows.Forms.LinkLabel();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            linkLabel5 = new System.Windows.Forms.LinkLabel();
            ((System.ComponentModel.ISupportInitialize)numTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvCustom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)errorProvider1).BeginInit();
            SuspendLayout();
            // 
            // txtName
            // 
            txtName.Location = new System.Drawing.Point(153, 69);
            txtName.Name = "txtName";
            txtName.Size = new System.Drawing.Size(237, 27);
            txtName.TabIndex = 2;
            txtName.Validating += TxtName_Validating;
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.Location = new System.Drawing.Point(12, 76);
            lblName.Name = "lblName";
            lblName.Size = new System.Drawing.Size(123, 20);
            lblName.TabIndex = 1;
            lblName.Text = "Collection Name:";
            toolTip1.SetToolTip(lblName, resources.GetString("lblName.ToolTip"));
            // 
            // cboProcedureName
            // 
            cboProcedureName.FormattingEnabled = true;
            cboProcedureName.Location = new System.Drawing.Point(153, 26);
            cboProcedureName.Name = "cboProcedureName";
            cboProcedureName.Size = new System.Drawing.Size(237, 28);
            cboProcedureName.TabIndex = 0;
            cboProcedureName.SelectedIndexChanged += CboProcedureName_SelectedIndexChanged;
            cboProcedureName.Validating += CboProcedureName_Validating;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 29);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(127, 20);
            label1.TabIndex = 3;
            label1.Text = "Stored Procedure:";
            toolTip1.SetToolTip(label1, resources.GetString("label1.ToolTip"));
            // 
            // txtCron
            // 
            txtCron.Location = new System.Drawing.Point(153, 130);
            txtCron.Name = "txtCron";
            txtCron.Size = new System.Drawing.Size(237, 27);
            txtCron.TabIndex = 8;
            txtCron.TextChanged += TxtCron_TextChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(12, 133);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(114, 20);
            label2.TabIndex = 5;
            label2.Text = "Schedule (Cron)";
            toolTip1.SetToolTip(label2, "Either specify a Quartz compatible cron or enter an interval time in seconds");
            // 
            // numTimeout
            // 
            numTimeout.Enabled = false;
            numTimeout.Location = new System.Drawing.Point(153, 195);
            numTimeout.Maximum = new decimal(new int[] { int.MaxValue, 0, 0, 0 });
            numTimeout.Name = "numTimeout";
            numTimeout.Size = new System.Drawing.Size(108, 27);
            numTimeout.TabIndex = 10;
            numTimeout.Value = new decimal(new int[] { 60, 0, 0, 0 });
            // 
            // chkDefaultTimeout
            // 
            chkDefaultTimeout.AutoSize = true;
            chkDefaultTimeout.Checked = true;
            chkDefaultTimeout.CheckState = System.Windows.Forms.CheckState.Checked;
            chkDefaultTimeout.Location = new System.Drawing.Point(267, 198);
            chkDefaultTimeout.Name = "chkDefaultTimeout";
            chkDefaultTimeout.Size = new System.Drawing.Size(80, 24);
            chkDefaultTimeout.TabIndex = 11;
            chkDefaultTimeout.Text = "Default";
            chkDefaultTimeout.UseVisualStyleBackColor = true;
            chkDefaultTimeout.CheckedChanged += ChkDefaultTimeout_CheckedChanged;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(12, 199);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(67, 20);
            label3.TabIndex = 8;
            label3.Text = "Timeout:";
            toolTip1.SetToolTip(label3, "Specify a command timout for your query if required. ");
            // 
            // dgvCustom
            // 
            dgvCustom.AllowUserToAddRows = false;
            dgvCustom.AllowUserToDeleteRows = false;
            dgvCustom.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            dgvCustom.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvCustom.Location = new System.Drawing.Point(12, 286);
            dgvCustom.Name = "dgvCustom";
            dgvCustom.RowHeadersVisible = false;
            dgvCustom.RowHeadersWidth = 51;
            dgvCustom.RowTemplate.Height = 29;
            dgvCustom.Size = new System.Drawing.Size(807, 256);
            dgvCustom.TabIndex = 13;
            dgvCustom.CellContentClick += DgvCustom_CellContentClick;
            dgvCustom.CellEndEdit += DgvCustom_CellEndEdit;
            // 
            // bttnAdd
            // 
            bttnAdd.Location = new System.Drawing.Point(359, 241);
            bttnAdd.Name = "bttnAdd";
            bttnAdd.Size = new System.Drawing.Size(108, 29);
            bttnAdd.TabIndex = 12;
            bttnAdd.Text = "Add";
            bttnAdd.UseVisualStyleBackColor = true;
            bttnAdd.Click += BttnAdd_Click;
            // 
            // chkRunOnStart
            // 
            chkRunOnStart.AutoSize = true;
            chkRunOnStart.Location = new System.Drawing.Point(153, 163);
            chkRunOnStart.Name = "chkRunOnStart";
            chkRunOnStart.Size = new System.Drawing.Size(159, 24);
            chkRunOnStart.TabIndex = 9;
            chkRunOnStart.Text = "Run on service start";
            toolTip1.SetToolTip(chkRunOnStart, "Option to run the collection when the service starts instead of waiting until the next schedule.  ");
            chkRunOnStart.UseVisualStyleBackColor = true;
            // 
            // bttnUpdate
            // 
            bttnUpdate.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnUpdate.Location = new System.Drawing.Point(725, 561);
            bttnUpdate.Name = "bttnUpdate";
            bttnUpdate.Size = new System.Drawing.Size(94, 29);
            bttnUpdate.TabIndex = 14;
            bttnUpdate.Text = "&Update";
            bttnUpdate.UseVisualStyleBackColor = true;
            bttnUpdate.Click += BttnUpdate_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.Location = new System.Drawing.Point(613, 561);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 15;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += BttnCancel_Click;
            // 
            // lnkPreview
            // 
            lnkPreview.AutoSize = true;
            lnkPreview.Location = new System.Drawing.Point(407, 29);
            lnkPreview.Name = "lnkPreview";
            lnkPreview.Size = new System.Drawing.Size(60, 20);
            lnkPreview.TabIndex = 1;
            lnkPreview.TabStop = true;
            lnkPreview.Text = "Preview";
            lnkPreview.LinkClicked += LnkPreview_LinkClicked;
            // 
            // errorProvider1
            // 
            errorProvider1.ContainerControl = this;
            // 
            // lblGetScript
            // 
            lblGetScript.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            lblGetScript.AutoSize = true;
            lblGetScript.Location = new System.Drawing.Point(12, 565);
            lblGetScript.Name = "lblGetScript";
            lblGetScript.Size = new System.Drawing.Size(390, 20);
            lblGetScript.TabIndex = 11;
            lblGetScript.Text = "Running the query to obtain schema for script generation.";
            lblGetScript.Visible = false;
            // 
            // timer1
            // 
            timer1.Interval = 500;
            timer1.Tick += Timer1_Tick;
            // 
            // lnk1min
            // 
            lnk1min.AutoSize = true;
            lnk1min.Location = new System.Drawing.Point(153, 107);
            lnk1min.Name = "lnk1min";
            lnk1min.Size = new System.Drawing.Size(42, 20);
            lnk1min.TabIndex = 3;
            lnk1min.TabStop = true;
            lnk1min.Tag = "0 * * ? * *";
            lnk1min.Text = "1min";
            lnk1min.LinkClicked += SetCron;
            // 
            // linkLabel1
            // 
            linkLabel1.AutoSize = true;
            linkLabel1.Location = new System.Drawing.Point(259, 107);
            linkLabel1.Name = "linkLabel1";
            linkLabel1.Size = new System.Drawing.Size(30, 20);
            linkLabel1.TabIndex = 5;
            linkLabel1.TabStop = true;
            linkLabel1.Tag = "0 0 * ? * *";
            linkLabel1.Text = "1hr";
            linkLabel1.LinkClicked += SetCron;
            // 
            // linkLabel2
            // 
            linkLabel2.AutoSize = true;
            linkLabel2.Location = new System.Drawing.Point(341, 107);
            linkLabel2.Name = "linkLabel2";
            linkLabel2.Size = new System.Drawing.Size(46, 20);
            linkLabel2.TabIndex = 7;
            linkLabel2.TabStop = true;
            linkLabel2.Tag = "0 0 0 1/1 * ? *";
            linkLabel2.Text = "12am";
            linkLabel2.LinkClicked += SetCron;
            // 
            // lblCron
            // 
            lblCron.AutoSize = true;
            lblCron.Location = new System.Drawing.Point(396, 133);
            lblCron.Name = "lblCron";
            lblCron.Size = new System.Drawing.Size(93, 20);
            lblCron.TabIndex = 15;
            lblCron.Text = "No Schedule";
            // 
            // linkLabel3
            // 
            linkLabel3.AutoSize = true;
            linkLabel3.Location = new System.Drawing.Point(206, 107);
            linkLabel3.Name = "linkLabel3";
            linkLabel3.Size = new System.Drawing.Size(42, 20);
            linkLabel3.TabIndex = 4;
            linkLabel3.TabStop = true;
            linkLabel3.Tag = "0 */5 * ? * *";
            linkLabel3.Text = "5min";
            linkLabel3.LinkClicked += SetCron;
            // 
            // linkLabel4
            // 
            linkLabel4.AutoSize = true;
            linkLabel4.Location = new System.Drawing.Point(300, 107);
            linkLabel4.Name = "linkLabel4";
            linkLabel4.Size = new System.Drawing.Size(30, 20);
            linkLabel4.TabIndex = 6;
            linkLabel4.TabStop = true;
            linkLabel4.Tag = "0 0 */2 ? * *";
            linkLabel4.Text = "2hr";
            linkLabel4.LinkClicked += SetCron;
            // 
            // linkLabel5
            // 
            linkLabel5.AutoSize = true;
            linkLabel5.Location = new System.Drawing.Point(407, 107);
            linkLabel5.Name = "linkLabel5";
            linkLabel5.Size = new System.Drawing.Size(59, 20);
            linkLabel5.TabIndex = 16;
            linkLabel5.TabStop = true;
            linkLabel5.Tag = "";
            linkLabel5.Text = "Disable";
            linkLabel5.LinkClicked += SetCron;
            // 
            // ManageCustomCollections
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(831, 602);
            Controls.Add(linkLabel5);
            Controls.Add(linkLabel4);
            Controls.Add(linkLabel3);
            Controls.Add(lblCron);
            Controls.Add(linkLabel2);
            Controls.Add(linkLabel1);
            Controls.Add(lnk1min);
            Controls.Add(lblGetScript);
            Controls.Add(lnkPreview);
            Controls.Add(bttnCancel);
            Controls.Add(bttnUpdate);
            Controls.Add(chkRunOnStart);
            Controls.Add(bttnAdd);
            Controls.Add(dgvCustom);
            Controls.Add(label3);
            Controls.Add(chkDefaultTimeout);
            Controls.Add(numTimeout);
            Controls.Add(label2);
            Controls.Add(txtCron);
            Controls.Add(label1);
            Controls.Add(cboProcedureName);
            Controls.Add(lblName);
            Controls.Add(txtName);
            MinimumSize = new System.Drawing.Size(650, 500);
            Name = "ManageCustomCollections";
            Text = "Custom Collections";
            Load += CustomCollections_Load;
            ((System.ComponentModel.ISupportInitialize)numTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvCustom).EndInit();
            ((System.ComponentModel.ISupportInitialize)errorProvider1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.ComboBox cboProcedureName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtCron;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numTimeout;
        private System.Windows.Forms.CheckBox chkDefaultTimeout;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DataGridView dgvCustom;
        private System.Windows.Forms.Button bttnAdd;
        private System.Windows.Forms.CheckBox chkRunOnStart;
        private System.Windows.Forms.Button bttnUpdate;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.LinkLabel lnkPreview;
        private System.Windows.Forms.ErrorProvider errorProvider1;
        private System.Windows.Forms.Label lblGetScript;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.LinkLabel lnk1min;
        private System.Windows.Forms.LinkLabel linkLabel2;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Label lblCron;
        private System.Windows.Forms.LinkLabel linkLabel4;
        private System.Windows.Forms.LinkLabel linkLabel3;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.LinkLabel linkLabel5;
    }
}