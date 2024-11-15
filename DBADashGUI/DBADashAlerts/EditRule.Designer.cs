namespace DBADashGUI.DBADashAlerts
{
    partial class EditRule
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
            cboType = new System.Windows.Forms.ComboBox();
            label1 = new System.Windows.Forms.Label();
            propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            bttnSave = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // cboType
            // 
            cboType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboType.FormattingEnabled = true;
            cboType.Location = new System.Drawing.Point(105, 22);
            cboType.Name = "cboType";
            cboType.Size = new System.Drawing.Size(151, 28);
            cboType.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 22);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(73, 20);
            label1.TabIndex = 1;
            label1.Text = "Rule Type";
            // 
            // propertyGrid1
            // 
            propertyGrid1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            propertyGrid1.Location = new System.Drawing.Point(12, 56);
            propertyGrid1.Name = "propertyGrid1";
            propertyGrid1.Size = new System.Drawing.Size(776, 414);
            propertyGrid1.TabIndex = 2;
            // 
            // bttnSave
            // 
            bttnSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnSave.Location = new System.Drawing.Point(694, 476);
            bttnSave.Name = "bttnSave";
            bttnSave.Size = new System.Drawing.Size(94, 29);
            bttnSave.TabIndex = 3;
            bttnSave.Text = "&Save";
            bttnSave.UseVisualStyleBackColor = true;
            bttnSave.Click += bttnSave_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            bttnCancel.Location = new System.Drawing.Point(594, 476);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 4;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += bttnCancel_Click;
            // 
            // EditRule
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 517);
            Controls.Add(bttnCancel);
            Controls.Add(bttnSave);
            Controls.Add(propertyGrid1);
            Controls.Add(label1);
            Controls.Add(cboType);
            Name = "EditRule";
            Text = "Edit Rule";
            Load += EditRule_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ComboBox cboType;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private System.Windows.Forms.Button bttnSave;
        private System.Windows.Forms.Button bttnCancel;
    }
}