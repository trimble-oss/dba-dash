namespace DBADashServiceConfig
{
    partial class AzureDBOptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AzureDBOptions));
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.chkScanAzureDB = new System.Windows.Forms.CheckBox();
            this.lblHHmm = new System.Windows.Forms.Label();
            this.bttnScanNow = new System.Windows.Forms.Button();
            this.numAzureScanInterval = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.chkScanEvery = new System.Windows.Forms.CheckBox();
            this.bttnOK = new System.Windows.Forms.Button();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.groupBox4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAzureScanInterval)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.chkScanAzureDB);
            this.groupBox4.Controls.Add(this.lblHHmm);
            this.groupBox4.Controls.Add(this.bttnScanNow);
            this.groupBox4.Controls.Add(this.numAzureScanInterval);
            this.groupBox4.Controls.Add(this.label10);
            this.groupBox4.Controls.Add(this.label11);
            this.groupBox4.Controls.Add(this.chkScanEvery);
            this.groupBox4.Location = new System.Drawing.Point(12, 13);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox4.Size = new System.Drawing.Size(521, 326);
            this.groupBox4.TabIndex = 31;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Azure DB";
            // 
            // chkScanAzureDB
            // 
            this.chkScanAzureDB.AutoSize = true;
            this.chkScanAzureDB.Location = new System.Drawing.Point(18, 39);
            this.chkScanAzureDB.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkScanAzureDB.Name = "chkScanAzureDB";
            this.chkScanAzureDB.Size = new System.Drawing.Size(256, 24);
            this.chkScanAzureDB.TabIndex = 23;
            this.chkScanAzureDB.Text = "Scan for AzureDBs on service start";
            this.chkScanAzureDB.UseVisualStyleBackColor = true;
            // 
            // lblHHmm
            // 
            this.lblHHmm.AutoSize = true;
            this.lblHHmm.Location = new System.Drawing.Point(586, 73);
            this.lblHHmm.Name = "lblHHmm";
            this.lblHHmm.Size = new System.Drawing.Size(0, 20);
            this.lblHHmm.TabIndex = 29;
            // 
            // bttnScanNow
            // 
            this.bttnScanNow.Location = new System.Drawing.Point(19, 114);
            this.bttnScanNow.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.bttnScanNow.Name = "bttnScanNow";
            this.bttnScanNow.Size = new System.Drawing.Size(104, 38);
            this.bttnScanNow.TabIndex = 22;
            this.bttnScanNow.Text = "Scan Now";
            this.bttnScanNow.UseVisualStyleBackColor = true;
            // 
            // numAzureScanInterval
            // 
            this.numAzureScanInterval.Increment = new decimal(new int[] {
            3600,
            0,
            0,
            0});
            this.numAzureScanInterval.Location = new System.Drawing.Point(248, 72);
            this.numAzureScanInterval.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.numAzureScanInterval.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.numAzureScanInterval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.numAzureScanInterval.Name = "numAzureScanInterval";
            this.numAzureScanInterval.Size = new System.Drawing.Size(95, 27);
            this.numAzureScanInterval.TabIndex = 28;
            // 
            // label10
            // 
            this.label10.Location = new System.Drawing.Point(18, 166);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(477, 121);
            this.label10.TabIndex = 24;
            this.label10.Text = resources.GetString("label10.Text");
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(349, 74);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(62, 20);
            this.label11.TabIndex = 27;
            this.label11.Text = "seconds";
            // 
            // chkScanEvery
            // 
            this.chkScanEvery.AutoSize = true;
            this.chkScanEvery.Location = new System.Drawing.Point(19, 73);
            this.chkScanEvery.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.chkScanEvery.Name = "chkScanEvery";
            this.chkScanEvery.Size = new System.Drawing.Size(223, 24);
            this.chkScanEvery.TabIndex = 26;
            this.chkScanEvery.Text = "Scan for new AzureDBs every";
            this.chkScanEvery.UseVisualStyleBackColor = true;
            // 
            // bttnOK
            // 
            this.bttnOK.Location = new System.Drawing.Point(439, 357);
            this.bttnOK.Name = "bttnOK";
            this.bttnOK.Size = new System.Drawing.Size(94, 29);
            this.bttnOK.TabIndex = 32;
            this.bttnOK.Text = "&OK";
            this.bttnOK.UseVisualStyleBackColor = true;
            // 
            // bttnCancel
            // 
            this.bttnCancel.Location = new System.Drawing.Point(329, 357);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(94, 29);
            this.bttnCancel.TabIndex = 33;
            this.bttnCancel.Text = "&Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            // 
            // AzureDBOptions
            // 
            this.AcceptButton = this.bttnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttnCancel;
            this.ClientSize = new System.Drawing.Size(545, 405);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.bttnOK);
            this.Controls.Add(this.groupBox4);
            this.Name = "AzureDBOptions";
            this.Text = "AzureDBOptions";
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAzureScanInterval)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.CheckBox chkScanAzureDB;
        private System.Windows.Forms.Label lblHHmm;
        private System.Windows.Forms.Button bttnScanNow;
        private System.Windows.Forms.NumericUpDown numAzureScanInterval;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.CheckBox chkScanEvery;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.Button bttnCancel;
    }
}