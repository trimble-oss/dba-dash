
namespace DBADashGUI
{
    partial class DDLCompareTo
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DDLCompareTo));
            this.label5 = new System.Windows.Forms.Label();
            this.cboDate_A = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cboInstanceA = new System.Windows.Forms.ComboBox();
            this.cboDatabaseA = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.cboObjectA = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cboObjectTypeA = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cboObjectTypeB = new System.Windows.Forms.ComboBox();
            this.cboObjectB = new System.Windows.Forms.ComboBox();
            this.cboDate_B = new System.Windows.Forms.ComboBox();
            this.cboInstanceB = new System.Windows.Forms.ComboBox();
            this.cboDatabaseB = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pnlCompare = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(20, 162);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(124, 17);
            this.label5.TabIndex = 18;
            this.label5.Text = "Snapshot Version:";
            // 
            // cboDate_A
            // 
            this.cboDate_A.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDate_A.FormattingEnabled = true;
            this.cboDate_A.Location = new System.Drawing.Point(176, 158);
            this.cboDate_A.Name = "cboDate_A";
            this.cboDate_A.Size = new System.Drawing.Size(280, 24);
            this.cboDate_A.TabIndex = 17;
            this.cboDate_A.SelectedIndexChanged += new System.EventHandler(this.cboDate_A_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 17);
            this.label1.TabIndex = 15;
            this.label1.Text = "Instance:";
            // 
            // cboInstanceA
            // 
            this.cboInstanceA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboInstanceA.FormattingEnabled = true;
            this.cboInstanceA.Location = new System.Drawing.Point(176, 42);
            this.cboInstanceA.Name = "cboInstanceA";
            this.cboInstanceA.Size = new System.Drawing.Size(280, 24);
            this.cboInstanceA.TabIndex = 13;
            this.cboInstanceA.SelectedIndexChanged += new System.EventHandler(this.cboInstanceA_SelectedIndexedChanged);
            // 
            // cboDatabaseA
            // 
            this.cboDatabaseA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDatabaseA.FormattingEnabled = true;
            this.cboDatabaseA.Location = new System.Drawing.Point(176, 71);
            this.cboDatabaseA.Name = "cboDatabaseA";
            this.cboDatabaseA.Size = new System.Drawing.Size(280, 24);
            this.cboDatabaseA.TabIndex = 14;
            this.cboDatabaseA.SelectedIndexChanged += new System.EventHandler(this.cboDatabaseA_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(20, 72);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(73, 17);
            this.label3.TabIndex = 16;
            this.label3.Text = "Database:";
            // 
            // cboObjectA
            // 
            this.cboObjectA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboObjectA.FormattingEnabled = true;
            this.cboObjectA.Location = new System.Drawing.Point(176, 129);
            this.cboObjectA.Name = "cboObjectA";
            this.cboObjectA.Size = new System.Drawing.Size(280, 24);
            this.cboObjectA.TabIndex = 19;
            this.cboObjectA.SelectedIndexChanged += new System.EventHandler(this.cboObjectA_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 132);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(53, 17);
            this.label2.TabIndex = 20;
            this.label2.Text = "Object:";
            // 
            // cboObjectTypeA
            // 
            this.cboObjectTypeA.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboObjectTypeA.FormattingEnabled = true;
            this.cboObjectTypeA.Location = new System.Drawing.Point(176, 100);
            this.cboObjectTypeA.Name = "cboObjectTypeA";
            this.cboObjectTypeA.Size = new System.Drawing.Size(280, 24);
            this.cboObjectTypeA.TabIndex = 21;
            this.cboObjectTypeA.SelectedIndexChanged += new System.EventHandler(this.cboObjectTypeA_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(20, 102);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(89, 17);
            this.label4.TabIndex = 22;
            this.label4.Text = "Object Type:";
            // 
            // cboObjectTypeB
            // 
            this.cboObjectTypeB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboObjectTypeB.FormattingEnabled = true;
            this.cboObjectTypeB.Location = new System.Drawing.Point(494, 101);
            this.cboObjectTypeB.Name = "cboObjectTypeB";
            this.cboObjectTypeB.Size = new System.Drawing.Size(280, 24);
            this.cboObjectTypeB.TabIndex = 27;
            this.cboObjectTypeB.SelectedIndexChanged += new System.EventHandler(this.cboObjectTypeB_SelectedIndexChanged);
            // 
            // cboObjectB
            // 
            this.cboObjectB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboObjectB.FormattingEnabled = true;
            this.cboObjectB.Location = new System.Drawing.Point(494, 130);
            this.cboObjectB.Name = "cboObjectB";
            this.cboObjectB.Size = new System.Drawing.Size(280, 24);
            this.cboObjectB.TabIndex = 26;
            this.cboObjectB.SelectedIndexChanged += new System.EventHandler(this.cboObjectB_SelectedIndexChanged);
            // 
            // cboDate_B
            // 
            this.cboDate_B.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDate_B.FormattingEnabled = true;
            this.cboDate_B.Location = new System.Drawing.Point(494, 159);
            this.cboDate_B.Name = "cboDate_B";
            this.cboDate_B.Size = new System.Drawing.Size(280, 24);
            this.cboDate_B.TabIndex = 25;
            this.cboDate_B.SelectedIndexChanged += new System.EventHandler(this.cboDate_B_SelectedIndexChanged);
            // 
            // cboInstanceB
            // 
            this.cboInstanceB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboInstanceB.FormattingEnabled = true;
            this.cboInstanceB.Location = new System.Drawing.Point(494, 43);
            this.cboInstanceB.Name = "cboInstanceB";
            this.cboInstanceB.Size = new System.Drawing.Size(280, 24);
            this.cboInstanceB.TabIndex = 23;
            this.cboInstanceB.SelectedIndexChanged += new System.EventHandler(this.cboInstanceB_SelectedIndexedChanged);
            // 
            // cboDatabaseB
            // 
            this.cboDatabaseB.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboDatabaseB.FormattingEnabled = true;
            this.cboDatabaseB.Location = new System.Drawing.Point(494, 72);
            this.cboDatabaseB.Name = "cboDatabaseB";
            this.cboDatabaseB.Size = new System.Drawing.Size(280, 24);
            this.cboDatabaseB.TabIndex = 24;
            this.cboDatabaseB.SelectedIndexChanged += new System.EventHandler(this.cboDatabaseB_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(173, 19);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(17, 17);
            this.label6.TabIndex = 28;
            this.label6.Text = "A";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(491, 19);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(17, 17);
            this.label7.TabIndex = 29;
            this.label7.Text = "B";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.label7);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.cboDatabaseA);
            this.panel1.Controls.Add(this.cboObjectTypeB);
            this.panel1.Controls.Add(this.cboInstanceA);
            this.panel1.Controls.Add(this.cboObjectB);
            this.panel1.Controls.Add(this.cboDate_A);
            this.panel1.Controls.Add(this.cboDate_B);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Controls.Add(this.cboInstanceB);
            this.panel1.Controls.Add(this.cboObjectA);
            this.panel1.Controls.Add(this.cboDatabaseB);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.cboObjectTypeA);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1521, 228);
            this.panel1.TabIndex = 30;
            // 
            // pnlCompare
            // 
            this.pnlCompare.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCompare.Location = new System.Drawing.Point(0, 228);
            this.pnlCompare.Name = "pnlCompare";
            this.pnlCompare.Size = new System.Drawing.Size(1521, 697);
            this.pnlCompare.TabIndex = 32;
            // 
            // DDLCompareTo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1521, 925);
            this.Controls.Add(this.pnlCompare);
            this.Controls.Add(this.panel1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "DDLCompareTo";
            this.Text = "Compare DDL";
            this.Load += new System.EventHandler(this.DDLCompareTo_Load);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox cboDate_A;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cboInstanceA;
        private System.Windows.Forms.ComboBox cboDatabaseA;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cboObjectA;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cboObjectTypeA;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cboObjectTypeB;
        private System.Windows.Forms.ComboBox cboObjectB;
        private System.Windows.Forms.ComboBox cboDate_B;
        private System.Windows.Forms.ComboBox cboInstanceB;
        private System.Windows.Forms.ComboBox cboDatabaseB;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel pnlCompare;
    }
}