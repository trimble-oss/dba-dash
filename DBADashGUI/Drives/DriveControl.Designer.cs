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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DriveControl));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.lblDriveLabel = new System.Windows.Forms.Label();
            this.lblFree = new System.Windows.Forms.Label();
            this.lnkThreshold = new System.Windows.Forms.LinkLabel();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.picStatus = new System.Windows.Forms.PictureBox();
            this.lblThresholds = new System.Windows.Forms.Label();
            this.lnkHistory = new System.Windows.Forms.LinkLabel();
            this.pbSpace = new DBADashGUI.CustomProgressBar();
            this.lblUpdated = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picStatus)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::DBADashGUI.Properties.Resources.Hard_Drive;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(54, 56);
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // lblDriveLabel
            // 
            this.lblDriveLabel.AutoSize = true;
            this.lblDriveLabel.Location = new System.Drawing.Point(63, 0);
            this.lblDriveLabel.Name = "lblDriveLabel";
            this.lblDriveLabel.Size = new System.Drawing.Size(25, 17);
            this.lblDriveLabel.TabIndex = 2;
            this.lblDriveLabel.Text = "C:\\";
            // 
            // lblFree
            // 
            this.lblFree.AutoSize = true;
            this.lblFree.Location = new System.Drawing.Point(63, 46);
            this.lblFree.Name = "lblFree";
            this.lblFree.Size = new System.Drawing.Size(113, 17);
            this.lblFree.TabIndex = 3;
            this.lblFree.Text = "0GB free of 0GB";
            // 
            // lnkThreshold
            // 
            this.lnkThreshold.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lnkThreshold.AutoSize = true;
            this.lnkThreshold.Location = new System.Drawing.Point(438, 0);
            this.lnkThreshold.Name = "lnkThreshold";
            this.lnkThreshold.Size = new System.Drawing.Size(69, 17);
            this.lnkThreshold.TabIndex = 28;
            this.lnkThreshold.TabStop = true;
            this.lnkThreshold.Text = "Configure";
            this.lnkThreshold.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkThreshold_LinkClicked);
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "StatusAnnotations_Critical_32xLG_color.png");
            this.imageList1.Images.SetKeyName(1, "StatusAnnotations_Warning_32xLG_color.png");
            this.imageList1.Images.SetKeyName(2, "StatusAnnotations_Complete_and_ok_32xLG_color.png");
            // 
            // picStatus
            // 
            this.picStatus.Image = global::DBADashGUI.Properties.Resources.StatusAnnotations_Warning_32xLG_color;
            this.picStatus.Location = new System.Drawing.Point(22, 50);
            this.picStatus.Name = "picStatus";
            this.picStatus.Size = new System.Drawing.Size(32, 32);
            this.picStatus.TabIndex = 30;
            this.picStatus.TabStop = false;
            this.picStatus.Visible = false;
            // 
            // lblThresholds
            // 
            this.lblThresholds.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblThresholds.Location = new System.Drawing.Point(94, 0);
            this.lblThresholds.Name = "lblThresholds";
            this.lblThresholds.Size = new System.Drawing.Size(338, 17);
            this.lblThresholds.TabIndex = 32;
            this.lblThresholds.Text = "Warning: 20%, Critical 10%";
            this.lblThresholds.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lnkHistory
            // 
            this.lnkHistory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lnkHistory.AutoSize = true;
            this.lnkHistory.Location = new System.Drawing.Point(455, 46);
            this.lnkHistory.Name = "lnkHistory";
            this.lnkHistory.Size = new System.Drawing.Size(52, 17);
            this.lnkHistory.TabIndex = 33;
            this.lnkHistory.TabStop = true;
            this.lnkHistory.Text = "History";
            this.lnkHistory.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LnkHistory_LinkClicked);
            // 
            // pbSpace
            // 
            this.pbSpace.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbSpace.BackColor = System.Drawing.Color.RoyalBlue;
            this.pbSpace.ForeColor = System.Drawing.Color.LightBlue;
            this.pbSpace.Location = new System.Drawing.Point(66, 20);
            this.pbSpace.MarqueeAnimationSpeed = 100000000;
            this.pbSpace.Name = "pbSpace";
            this.pbSpace.Size = new System.Drawing.Size(441, 23);
            this.pbSpace.TabIndex = 31;
            this.pbSpace.Value = 50;
            // 
            // lblUpdated
            // 
            this.lblUpdated.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblUpdated.AutoSize = true;
            this.lblUpdated.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblUpdated.ForeColor = System.Drawing.Color.Black;
            this.lblUpdated.Location = new System.Drawing.Point(63, 63);
            this.lblUpdated.Name = "lblUpdated";
            this.lblUpdated.Size = new System.Drawing.Size(143, 17);
            this.lblUpdated.TabIndex = 34;
            this.lblUpdated.Text = "Updated minutes ago";
            // 
            // DriveControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblUpdated);
            this.Controls.Add(this.lnkHistory);
            this.Controls.Add(this.lblThresholds);
            this.Controls.Add(this.pbSpace);
            this.Controls.Add(this.picStatus);
            this.Controls.Add(this.lnkThreshold);
            this.Controls.Add(this.lblFree);
            this.Controls.Add(this.lblDriveLabel);
            this.Controls.Add(this.pictureBox1);
            this.Name = "DriveControl";
            this.Size = new System.Drawing.Size(510, 82);
            this.Load += new System.EventHandler(this.DriveControl_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picStatus)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblDriveLabel;
        private System.Windows.Forms.Label lblFree;
        private System.Windows.Forms.LinkLabel lnkThreshold;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.PictureBox picStatus;
        private CustomProgressBar pbSpace;
        private System.Windows.Forms.Label lblThresholds;
        private System.Windows.Forms.LinkLabel lnkHistory;
        private System.Windows.Forms.Label lblUpdated;
    }
}
