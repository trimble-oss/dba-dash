namespace DBADashGUI.SchemaCompare
{
    partial class ExternalDiffConfig
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
            txtPath = new System.Windows.Forms.TextBox();
            txtArgs = new System.Windows.Forms.TextBox();
            lblDiffToolPath = new System.Windows.Forms.Label();
            lblDiffToolArgs = new System.Windows.Forms.Label();
            cboDiffTool = new System.Windows.Forms.ComboBox();
            label3 = new System.Windows.Forms.Label();
            bttnSave = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            label4 = new System.Windows.Forms.Label();
            bttnOpen = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // txtPath
            // 
            txtPath.Location = new System.Drawing.Point(219, 106);
            txtPath.Name = "txtPath";
            txtPath.Size = new System.Drawing.Size(333, 27);
            txtPath.TabIndex = 0;
            // 
            // txtArgs
            // 
            txtArgs.Location = new System.Drawing.Point(219, 139);
            txtArgs.Name = "txtArgs";
            txtArgs.Size = new System.Drawing.Size(373, 27);
            txtArgs.TabIndex = 1;
            // 
            // lblDiffToolPath
            // 
            lblDiffToolPath.AutoSize = true;
            lblDiffToolPath.Location = new System.Drawing.Point(12, 109);
            lblDiffToolPath.Name = "lblDiffToolPath";
            lblDiffToolPath.Size = new System.Drawing.Size(102, 20);
            lblDiffToolPath.TabIndex = 2;
            lblDiffToolPath.Text = "Diff Tool Path:";
            // 
            // lblDiffToolArgs
            // 
            lblDiffToolArgs.AutoSize = true;
            lblDiffToolArgs.Location = new System.Drawing.Point(12, 142);
            lblDiffToolArgs.Name = "lblDiffToolArgs";
            lblDiffToolArgs.Size = new System.Drawing.Size(146, 20);
            lblDiffToolArgs.TabIndex = 3;
            lblDiffToolArgs.Text = "Diff Tool Arguments:";
            // 
            // cboDiffTool
            // 
            cboDiffTool.FormattingEnabled = true;
            cboDiffTool.Location = new System.Drawing.Point(219, 72);
            cboDiffTool.Name = "cboDiffTool";
            cboDiffTool.Size = new System.Drawing.Size(373, 28);
            cboDiffTool.TabIndex = 4;
            cboDiffTool.SelectedIndexChanged += SelectDiffTool;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(12, 75);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(82, 20);
            label3.TabIndex = 5;
            label3.Text = "Select Tool";
            // 
            // bttnSave
            // 
            bttnSave.Location = new System.Drawing.Point(498, 188);
            bttnSave.Name = "bttnSave";
            bttnSave.Size = new System.Drawing.Size(94, 29);
            bttnSave.TabIndex = 6;
            bttnSave.Text = "&Save";
            bttnSave.UseVisualStyleBackColor = true;
            bttnSave.Click += bttnSave_Click;
            // 
            // bttnCancel
            // 
            bttnCancel.Location = new System.Drawing.Point(398, 188);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 7;
            bttnCancel.Text = "&Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            bttnCancel.Click += BttnCancel_Click;
            // 
            // label4
            // 
            label4.Location = new System.Drawing.Point(12, 9);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(580, 54);
            label4.TabIndex = 8;
            label4.Text = "Configure external diff tool.  Select 'None' to use built in diff or select a diff tool.  Use $OLD$ and $NEW$ as placeholders for file names in arguments.";
            // 
            // bttnOpen
            // 
            bttnOpen.Image = Properties.Resources.FolderOpened_16x;
            bttnOpen.Location = new System.Drawing.Point(558, 104);
            bttnOpen.Name = "bttnOpen";
            bttnOpen.Size = new System.Drawing.Size(34, 29);
            bttnOpen.TabIndex = 9;
            bttnOpen.UseVisualStyleBackColor = true;
            bttnOpen.Click += BttnOpen_Click;
            // 
            // ExternalDiffConfig
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(618, 239);
            Controls.Add(bttnOpen);
            Controls.Add(label4);
            Controls.Add(bttnCancel);
            Controls.Add(bttnSave);
            Controls.Add(label3);
            Controls.Add(cboDiffTool);
            Controls.Add(lblDiffToolArgs);
            Controls.Add(lblDiffToolPath);
            Controls.Add(txtArgs);
            Controls.Add(txtPath);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            Name = "ExternalDiffConfig";
            Text = "External Diff Config";
            Load += ExternalDiffConfig_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.TextBox txtArgs;
        private System.Windows.Forms.Label lblDiffToolPath;
        private System.Windows.Forms.Label lblDiffToolArgs;
        private System.Windows.Forms.ComboBox cboDiffTool;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button bttnSave;
        private System.Windows.Forms.Button bttnCancel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button bttnOpen;
    }
}