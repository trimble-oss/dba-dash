namespace DBADashGUI.Tagging
{
    partial class TagPicker
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
            cboName = new System.Windows.Forms.ComboBox();
            cboValue = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            bttnOK = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            chkAll = new System.Windows.Forms.CheckBox();
            SuspendLayout();
            // 
            // cboName
            // 
            cboName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboName.FormattingEnabled = true;
            cboName.Location = new System.Drawing.Point(74, 29);
            cboName.Name = "cboName";
            cboName.Size = new System.Drawing.Size(205, 28);
            cboName.TabIndex = 0;
            cboName.SelectedIndexChanged += Selected_Tag;
            // 
            // cboValue
            // 
            cboValue.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboValue.FormattingEnabled = true;
            cboValue.Location = new System.Drawing.Point(74, 82);
            cboValue.Name = "cboValue";
            cboValue.Size = new System.Drawing.Size(205, 28);
            cboValue.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 26);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(32, 20);
            label1.TabIndex = 2;
            label1.Text = "Tag";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(12, 87);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(45, 20);
            label2.TabIndex = 3;
            label2.Text = "Value";
            // 
            // bttnOK
            // 
            bttnOK.Location = new System.Drawing.Point(185, 169);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new System.Drawing.Size(94, 29);
            bttnOK.TabIndex = 4;
            bttnOK.Text = "&OK";
            bttnOK.UseVisualStyleBackColor = true;
            bttnOK.Click += bttnOK_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Location = new System.Drawing.Point(74, 169);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 5;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += Cancel_Click;
            // 
            // chkAll
            // 
            chkAll.AutoSize = true;
            chkAll.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            chkAll.Location = new System.Drawing.Point(224, 130);
            chkAll.Name = "chkAll";
            chkAll.Size = new System.Drawing.Size(55, 24);
            chkAll.TabIndex = 6;
            chkAll.Text = "ALL";
            chkAll.UseVisualStyleBackColor = true;
            chkAll.CheckedChanged += All_CheckChanged;
            // 
            // TagPicker
            // 
            AcceptButton = bttnOK;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new System.Drawing.Size(301, 213);
            Controls.Add(chkAll);
            Controls.Add(bttnCancel);
            Controls.Add(bttnOK);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(cboValue);
            Controls.Add(cboName);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            Name = "TagPicker";
            Text = "Tag Picker";
            Load += TagPicker_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ComboBox cboName;
        private System.Windows.Forms.ComboBox cboValue;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.CheckBox chkAll;
    }
}