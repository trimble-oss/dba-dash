namespace DBAChecksGUI.Performance
{
    partial class CustomTimePicker
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
            this.time1 = new System.Windows.Forms.DateTimePicker();
            this.time2 = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.bttnOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // time1
            // 
            this.time1.CustomFormat = "yyyy-MM-dd HH:mm";
            this.time1.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.time1.Location = new System.Drawing.Point(30, 51);
            this.time1.Name = "time1";
            this.time1.Size = new System.Drawing.Size(250, 22);
            this.time1.TabIndex = 0;
            // 
            // time2
            // 
            this.time2.CustomFormat = "yyyy-MM-dd HH:mm";
            this.time2.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.time2.Location = new System.Drawing.Point(299, 51);
            this.time2.Name = "time2";
            this.time2.Size = new System.Drawing.Size(250, 22);
            this.time2.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "From:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(296, 31);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "To:";
            // 
            // bttnOK
            // 
            this.bttnOK.Location = new System.Drawing.Point(474, 139);
            this.bttnOK.Name = "bttnOK";
            this.bttnOK.Size = new System.Drawing.Size(75, 23);
            this.bttnOK.TabIndex = 4;
            this.bttnOK.Text = "OK";
            this.bttnOK.UseVisualStyleBackColor = true;
            this.bttnOK.Click += new System.EventHandler(this.bttnOK_Click);
            // 
            // CustomTimePicker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(591, 217);
            this.Controls.Add(this.bttnOK);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.time2);
            this.Controls.Add(this.time1);
            this.Name = "CustomTimePicker";
            this.Text = "Time Picker";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DateTimePicker time1;
        private System.Windows.Forms.DateTimePicker time2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bttnOK;
    }
}