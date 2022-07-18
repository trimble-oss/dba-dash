namespace DBADashGUI
{
    partial class DayOfWeekSelection
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
            this.chkDayOfWeek = new System.Windows.Forms.CheckedListBox();
            this.bttnOK = new System.Windows.Forms.Button();
            this.bttnCancel = new System.Windows.Forms.Button();
            this.bttnNone = new System.Windows.Forms.Button();
            this.bttnALL = new System.Windows.Forms.Button();
            this.bttnWeekday = new System.Windows.Forms.Button();
            this.bttnToggle = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // chkDayOfWeek
            // 
            this.chkDayOfWeek.CheckOnClick = true;
            this.chkDayOfWeek.Dock = System.Windows.Forms.DockStyle.Left;
            this.chkDayOfWeek.FormattingEnabled = true;
            this.chkDayOfWeek.Items.AddRange(new object[] {
            "Monday",
            "Tuesday",
            "Wednesday",
            "Thursday",
            "Friday",
            "Saturday",
            "Sunday"});
            this.chkDayOfWeek.Location = new System.Drawing.Point(0, 0);
            this.chkDayOfWeek.Name = "chkDayOfWeek";
            this.chkDayOfWeek.Size = new System.Drawing.Size(138, 222);
            this.chkDayOfWeek.TabIndex = 0;
            // 
            // bttnOK
            // 
            this.bttnOK.Location = new System.Drawing.Point(172, 178);
            this.bttnOK.Name = "bttnOK";
            this.bttnOK.Size = new System.Drawing.Size(94, 29);
            this.bttnOK.TabIndex = 1;
            this.bttnOK.Text = "&OK";
            this.bttnOK.UseVisualStyleBackColor = true;
            this.bttnOK.Click += new System.EventHandler(this.bttnOK_Click);
            // 
            // bttnCancel
            // 
            this.bttnCancel.Location = new System.Drawing.Point(272, 178);
            this.bttnCancel.Name = "bttnCancel";
            this.bttnCancel.Size = new System.Drawing.Size(94, 29);
            this.bttnCancel.TabIndex = 2;
            this.bttnCancel.Text = "&Cancel";
            this.bttnCancel.UseVisualStyleBackColor = true;
            // 
            // bttnNone
            // 
            this.bttnNone.Location = new System.Drawing.Point(172, 47);
            this.bttnNone.Name = "bttnNone";
            this.bttnNone.Size = new System.Drawing.Size(148, 29);
            this.bttnNone.TabIndex = 10;
            this.bttnNone.Text = "None";
            this.bttnNone.UseVisualStyleBackColor = true;
            this.bttnNone.Click += new System.EventHandler(this.bttnNone_Click);
            // 
            // bttnALL
            // 
            this.bttnALL.Location = new System.Drawing.Point(172, 12);
            this.bttnALL.Name = "bttnALL";
            this.bttnALL.Size = new System.Drawing.Size(148, 29);
            this.bttnALL.TabIndex = 9;
            this.bttnALL.Text = "All";
            this.bttnALL.UseVisualStyleBackColor = true;
            this.bttnALL.Click += new System.EventHandler(this.bttnALL_Click);
            // 
            // bttnWeekday
            // 
            this.bttnWeekday.Location = new System.Drawing.Point(172, 117);
            this.bttnWeekday.Name = "bttnWeekday";
            this.bttnWeekday.Size = new System.Drawing.Size(148, 29);
            this.bttnWeekday.TabIndex = 8;
            this.bttnWeekday.Text = "Weekday";
            this.bttnWeekday.UseVisualStyleBackColor = true;
            this.bttnWeekday.Click += new System.EventHandler(this.bttnWeekday_Click);
            // 
            // bttnToggle
            // 
            this.bttnToggle.Location = new System.Drawing.Point(172, 82);
            this.bttnToggle.Name = "bttnToggle";
            this.bttnToggle.Size = new System.Drawing.Size(148, 29);
            this.bttnToggle.TabIndex = 7;
            this.bttnToggle.Text = "Toggle Selection";
            this.bttnToggle.UseVisualStyleBackColor = true;
            this.bttnToggle.Click += new System.EventHandler(this.bttnToggle_Click);
            // 
            // DayOfWeekSelection
            // 
            this.AcceptButton = this.bttnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.bttnCancel;
            this.ClientSize = new System.Drawing.Size(383, 222);
            this.Controls.Add(this.bttnNone);
            this.Controls.Add(this.bttnALL);
            this.Controls.Add(this.bttnWeekday);
            this.Controls.Add(this.bttnToggle);
            this.Controls.Add(this.bttnCancel);
            this.Controls.Add(this.bttnOK);
            this.Controls.Add(this.chkDayOfWeek);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "DayOfWeekSelection";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Day Of Week";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckedListBox chkDayOfWeek;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Button bttnNone;
        private System.Windows.Forms.Button bttnALL;
        private System.Windows.Forms.Button bttnWeekday;
        private System.Windows.Forms.Button bttnToggle;
    }
}