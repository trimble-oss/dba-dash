namespace DBAChecksGUI.Properties
{
    partial class DrivesControl
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
            this.lblRootThreshold = new System.Windows.Forms.Label();
            this.lblInstanceThreshold = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lnkConfigureInstance = new System.Windows.Forms.LinkLabel();
            this.linkConfigureRoot = new System.Windows.Forms.LinkLabel();
            this.pnlDrives = new System.Windows.Forms.Panel();
            this.driveControl1 = new DBAChecksGUI.DriveControl();
            this.panel1.SuspendLayout();
            this.pnlDrives.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblRootThreshold
            // 
            this.lblRootThreshold.AutoSize = true;
            this.lblRootThreshold.Location = new System.Drawing.Point(146, 10);
            this.lblRootThreshold.Name = "lblRootThreshold";
            this.lblRootThreshold.Size = new System.Drawing.Size(18, 17);
            this.lblRootThreshold.TabIndex = 0;
            this.lblRootThreshold.Text = "{}";
            // 
            // lblInstanceThreshold
            // 
            this.lblInstanceThreshold.AutoSize = true;
            this.lblInstanceThreshold.Location = new System.Drawing.Point(146, 37);
            this.lblInstanceThreshold.Name = "lblInstanceThreshold";
            this.lblInstanceThreshold.Size = new System.Drawing.Size(18, 17);
            this.lblInstanceThreshold.TabIndex = 1;
            this.lblInstanceThreshold.Text = "{}";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.lnkConfigureInstance);
            this.panel1.Controls.Add(this.linkConfigureRoot);
            this.panel1.Controls.Add(this.lblRootThreshold);
            this.panel1.Controls.Add(this.lblInstanceThreshold);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(636, 80);
            this.panel1.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 37);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(137, 17);
            this.label2.TabIndex = 5;
            this.label2.Text = "Instance Threshold: ";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(114, 17);
            this.label1.TabIndex = 4;
            this.label1.Text = "Root Threshold: ";
            // 
            // lnkConfigureInstance
            // 
            this.lnkConfigureInstance.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lnkConfigureInstance.AutoSize = true;
            this.lnkConfigureInstance.Location = new System.Drawing.Point(564, 37);
            this.lnkConfigureInstance.Name = "lnkConfigureInstance";
            this.lnkConfigureInstance.Size = new System.Drawing.Size(69, 17);
            this.lnkConfigureInstance.TabIndex = 3;
            this.lnkConfigureInstance.TabStop = true;
            this.lnkConfigureInstance.Text = "Configure";
            this.lnkConfigureInstance.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkConfigureInstance_LinkClicked);
            // 
            // linkConfigureRoot
            // 
            this.linkConfigureRoot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.linkConfigureRoot.AutoSize = true;
            this.linkConfigureRoot.Location = new System.Drawing.Point(564, 10);
            this.linkConfigureRoot.Name = "linkConfigureRoot";
            this.linkConfigureRoot.Size = new System.Drawing.Size(69, 17);
            this.linkConfigureRoot.TabIndex = 2;
            this.linkConfigureRoot.TabStop = true;
            this.linkConfigureRoot.Text = "Configure";
            this.linkConfigureRoot.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkConfigureRoot_LinkClicked);
            // 
            // pnlDrives
            // 
            this.pnlDrives.Controls.Add(this.driveControl1);
            this.pnlDrives.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlDrives.Location = new System.Drawing.Point(0, 80);
            this.pnlDrives.Name = "pnlDrives";
            this.pnlDrives.Size = new System.Drawing.Size(636, 70);
            this.pnlDrives.TabIndex = 3;
            // 
            // driveControl1
            // 
            this.driveControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.driveControl1.Drive.ConnectionString = null;
            this.driveControl1.Drive.CriticalThreshold = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.driveControl1.Drive.DriveCapacity = ((long)(0));
            this.driveControl1.Drive.DriveCapacityGB = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.driveControl1.Drive.DriveCheckType = DBAChecksGUI.DriveThreshold.DriveCheckTypeEnum.Percent;
            this.driveControl1.Drive.DriveCheckTypeChar = 'I';
            this.driveControl1.Drive.DriveID = 0;
            this.driveControl1.Drive.DriveLabel = null;
            this.driveControl1.Drive.DriveLetter = null;
            this.driveControl1.Drive.FreeSpace = ((long)(0));
            this.driveControl1.Drive.FreeSpaceGB = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.driveControl1.Drive.Inherited = false;
            this.driveControl1.Drive.InstanceID = 0;
            this.driveControl1.Drive.WarningThreshold = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.driveControl1.Location = new System.Drawing.Point(0, 0);
            this.driveControl1.Name = "driveControl1";
            this.driveControl1.Size = new System.Drawing.Size(636, 70);
            this.driveControl1.TabIndex = 0;
            // 
            // DrivesControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlDrives);
            this.Controls.Add(this.panel1);
            this.Name = "DrivesControl";
            this.Size = new System.Drawing.Size(636, 150);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.pnlDrives.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblRootThreshold;
        private System.Windows.Forms.Label lblInstanceThreshold;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.LinkLabel lnkConfigureInstance;
        private System.Windows.Forms.LinkLabel linkConfigureRoot;
        private System.Windows.Forms.Panel pnlDrives;
        private DriveControl driveControl1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
    }
}
