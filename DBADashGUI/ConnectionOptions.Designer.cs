
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
            this.label1 = new System.Windows.Forms.Label();
            this.optConfigure = new System.Windows.Forms.RadioButton();
            this.optConnect = new System.Windows.Forms.RadioButton();
            this.bttnOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(25, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(751, 90);
            this.label1.TabIndex = 0;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // optConfigure
            // 
            this.optConfigure.AutoSize = true;
            this.optConfigure.Checked = true;
            this.optConfigure.Location = new System.Drawing.Point(29, 147);
            this.optConfigure.Name = "optConfigure";
            this.optConfigure.Size = new System.Drawing.Size(163, 21);
            this.optConfigure.TabIndex = 1;
            this.optConfigure.TabStop = true;
            this.optConfigure.Text = "Configure the service";
            this.optConfigure.UseVisualStyleBackColor = true;
            // 
            // optConnect
            // 
            this.optConnect.AutoSize = true;
            this.optConnect.Location = new System.Drawing.Point(29, 190);
            this.optConnect.Name = "optConnect";
            this.optConnect.Size = new System.Drawing.Size(308, 21);
            this.optConnect.TabIndex = 2;
            this.optConnect.Text = "Connect to an existing DBA Dash repository.";
            this.optConnect.UseVisualStyleBackColor = true;
            // 
            // bttnOK
            // 
            this.bttnOK.Location = new System.Drawing.Point(612, 269);
            this.bttnOK.Name = "bttnOK";
            this.bttnOK.Size = new System.Drawing.Size(92, 32);
            this.bttnOK.TabIndex = 3;
            this.bttnOK.Text = "OK";
            this.bttnOK.UseVisualStyleBackColor = true;
            this.bttnOK.Click += new System.EventHandler(this.bttnOK_Click);
            // 
            // ConnectionOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(763, 335);
            this.Controls.Add(this.bttnOK);
            this.Controls.Add(this.optConnect);
            this.Controls.Add(this.optConfigure);
            this.Controls.Add(this.label1);
            this.Name = "ConnectionOptions";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Connection Options";
            this.Load += new System.EventHandler(this.ConnectionOptions_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton optConfigure;
        private System.Windows.Forms.RadioButton optConnect;
        private System.Windows.Forms.Button bttnOK;
    }
}