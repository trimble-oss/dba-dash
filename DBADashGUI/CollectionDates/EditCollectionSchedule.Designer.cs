namespace DBADashGUI.CollectionDates
{
    partial class EditCollectionSchedule
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
            lblTarget = new System.Windows.Forms.Label();
            grpScope = new System.Windows.Forms.GroupBox();
            rbService = new System.Windows.Forms.RadioButton();
            rbInstance = new System.Windows.Forms.RadioButton();
            grpSchedule = new System.Windows.Forms.GroupBox();
            chkRunOnServiceStart = new System.Windows.Forms.CheckBox();
            lblDescription = new System.Windows.Forms.Label();
            lnkBuilder = new System.Windows.Forms.LinkLabel();
            txtSchedule = new System.Windows.Forms.TextBox();
            rbCustom = new System.Windows.Forms.RadioButton();
            rbDisabled = new System.Windows.Forms.RadioButton();
            rbInherit = new System.Windows.Forms.RadioButton();
            bttnOK = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            grpScope.SuspendLayout();
            grpSchedule.SuspendLayout();
            SuspendLayout();
            //
            // lblTarget
            //
            lblTarget.Location = new System.Drawing.Point(12, 9);
            lblTarget.Name = "lblTarget";
            lblTarget.Size = new System.Drawing.Size(560, 66);
            lblTarget.TabIndex = 0;
            lblTarget.Text = "Collections:";
            //
            // grpScope
            //
            grpScope.Controls.Add(rbService);
            grpScope.Controls.Add(rbInstance);
            grpScope.Location = new System.Drawing.Point(12, 78);
            grpScope.Name = "grpScope";
            grpScope.Size = new System.Drawing.Size(560, 92);
            grpScope.TabIndex = 1;
            grpScope.TabStop = false;
            grpScope.Text = "Apply To";
            //
            // rbService
            //
            rbService.AutoSize = true;
            rbService.Location = new System.Drawing.Point(15, 56);
            rbService.Name = "rbService";
            rbService.Size = new System.Drawing.Size(240, 24);
            rbService.TabIndex = 1;
            rbService.Text = "All instances on the service";
            rbService.UseVisualStyleBackColor = true;
            rbService.CheckedChanged += Scope_CheckedChanged;
            //
            // rbInstance
            //
            rbInstance.AutoSize = true;
            rbInstance.Checked = true;
            rbInstance.Location = new System.Drawing.Point(15, 26);
            rbInstance.Name = "rbInstance";
            rbInstance.Size = new System.Drawing.Size(155, 24);
            rbInstance.TabIndex = 0;
            rbInstance.TabStop = true;
            rbInstance.Text = "This instance only";
            rbInstance.UseVisualStyleBackColor = true;
            rbInstance.CheckedChanged += Scope_CheckedChanged;
            //
            // grpSchedule
            //
            grpSchedule.Controls.Add(chkRunOnServiceStart);
            grpSchedule.Controls.Add(lblDescription);
            grpSchedule.Controls.Add(lnkBuilder);
            grpSchedule.Controls.Add(txtSchedule);
            grpSchedule.Controls.Add(rbCustom);
            grpSchedule.Controls.Add(rbDisabled);
            grpSchedule.Controls.Add(rbInherit);
            grpSchedule.Location = new System.Drawing.Point(12, 176);
            grpSchedule.Name = "grpSchedule";
            grpSchedule.Size = new System.Drawing.Size(560, 182);
            grpSchedule.TabIndex = 2;
            grpSchedule.TabStop = false;
            grpSchedule.Text = "Schedule";
            //
            // chkRunOnServiceStart
            //
            chkRunOnServiceStart.AutoSize = true;
            chkRunOnServiceStart.Location = new System.Drawing.Point(15, 148);
            chkRunOnServiceStart.Name = "chkRunOnServiceStart";
            chkRunOnServiceStart.Size = new System.Drawing.Size(172, 24);
            chkRunOnServiceStart.TabIndex = 6;
            chkRunOnServiceStart.Text = "Run on service start";
            chkRunOnServiceStart.UseVisualStyleBackColor = true;
            //
            // lblDescription
            //
            lblDescription.Location = new System.Drawing.Point(111, 116);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new System.Drawing.Size(435, 24);
            lblDescription.TabIndex = 5;
            //
            // lnkBuilder
            //
            lnkBuilder.AutoSize = true;
            lnkBuilder.Location = new System.Drawing.Point(476, 89);
            lnkBuilder.Name = "lnkBuilder";
            lnkBuilder.Size = new System.Drawing.Size(67, 20);
            lnkBuilder.TabIndex = 4;
            lnkBuilder.TabStop = true;
            lnkBuilder.Text = "Builder...";
            lnkBuilder.LinkClicked += LnkBuilder_LinkClicked;
            //
            // txtSchedule
            //
            txtSchedule.Location = new System.Drawing.Point(111, 86);
            txtSchedule.Name = "txtSchedule";
            txtSchedule.Size = new System.Drawing.Size(355, 27);
            txtSchedule.TabIndex = 3;
            txtSchedule.TextChanged += TxtSchedule_TextChanged;
            //
            // rbCustom
            //
            rbCustom.AutoSize = true;
            rbCustom.Location = new System.Drawing.Point(15, 87);
            rbCustom.Name = "rbCustom";
            rbCustom.Size = new System.Drawing.Size(86, 24);
            rbCustom.TabIndex = 2;
            rbCustom.Text = "Custom:";
            rbCustom.UseVisualStyleBackColor = true;
            rbCustom.CheckedChanged += Schedule_CheckedChanged;
            //
            // rbDisabled
            //
            rbDisabled.AutoSize = true;
            rbDisabled.Location = new System.Drawing.Point(15, 56);
            rbDisabled.Name = "rbDisabled";
            rbDisabled.Size = new System.Drawing.Size(88, 24);
            rbDisabled.TabIndex = 1;
            rbDisabled.Text = "Disabled";
            rbDisabled.UseVisualStyleBackColor = true;
            rbDisabled.CheckedChanged += Schedule_CheckedChanged;
            //
            // rbInherit
            //
            rbInherit.AutoSize = true;
            rbInherit.Checked = true;
            rbInherit.Location = new System.Drawing.Point(15, 26);
            rbInherit.Name = "rbInherit";
            rbInherit.Size = new System.Drawing.Size(76, 24);
            rbInherit.TabIndex = 0;
            rbInherit.TabStop = true;
            rbInherit.Text = "Inherit";
            rbInherit.UseVisualStyleBackColor = true;
            rbInherit.CheckedChanged += Schedule_CheckedChanged;
            //
            // bttnOK
            //
            bttnOK.Location = new System.Drawing.Point(378, 370);
            bttnOK.Name = "bttnOK";
            bttnOK.Size = new System.Drawing.Size(94, 29);
            bttnOK.TabIndex = 3;
            bttnOK.Text = "OK";
            bttnOK.UseVisualStyleBackColor = true;
            bttnOK.Click += BttnOK_Click;
            //
            // bttnCancel
            //
            bttnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            bttnCancel.Location = new System.Drawing.Point(478, 370);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 4;
            bttnCancel.Text = "Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            //
            // EditCollectionSchedule
            //
            AcceptButton = bttnOK;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new System.Drawing.Size(584, 411);
            Controls.Add(bttnCancel);
            Controls.Add(bttnOK);
            Controls.Add(grpSchedule);
            Controls.Add(grpScope);
            Controls.Add(lblTarget);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "EditCollectionSchedule";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Edit Collection Schedule";
            Load += EditCollectionSchedule_Load;
            grpScope.ResumeLayout(false);
            grpScope.PerformLayout();
            grpSchedule.ResumeLayout(false);
            grpSchedule.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblTarget;
        private System.Windows.Forms.GroupBox grpScope;
        private System.Windows.Forms.RadioButton rbService;
        private System.Windows.Forms.RadioButton rbInstance;
        private System.Windows.Forms.GroupBox grpSchedule;
        private System.Windows.Forms.CheckBox chkRunOnServiceStart;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.LinkLabel lnkBuilder;
        private System.Windows.Forms.TextBox txtSchedule;
        private System.Windows.Forms.RadioButton rbCustom;
        private System.Windows.Forms.RadioButton rbDisabled;
        private System.Windows.Forms.RadioButton rbInherit;
        private System.Windows.Forms.Button bttnOK;
        private System.Windows.Forms.Button bttnCancel;
    }
}
