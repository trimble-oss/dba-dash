namespace DBAChecksGUI
{
    partial class SQLPatching
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.dgv = new System.Windows.Forms.DataGridView();
            this.Instance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ChangedDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.OldVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NewVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.OldProductLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NewProductLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.OldProductUpdateLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NewProductUpdateLevel = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.OldEdition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NewEdition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.SuspendLayout();
            // 
            // dgv
            // 
            this.dgv.AllowUserToAddRows = false;
            this.dgv.AllowUserToDeleteRows = false;
            this.dgv.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Instance,
            this.ChangedDate,
            this.OldVersion,
            this.NewVersion,
            this.OldProductLevel,
            this.NewProductLevel,
            this.OldProductUpdateLevel,
            this.NewProductUpdateLevel,
            this.OldEdition,
            this.NewEdition});
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 0);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.Size = new System.Drawing.Size(150, 150);
            this.dgv.TabIndex = 0;
            // 
            // Instance
            // 
            this.Instance.DataPropertyName = "Instance";
            this.Instance.HeaderText = "Instance";
            this.Instance.MinimumWidth = 6;
            this.Instance.Name = "Instance";
            this.Instance.ReadOnly = true;
            this.Instance.Width = 90;
            // 
            // ChangedDate
            // 
            this.ChangedDate.DataPropertyName = "ChangedDate";
            this.ChangedDate.HeaderText = "Changed Date";
            this.ChangedDate.MinimumWidth = 6;
            this.ChangedDate.Name = "ChangedDate";
            this.ChangedDate.ReadOnly = true;
            this.ChangedDate.Width = 128;
            // 
            // OldVersion
            // 
            this.OldVersion.DataPropertyName = "OldVersion";
            this.OldVersion.HeaderText = "Old Version";
            this.OldVersion.MinimumWidth = 6;
            this.OldVersion.Name = "OldVersion";
            this.OldVersion.ReadOnly = true;
            this.OldVersion.Width = 111;
            // 
            // NewVersion
            // 
            this.NewVersion.DataPropertyName = "NewVersion";
            this.NewVersion.HeaderText = "New Version";
            this.NewVersion.MinimumWidth = 6;
            this.NewVersion.Name = "NewVersion";
            this.NewVersion.ReadOnly = true;
            this.NewVersion.Width = 116;
            // 
            // OldProductLevel
            // 
            this.OldProductLevel.DataPropertyName = "OldProductLevel";
            this.OldProductLevel.HeaderText = "Old Product Level";
            this.OldProductLevel.MinimumWidth = 6;
            this.OldProductLevel.Name = "OldProductLevel";
            this.OldProductLevel.ReadOnly = true;
            this.OldProductLevel.Width = 137;
            // 
            // NewProductLevel
            // 
            this.NewProductLevel.DataPropertyName = "NewProductLevel";
            this.NewProductLevel.HeaderText = "New Product Level";
            this.NewProductLevel.MinimumWidth = 6;
            this.NewProductLevel.Name = "NewProductLevel";
            this.NewProductLevel.ReadOnly = true;
            this.NewProductLevel.Width = 142;
            // 
            // OldProductUpdateLevel
            // 
            this.OldProductUpdateLevel.DataPropertyName = "OldProductUpdateLevel";
            this.OldProductUpdateLevel.HeaderText = "Old Product Update Level";
            this.OldProductUpdateLevel.MinimumWidth = 6;
            this.OldProductUpdateLevel.Name = "OldProductUpdateLevel";
            this.OldProductUpdateLevel.ReadOnly = true;
            this.OldProductUpdateLevel.Width = 152;
            // 
            // NewProductUpdateLevel
            // 
            this.NewProductUpdateLevel.DataPropertyName = "NewProductUpdateLevel";
            this.NewProductUpdateLevel.HeaderText = "New Product Update Level";
            this.NewProductUpdateLevel.MinimumWidth = 6;
            this.NewProductUpdateLevel.Name = "NewProductUpdateLevel";
            this.NewProductUpdateLevel.ReadOnly = true;
            this.NewProductUpdateLevel.Width = 156;
            // 
            // OldEdition
            // 
            this.OldEdition.DataPropertyName = "OldEdition";
            this.OldEdition.HeaderText = "Old Edition";
            this.OldEdition.MinimumWidth = 6;
            this.OldEdition.Name = "OldEdition";
            this.OldEdition.ReadOnly = true;
            this.OldEdition.Width = 98;
            // 
            // NewEdition
            // 
            this.NewEdition.DataPropertyName = "NewEdition";
            this.NewEdition.HeaderText = "New Edition";
            this.NewEdition.MinimumWidth = 6;
            this.NewEdition.Name = "NewEdition";
            this.NewEdition.ReadOnly = true;
            this.NewEdition.Width = 102;
            // 
            // SQLPatching
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgv);
            this.Name = "SQLPatching";
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.DataGridViewTextBoxColumn Instance;
        private System.Windows.Forms.DataGridViewTextBoxColumn ChangedDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn OldVersion;
        private System.Windows.Forms.DataGridViewTextBoxColumn NewVersion;
        private System.Windows.Forms.DataGridViewTextBoxColumn OldProductLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn NewProductLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn OldProductUpdateLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn NewProductUpdateLevel;
        private System.Windows.Forms.DataGridViewTextBoxColumn OldEdition;
        private System.Windows.Forms.DataGridViewTextBoxColumn NewEdition;
    }
}
