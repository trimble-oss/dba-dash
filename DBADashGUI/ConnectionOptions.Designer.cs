
namespace DBADashGUI
{
    partial class ConnectionOptions
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConnectionOptions));
            label1 = new System.Windows.Forms.Label();
            optConfigure = new System.Windows.Forms.RadioButton();
            optConnect = new System.Windows.Forms.RadioButton();
            bttnOK = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // label1
            // 
            label1.Location = new System.Drawing.Point(25, 29);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(726, 112);
            label1.TabIndex = 0;
            label1.Text = resources.GetString("label1.Text");
            // 
            // optConfigure
            // 
            optConfigure.AutoSize = true;
            optConfigure.Checked = true;
            optConfigure.Location = new System.Drawing.Point(29, 184);
            optConfigure.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            optConfigure.Name = "optConfigure";
            optConfigure.Size = new System.Drawing.Size(169, 24);
            optConfigure.TabIndex = 1;
            optConfigure.TabStop = true;
            optConfigure.Text = "Configure the service";
            optConfigure.UseVisualStyleBackColor = true;
            // 
            // optConnect
            // 
            optConnect.AutoSize = true;
            optConnect.Location = new System.Drawing.Point(29, 238);
            optConnect.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            optConnect.Name = "optConnect";
            optConnect.Size = new System.Drawing.Size(322, 24);
            optConnect.TabIndex = 2;
            optConnect.Text = "Connect to an existing DBA Dash repository.";
            optConnect.UseVisualStyleBackColor = true;
            // 
            // bttnOK
            // 
            bttnOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnOK.AutoSize = true;
            bttnOK.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            bttnOK.Location = new System.Drawing.Point(611, 330);
            bttnOK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            bttnOK.Name = "bttnOK";
            bttnOK.Padding = new System.Windows.Forms.Padding(30, 8, 30, 8);
            bttnOK.Size = new System.Drawing.Size(99, 46);
            bttnOK.TabIndex = 3;
            bttnOK.Text = "OK";
            bttnOK.UseVisualStyleBackColor = true;
            bttnOK.Click += BttnOK_Click;
            // 
            // ConnectionOptions
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(763, 419);
            Controls.Add(bttnOK);
            Controls.Add(optConnect);
            Controls.Add(optConfigure);
            Controls.Add(label1);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "ConnectionOptions";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Connection Options";
            Load += ConnectionOptions_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton optConfigure;
        private System.Windows.Forms.RadioButton optConnect;
        private System.Windows.Forms.Button bttnOK;
    }
}