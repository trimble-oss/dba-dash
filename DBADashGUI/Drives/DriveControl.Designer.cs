namespace DBADashGUI
{
    partial class DriveControl
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
            pictureBox1 = new System.Windows.Forms.PictureBox();
            lblDriveLabel = new System.Windows.Forms.Label();
            lblFree = new System.Windows.Forms.Label();
            lnkThreshold = new System.Windows.Forms.LinkLabel();
            picStatus = new System.Windows.Forms.PictureBox();
            lblThresholds = new System.Windows.Forms.Label();
            lnkHistory = new System.Windows.Forms.LinkLabel();
            pbSpace = new CustomProgressBar();
            lblUpdated = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picStatus).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.Hard_Drive;
            pictureBox1.Location = new System.Drawing.Point(0, 0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(47, 52);
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            // 
            // lblDriveLabel
            // 
            lblDriveLabel.AutoSize = true;
            lblDriveLabel.Location = new System.Drawing.Point(55, 0);
            lblDriveLabel.Name = "lblDriveLabel";
            lblDriveLabel.Size = new System.Drawing.Size(23, 15);
            lblDriveLabel.TabIndex = 2;
            lblDriveLabel.Text = "C:\\";
            // 
            // lblFree
            // 
            lblFree.AutoSize = true;
            lblFree.Location = new System.Drawing.Point(55, 44);
            lblFree.Name = "lblFree";
            lblFree.Size = new System.Drawing.Size(89, 15);
            lblFree.TabIndex = 3;
            lblFree.Text = "0GB free of 0GB";
            // 
            // lnkThreshold
            // 
            lnkThreshold.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lnkThreshold.AutoSize = true;
            lnkThreshold.Location = new System.Drawing.Point(383, 0);
            lnkThreshold.Name = "lnkThreshold";
            lnkThreshold.Size = new System.Drawing.Size(60, 15);
            lnkThreshold.TabIndex = 28;
            lnkThreshold.TabStop = true;
            lnkThreshold.Text = "Configure";
            lnkThreshold.LinkClicked += LnkThreshold_LinkClicked;
            // 
            // picStatus
            // 
            picStatus.Image = Properties.Resources.StatusAnnotations_Warning_32xLG_color;
            picStatus.Location = new System.Drawing.Point(19, 46);
            picStatus.Name = "picStatus";
            picStatus.Size = new System.Drawing.Size(28, 30);
            picStatus.TabIndex = 30;
            picStatus.TabStop = false;
            picStatus.Visible = false;
            // 
            // lblThresholds
            // 
            lblThresholds.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lblThresholds.Location = new System.Drawing.Point(82, 0);
            lblThresholds.Name = "lblThresholds";
            lblThresholds.Size = new System.Drawing.Size(296, 16);
            lblThresholds.TabIndex = 32;
            lblThresholds.Text = "Warning: 20%, Critical 10%";
            lblThresholds.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lnkHistory
            // 
            lnkHistory.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            lnkHistory.AutoSize = true;
            lnkHistory.Location = new System.Drawing.Point(398, 44);
            lnkHistory.Name = "lnkHistory";
            lnkHistory.Size = new System.Drawing.Size(45, 15);
            lnkHistory.TabIndex = 33;
            lnkHistory.TabStop = true;
            lnkHistory.Text = "History";
            lnkHistory.LinkClicked += LnkHistory_LinkClicked;
            // 
            // pbSpace
            // 
            pbSpace.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            pbSpace.BackColor = System.Drawing.Color.RoyalBlue;
            pbSpace.ForeColor = System.Drawing.Color.LightBlue;
            pbSpace.Location = new System.Drawing.Point(58, 19);
            pbSpace.MarqueeAnimationSpeed = 100000000;
            pbSpace.Name = "pbSpace";
            pbSpace.Size = new System.Drawing.Size(386, 22);
            pbSpace.TabIndex = 31;
            pbSpace.Value = 50;
            // 
            // lblUpdated
            // 
            lblUpdated.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            lblUpdated.AutoSize = true;
            lblUpdated.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, 0);
            lblUpdated.ForeColor = System.Drawing.Color.Black;
            lblUpdated.Location = new System.Drawing.Point(55, 59);
            lblUpdated.Name = "lblUpdated";
            lblUpdated.Size = new System.Drawing.Size(121, 15);
            lblUpdated.TabIndex = 34;
            lblUpdated.Text = "Updated minutes ago";
            // 
            // DriveControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(lblUpdated);
            Controls.Add(lnkHistory);
            Controls.Add(lblThresholds);
            Controls.Add(pbSpace);
            Controls.Add(picStatus);
            Controls.Add(lnkThreshold);
            Controls.Add(lblFree);
            Controls.Add(lblDriveLabel);
            Controls.Add(pictureBox1);
            Name = "DriveControl";
            Size = new System.Drawing.Size(446, 76);
            Load += DriveControl_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)picStatus).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblDriveLabel;
        private System.Windows.Forms.Label lblFree;
        private System.Windows.Forms.LinkLabel lnkThreshold;
        private System.Windows.Forms.PictureBox picStatus;
        private CustomProgressBar pbSpace;
        private System.Windows.Forms.Label lblThresholds;
        private System.Windows.Forms.LinkLabel lnkHistory;
        private System.Windows.Forms.Label lblUpdated;
    }
}
