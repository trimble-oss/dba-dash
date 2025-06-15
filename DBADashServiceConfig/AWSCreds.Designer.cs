namespace DBADashServiceConfig
{
    partial class AWSCreds
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AWSCreds));
            groupBox3 = new System.Windows.Forms.GroupBox();
            label4 = new System.Windows.Forms.Label();
            label12 = new System.Windows.Forms.Label();
            txtSecretKey = new System.Windows.Forms.TextBox();
            label3 = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            txtAccessKey = new System.Windows.Forms.TextBox();
            txtAWSProfile = new System.Windows.Forms.TextBox();
            bttnCancel = new System.Windows.Forms.Button();
            bttnOK = new System.Windows.Forms.Button();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox3
            // 
            groupBox3.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox3.Controls.Add(label4);
            groupBox3.Controls.Add(label12);
            groupBox3.Controls.Add(txtSecretKey);
            groupBox3.Controls.Add(label3);
            groupBox3.Controls.Add(label6);
            groupBox3.Controls.Add(txtAccessKey);
            groupBox3.Controls.Add(txtAWSProfile);
            groupBox3.Location = new System.Drawing.Point(12, 13);
            groupBox3.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            groupBox3.Size = new System.Drawing.Size(642, 358);
            groupBox3.TabIndex = 17;
            groupBox3.TabStop = false;
            groupBox3.Text = "AWS Credentials";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(16, 35);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(162, 20);
            label4.TabIndex = 8;
            label4.Text = "AWS Profile (Optional):";
            // 
            // label12
            // 
            label12.Location = new System.Drawing.Point(19, 156);
            label12.Name = "label12";
            label12.Size = new System.Drawing.Size(617, 182);
            label12.TabIndex = 15;
            label12.Text = resources.GetString("label12.Text");
            // 
            // txtSecretKey
            // 
            txtSecretKey.Location = new System.Drawing.Point(184, 99);
            txtSecretKey.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtSecretKey.Name = "txtSecretKey";
            txtSecretKey.Size = new System.Drawing.Size(452, 27);
            txtSecretKey.TabIndex = 5;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(16, 69);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(156, 20);
            label3.TabIndex = 13;
            label3.Text = "Access Key (Optional):";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(16, 103);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(153, 20);
            label6.TabIndex = 14;
            label6.Text = "Secret Key (Optional):";
            // 
            // txtAccessKey
            // 
            txtAccessKey.Location = new System.Drawing.Point(184, 64);
            txtAccessKey.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtAccessKey.Name = "txtAccessKey";
            txtAccessKey.Size = new System.Drawing.Size(452, 27);
            txtAccessKey.TabIndex = 4;
            // 
            // txtAWSProfile
            // 
            txtAWSProfile.Location = new System.Drawing.Point(184, 30);
            txtAWSProfile.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            txtAWSProfile.Name = "txtAWSProfile";
            txtAWSProfile.Size = new System.Drawing.Size(452, 27);
            txtAWSProfile.TabIndex = 3;
            // 
            // bttnCancel
            // 
            bttnCancel.Location = new System.Drawing.Point(445, 408);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 18;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += BttnCancel_Click;
            // 
            // bttnOK
            // 
            bttnOK.Location = new System.Drawing.Point(554, 408);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new System.Drawing.Size(94, 29);
            bttnOK.TabIndex = 19;
            bttnOK.Text = "&OK";
            bttnOK.UseVisualStyleBackColor = true;
            bttnOK.Click += BttnOK_Click;
            // 
            // AWSCreds
            // 
            AcceptButton = bttnOK;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new System.Drawing.Size(666, 465);
            Controls.Add(bttnOK);
            Controls.Add(bttnCancel);
            Controls.Add(groupBox3);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            Name = "AWSCreds";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "AWS Creds";
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox txtSecretKey;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtAccessKey;
        private System.Windows.Forms.TextBox txtAWSProfile;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Button bttnOK;
    }
}