namespace DBAChecksGUI.Changes
{
    partial class AzureServiceObjectivesHistory
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
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsCopy = new System.Windows.Forms.ToolStripButton();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn8 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn9 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn10 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colInstance = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEdition = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEditionNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colServiceObjective = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colServiceObjectiveNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPoolName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colPoolNameNew = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValidFrom = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colValidTo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.toolStrip1.SuspendLayout();
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
            this.colInstance,
            this.colDB,
            this.colEdition,
            this.colEditionNew,
            this.colServiceObjective,
            this.colServiceObjectiveNew,
            this.colPoolName,
            this.colPoolNameNew,
            this.colValidFrom,
            this.colValidTo});
            this.dgv.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv.Location = new System.Drawing.Point(0, 31);
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.RowHeadersWidth = 51;
            this.dgv.RowTemplate.Height = 24;
            this.dgv.Size = new System.Drawing.Size(1344, 723);
            this.dgv.TabIndex = 0;
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsRefresh,
            this.tsCopy});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1344, 31);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tsRefresh
            // 
            this.tsRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsRefresh.Image = global::DBAChecksGUI.Properties.Resources._112_RefreshArrow_Green_16x16_72;
            this.tsRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsRefresh.Name = "tsRefresh";
            this.tsRefresh.Size = new System.Drawing.Size(29, 28);
            this.tsRefresh.Text = "Refresh";
            this.tsRefresh.Click += new System.EventHandler(this.tsRefresh_Click);
            // 
            // tsCopy
            // 
            this.tsCopy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsCopy.Image = global::DBAChecksGUI.Properties.Resources.ASX_Copy_blue_16x;
            this.tsCopy.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsCopy.Name = "tsCopy";
            this.tsCopy.Size = new System.Drawing.Size(29, 28);
            this.tsCopy.Text = "Copy";
            this.tsCopy.Click += new System.EventHandler(this.tsCopy_Click);
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "Instance";
            this.dataGridViewTextBoxColumn1.HeaderText = "Instance";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.Width = 90;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.DataPropertyName = "db";
            this.dataGridViewTextBoxColumn2.HeaderText = "DB";
            this.dataGridViewTextBoxColumn2.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.ReadOnly = true;
            this.dataGridViewTextBoxColumn2.Width = 56;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.DataPropertyName = "edition";
            this.dataGridViewTextBoxColumn3.HeaderText = "Edition (Old)";
            this.dataGridViewTextBoxColumn3.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 116;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.DataPropertyName = "new_edition";
            this.dataGridViewTextBoxColumn4.HeaderText = "Edition (New)";
            this.dataGridViewTextBoxColumn4.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.ReadOnly = true;
            this.dataGridViewTextBoxColumn4.Width = 121;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.DataPropertyName = "service_objective";
            this.dataGridViewTextBoxColumn5.HeaderText = "Service Objective (Old)";
            this.dataGridViewTextBoxColumn5.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.ReadOnly = true;
            this.dataGridViewTextBoxColumn5.Width = 138;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.DataPropertyName = "service_objective_new";
            this.dataGridViewTextBoxColumn6.HeaderText = "Service Objective (New)";
            this.dataGridViewTextBoxColumn6.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.ReadOnly = true;
            this.dataGridViewTextBoxColumn6.Width = 138;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.DataPropertyName = "elastic_pool_name";
            this.dataGridViewTextBoxColumn7.HeaderText = "Pool Name";
            this.dataGridViewTextBoxColumn7.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.ReadOnly = true;
            this.dataGridViewTextBoxColumn7.Width = 98;
            // 
            // dataGridViewTextBoxColumn8
            // 
            this.dataGridViewTextBoxColumn8.DataPropertyName = "new_elastic_pool_name";
            this.dataGridViewTextBoxColumn8.HeaderText = "Pool Name (New)";
            this.dataGridViewTextBoxColumn8.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn8.Name = "dataGridViewTextBoxColumn8";
            this.dataGridViewTextBoxColumn8.ReadOnly = true;
            this.dataGridViewTextBoxColumn8.Width = 135;
            // 
            // dataGridViewTextBoxColumn9
            // 
            this.dataGridViewTextBoxColumn9.DataPropertyName = "ValidFrom";
            this.dataGridViewTextBoxColumn9.HeaderText = "Old Config Valid From";
            this.dataGridViewTextBoxColumn9.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn9.Name = "dataGridViewTextBoxColumn9";
            this.dataGridViewTextBoxColumn9.ReadOnly = true;
            this.dataGridViewTextBoxColumn9.Width = 130;
            // 
            // dataGridViewTextBoxColumn10
            // 
            this.dataGridViewTextBoxColumn10.DataPropertyName = "ValidTo";
            this.dataGridViewTextBoxColumn10.HeaderText = "Change Date";
            this.dataGridViewTextBoxColumn10.MinimumWidth = 6;
            this.dataGridViewTextBoxColumn10.Name = "dataGridViewTextBoxColumn10";
            this.dataGridViewTextBoxColumn10.ReadOnly = true;
            this.dataGridViewTextBoxColumn10.Width = 110;
            // 
            // colInstance
            // 
            this.colInstance.DataPropertyName = "Instance";
            this.colInstance.HeaderText = "Instance";
            this.colInstance.MinimumWidth = 6;
            this.colInstance.Name = "colInstance";
            this.colInstance.ReadOnly = true;
            this.colInstance.Width = 90;
            // 
            // colDB
            // 
            this.colDB.DataPropertyName = "db";
            this.colDB.HeaderText = "DB";
            this.colDB.MinimumWidth = 6;
            this.colDB.Name = "colDB";
            this.colDB.ReadOnly = true;
            this.colDB.Width = 56;
            // 
            // colEdition
            // 
            this.colEdition.DataPropertyName = "edition";
            this.colEdition.HeaderText = "Edition (Old)";
            this.colEdition.MinimumWidth = 6;
            this.colEdition.Name = "colEdition";
            this.colEdition.ReadOnly = true;
            this.colEdition.Width = 116;
            // 
            // colEditionNew
            // 
            this.colEditionNew.DataPropertyName = "new_edition";
            this.colEditionNew.HeaderText = "Edition (New)";
            this.colEditionNew.MinimumWidth = 6;
            this.colEditionNew.Name = "colEditionNew";
            this.colEditionNew.ReadOnly = true;
            this.colEditionNew.Width = 121;
            // 
            // colServiceObjective
            // 
            this.colServiceObjective.DataPropertyName = "service_objective";
            this.colServiceObjective.HeaderText = "Service Objective (Old)";
            this.colServiceObjective.MinimumWidth = 6;
            this.colServiceObjective.Name = "colServiceObjective";
            this.colServiceObjective.ReadOnly = true;
            this.colServiceObjective.Width = 138;
            // 
            // colServiceObjectiveNew
            // 
            this.colServiceObjectiveNew.DataPropertyName = "new_service_objective";
            this.colServiceObjectiveNew.HeaderText = "Service Objective (New)";
            this.colServiceObjectiveNew.MinimumWidth = 6;
            this.colServiceObjectiveNew.Name = "colServiceObjectiveNew";
            this.colServiceObjectiveNew.ReadOnly = true;
            this.colServiceObjectiveNew.Width = 138;
            // 
            // colPoolName
            // 
            this.colPoolName.DataPropertyName = "elastic_pool_name";
            this.colPoolName.HeaderText = "Pool Name";
            this.colPoolName.MinimumWidth = 6;
            this.colPoolName.Name = "colPoolName";
            this.colPoolName.ReadOnly = true;
            this.colPoolName.Width = 98;
            // 
            // colPoolNameNew
            // 
            this.colPoolNameNew.DataPropertyName = "new_elastic_pool_name";
            this.colPoolNameNew.HeaderText = "Pool Name (New)";
            this.colPoolNameNew.MinimumWidth = 6;
            this.colPoolNameNew.Name = "colPoolNameNew";
            this.colPoolNameNew.ReadOnly = true;
            this.colPoolNameNew.Width = 135;
            // 
            // colValidFrom
            // 
            this.colValidFrom.DataPropertyName = "ValidFrom";
            this.colValidFrom.HeaderText = "Old Config Valid From";
            this.colValidFrom.MinimumWidth = 6;
            this.colValidFrom.Name = "colValidFrom";
            this.colValidFrom.ReadOnly = true;
            this.colValidFrom.Width = 130;
            // 
            // colValidTo
            // 
            this.colValidTo.DataPropertyName = "ValidTo";
            this.colValidTo.HeaderText = "Change Date";
            this.colValidTo.MinimumWidth = 6;
            this.colValidTo.Name = "colValidTo";
            this.colValidTo.ReadOnly = true;
            this.colValidTo.Width = 110;
            // 
            // AzureServiceObjectivesHistory
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.toolStrip1);
            this.Name = "AzureServiceObjectivesHistory";
            this.Size = new System.Drawing.Size(1344, 754);
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgv;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsRefresh;
        private System.Windows.Forms.ToolStripButton tsCopy;
        private System.Windows.Forms.DataGridViewTextBoxColumn colInstance;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEdition;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEditionNew;
        private System.Windows.Forms.DataGridViewTextBoxColumn colServiceObjective;
        private System.Windows.Forms.DataGridViewTextBoxColumn colServiceObjectiveNew;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPoolName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colPoolNameNew;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValidFrom;
        private System.Windows.Forms.DataGridViewTextBoxColumn colValidTo;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn8;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn9;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn10;
    }
}
