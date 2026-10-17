namespace DBADashGUI.CollectionDates
{
    partial class PauseCollections
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
            lblInstances = new System.Windows.Forms.Label();
            grpPause = new System.Windows.Forms.GroupBox();
            dtpUntil = new System.Windows.Forms.DateTimePicker();
            rbUntil = new System.Windows.Forms.RadioButton();
            cboDuration = new System.Windows.Forms.ComboBox();
            rbFor = new System.Windows.Forms.RadioButton();
            lblNote = new System.Windows.Forms.Label();
            bttnPause = new System.Windows.Forms.Button();
            bttnResume = new System.Windows.Forms.Button();
            bttnCancel = new System.Windows.Forms.Button();
            grpPause.SuspendLayout();
            SuspendLayout();
            //
            // lblInstances
            //
            lblInstances.Location = new System.Drawing.Point(12, 9);
            lblInstances.Name = "lblInstances";
            lblInstances.Size = new System.Drawing.Size(520, 84);
            lblInstances.TabIndex = 0;
            lblInstances.Text = "Instances:";
            //
            // grpPause
            //
            grpPause.Controls.Add(dtpUntil);
            grpPause.Controls.Add(rbUntil);
            grpPause.Controls.Add(cboDuration);
            grpPause.Controls.Add(rbFor);
            grpPause.Location = new System.Drawing.Point(12, 96);
            grpPause.Name = "grpPause";
            grpPause.Size = new System.Drawing.Size(520, 96);
            grpPause.TabIndex = 1;
            grpPause.TabStop = false;
            grpPause.Text = "Pause";
            //
            // dtpUntil
            //
            dtpUntil.CustomFormat = "yyyy-MM-dd HH:mm";
            dtpUntil.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            dtpUntil.Location = new System.Drawing.Point(130, 58);
            dtpUntil.Name = "dtpUntil";
            dtpUntil.Size = new System.Drawing.Size(200, 27);
            dtpUntil.TabIndex = 3;
            dtpUntil.ValueChanged += Pause_Changed;
            //
            // rbUntil
            //
            rbUntil.AutoSize = true;
            rbUntil.Location = new System.Drawing.Point(15, 59);
            rbUntil.Name = "rbUntil";
            rbUntil.Size = new System.Drawing.Size(65, 24);
            rbUntil.TabIndex = 2;
            rbUntil.Text = "Until:";
            rbUntil.UseVisualStyleBackColor = true;
            rbUntil.CheckedChanged += Pause_Changed;
            //
            // cboDuration
            //
            cboDuration.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cboDuration.Location = new System.Drawing.Point(130, 24);
            cboDuration.Name = "cboDuration";
            cboDuration.Size = new System.Drawing.Size(200, 28);
            cboDuration.TabIndex = 1;
            cboDuration.SelectedIndexChanged += Pause_Changed;
            //
            // rbFor
            //
            rbFor.AutoSize = true;
            rbFor.Checked = true;
            rbFor.Location = new System.Drawing.Point(15, 25);
            rbFor.Name = "rbFor";
            rbFor.Size = new System.Drawing.Size(55, 24);
            rbFor.TabIndex = 0;
            rbFor.TabStop = true;
            rbFor.Text = "For:";
            rbFor.UseVisualStyleBackColor = true;
            rbFor.CheckedChanged += Pause_Changed;
            //
            // lblNote
            //
            lblNote.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            lblNote.Location = new System.Drawing.Point(12, 198);
            lblNote.Name = "lblNote";
            lblNote.Size = new System.Drawing.Size(520, 60);
            lblNote.TabIndex = 2;
            lblNote.Text = "Scheduled collections are skipped and alerts are muted until the pause ends.  Collections you trigger manually still run.  Survives a service restart.";
            //
            // bttnPause
            //
            bttnPause.Location = new System.Drawing.Point(238, 265);
            bttnPause.Name = "bttnPause";
            bttnPause.Size = new System.Drawing.Size(94, 29);
            bttnPause.TabIndex = 3;
            bttnPause.Text = "Pause";
            bttnPause.UseVisualStyleBackColor = true;
            bttnPause.Click += BttnPause_Click;
            //
            // bttnResume
            //
            bttnResume.Location = new System.Drawing.Point(338, 265);
            bttnResume.Name = "bttnResume";
            bttnResume.Size = new System.Drawing.Size(94, 29);
            bttnResume.TabIndex = 4;
            bttnResume.Text = "Resume";
            bttnResume.UseVisualStyleBackColor = true;
            bttnResume.Click += BttnResume_Click;
            //
            // bttnCancel
            //
            bttnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            bttnCancel.Location = new System.Drawing.Point(438, 265);
            bttnCancel.Name = "bttnCancel";
            bttnCancel.Size = new System.Drawing.Size(94, 29);
            bttnCancel.TabIndex = 5;
            bttnCancel.Text = "Cancel";
            bttnCancel.UseVisualStyleBackColor = true;
            //
            // PauseCollections
            //
            AcceptButton = bttnPause;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = bttnCancel;
            ClientSize = new System.Drawing.Size(544, 306);
            Controls.Add(bttnCancel);
            Controls.Add(bttnResume);
            Controls.Add(bttnPause);
            Controls.Add(lblNote);
            Controls.Add(grpPause);
            Controls.Add(lblInstances);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PauseCollections";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Pause Collections";
            Load += PauseCollections_Load;
            grpPause.ResumeLayout(false);
            grpPause.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblInstances;
        private System.Windows.Forms.GroupBox grpPause;
        private System.Windows.Forms.DateTimePicker dtpUntil;
        private System.Windows.Forms.RadioButton rbUntil;
        private System.Windows.Forms.ComboBox cboDuration;
        private System.Windows.Forms.RadioButton rbFor;
        private System.Windows.Forms.Label lblNote;
        private System.Windows.Forms.Button bttnPause;
        private System.Windows.Forms.Button bttnResume;
        private System.Windows.Forms.Button bttnCancel;
    }
}
