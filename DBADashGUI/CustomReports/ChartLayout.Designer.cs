namespace DBADashGUI.CustomReports
{
    partial class ChartLayout
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
            numRows = new System.Windows.Forms.NumericUpDown();
            numColumns = new System.Windows.Forms.NumericUpDown();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            bttnOK = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            lblSpan = new System.Windows.Forms.Label();
            chkAuto = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)numRows).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numColumns).BeginInit();
            SuspendLayout();
            // 
            // numRows
            // 
            numRows.Enabled = false;
            numRows.Location = new System.Drawing.Point(12, 46);
            numRows.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numRows.Name = "numRows";
            numRows.Size = new System.Drawing.Size(150, 27);
            numRows.TabIndex = 0;
            numRows.Value = new decimal(new int[] { 1, 0, 0, 0 });
            numRows.ValueChanged += Rows_Changed;
            // 
            // numColumns
            // 
            numColumns.Location = new System.Drawing.Point(168, 46);
            numColumns.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numColumns.Name = "numColumns";
            numColumns.Size = new System.Drawing.Size(150, 27);
            numColumns.TabIndex = 1;
            numColumns.Value = new decimal(new int[] { 1, 0, 0, 0 });
            numColumns.ValueChanged += Columns_Changed;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(12, 23);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(47, 20);
            label1.TabIndex = 2;
            label1.Text = "Rows:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(168, 23);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(69, 20);
            label2.TabIndex = 3;
            label2.Text = "Columns:";
            // 
            // bttnOK
            // 
            bttnOK.Location = new System.Drawing.Point(233, 110);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new System.Drawing.Size(94, 29);
            bttnOK.TabIndex = 4;
            bttnOK.Text = "&OK";
            bttnOK.UseVisualStyleBackColor = true;
            bttnOK.Click += OK_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.CausesValidation = false;
            bttnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            bttnCancel.Location = new System.Drawing.Point(118, 110);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 5;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            // 
            // lblSpan
            // 
            lblSpan.AutoSize = true;
            lblSpan.Location = new System.Drawing.Point(12, 160);
            lblSpan.Name = "lblSpan";
            lblSpan.Size = new System.Drawing.Size(0, 20);
            lblSpan.TabIndex = 6;
            // 
            // chkAuto
            // 
            chkAuto.AutoSize = true;
            chkAuto.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            chkAuto.Location = new System.Drawing.Point(255, 80);
            chkAuto.Name = "chkAuto";
            chkAuto.Size = new System.Drawing.Size(63, 24);
            chkAuto.TabIndex = 7;
            chkAuto.Text = "Auto";
            chkAuto.UseVisualStyleBackColor = true;
            chkAuto.CheckedChanged += Auto_CheckChanged;
            // 
            // ChartLayout
            // 
            AcceptButton = bttnOK;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new System.Drawing.Size(337, 189);
            Controls.Add(chkAuto);
            Controls.Add(lblSpan);
            Controls.Add(bttnCancel);
            Controls.Add(bttnOK);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(numColumns);
            Controls.Add(numRows);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            Name = "ChartLayout";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Chart Layout";
            Load += ChartLayout_Load;
            ((System.ComponentModel.ISupportInitialize)numRows).EndInit();
            ((System.ComponentModel.ISupportInitialize)numColumns).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.NumericUpDown numRows;
        private System.Windows.Forms.NumericUpDown numColumns;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Label lblSpan;
        private System.Windows.Forms.CheckBox chkAuto;
    }
}