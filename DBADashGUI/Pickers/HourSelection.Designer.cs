namespace DBADashGUI
{
    partial class HourSelection
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
            this.chkHours = new System.Windows.Forms.CheckedListBox();
            this.bttnToggle = new System.Windows.Forms.Button();
            this.bttn9to5 = new System.Windows.Forms.Button();
            this.bttnOK = new System.Windows.Forms.Button();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.bttnAll = new System.Windows.Forms.Button();
            this.bttnNone = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // chkHours
            // 
            this.chkHours.CheckOnClick = true;
            this.chkHours.Dock = System.Windows.Forms.DockStyle.Left;
            this.chkHours.FormattingEnabled = true;
            this.chkHours.Items.AddRange(new object[] {
            "00:00 to 01:00",
            "01:00 to 02:00",
            "02:00 to 03:00",
            "03:00 to 04:00",
            "04:00 to 05:00",
            "05:00 to 06:00",
            "06:00 to 07:00",
            "07:00 to 08:00",
            "08:00 to 09:00",
            "09:00 to 10:00",
            "10:00 to 11:00",
            "11:00 to 12:00",
            "12:00 to 13:00",
            "13:00 to 14:00",
            "14:00 to 15:00",
            "15:00 to 16:00",
            "16:00 to 17:00",
            "17:00 to 18:00",
            "18:00 to 19:00",
            "19:00 to 20:00",
            "20:00 to 21:00",
            "21:00 to 22:00",
            "22:00 to 23:00",
            "23:00 to 00:00"});
            this.chkHours.Location = new System.Drawing.Point(0, 0);
            this.chkHours.Name = "chkHours";
            this.chkHours.Size = new System.Drawing.Size(160, 557);
            this.chkHours.TabIndex = 0;
            // 
            // bttnToggle
            // 
            this.bttnToggle.Location = new System.Drawing.Point(186, 82);
            this.bttnToggle.Name = "bttnToggle";
            this.bttnToggle.Size = new System.Drawing.Size(148, 29);
            this.bttnToggle.TabIndex = 1;
            this.bttnToggle.Text = "Toggle Selection";
            this.bttnToggle.UseVisualStyleBackColor = true;
            this.bttnToggle.Click += new System.EventHandler(this.bttnToggle_Click);
            // 
            // bttn9to5
            // 
            this.bttn9to5.Location = new System.Drawing.Point(186, 117);
            this.bttn9to5.Name = "bttn9to5";
            this.bttn9to5.Size = new System.Drawing.Size(148, 29);
            this.bttn9to5.TabIndex = 2;
            this.bttn9to5.Text = "09:00 to 17:00";
            this.bttn9to5.UseVisualStyleBackColor = true;
            this.bttn9to5.Click += new System.EventHandler(this.bttn9to5_Click);
            // 
            // bttnOK
            // 
            this.bttnOK.Location = new System.Drawing.Point(186, 504);
            this.bttnOK.Name = "bttnOK";
            this.bttnOK.Size = new System.Drawing.Size(94, 29);
            this.bttnOK.TabIndex = 3;
            this.bttnOK.Text = "&OK";
            this.bttnOK.UseVisualStyleBackColor = true;
            this.bttnOK.Click += new System.EventHandler(this.bttnOK_Click);
            // 
            // bttnCancel
            // 
            this.bttnCancel.Location = new System.Drawing.Point(286, 504);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(94, 29);
            this.bttnCancel.TabIndex = 4;
            this.bttnCancel.Text = "&Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            // 
            // bttnAll
            // 
            this.bttnAll.Location = new System.Drawing.Point(186, 12);
            this.bttnAll.Name = "bttnAll";
            this.bttnAll.Size = new System.Drawing.Size(148, 29);
            this.bttnAll.TabIndex = 5;
            this.bttnAll.Text = "All";
            this.bttnAll.UseVisualStyleBackColor = true;
            this.bttnAll.Click += new System.EventHandler(this.bttnAll_Click);
            // 
            // bttnNone
            // 
            this.bttnNone.Location = new System.Drawing.Point(186, 47);
            this.bttnNone.Name = "bttnNone";
            this.bttnNone.Size = new System.Drawing.Size(148, 29);
            this.bttnNone.TabIndex = 6;
            this.bttnNone.Text = "None";
            this.bttnNone.UseVisualStyleBackColor = true;
            this.bttnNone.Click += new System.EventHandler(this.bttnNone_Click);
            // 
            // HourSelection
            // 
            this.AcceptButton = this.bttnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttnCancel;
            this.ClientSize = new System.Drawing.Size(396, 557);
            this.Controls.Add(this.bttnNone);
            this.Controls.Add(this.bttnAll);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.bttnOK);
            this.Controls.Add(this.bttn9to5);
            this.Controls.Add(this.bttnToggle);
            this.Controls.Add(this.chkHours);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "HourSelection";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Hour Selection";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckedListBox chkHours;
        private System.Windows.Forms.Button bttnToggle;
        private System.Windows.Forms.Button bttn9to5;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Button bttnAll;
        private System.Windows.Forms.Button bttnNone;
    }
}