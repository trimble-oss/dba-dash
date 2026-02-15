namespace DBADashGUI.Performance
{
    partial class ResourceGovernorPerformance
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
            tabs = new DBADashGUI.Theme.ThemedTabControl();
            tabWorkloadGroups = new System.Windows.Forms.TabPage();
            resourceGovernorWorkloadGroupsMetrics1 = new ResourceGovernorWorkloadGroupsMetrics();
            tabPools = new System.Windows.Forms.TabPage();
            resourceGovernorResourcePools1 = new ResourceGovernorResourcePools();
            tabs.SuspendLayout();
            tabWorkloadGroups.SuspendLayout();
            tabPools.SuspendLayout();
            SuspendLayout();
            // 
            // tabs
            // 
            tabs.Controls.Add(tabWorkloadGroups);
            tabs.Controls.Add(tabPools);
            tabs.Dock = System.Windows.Forms.DockStyle.Fill;
            tabs.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            tabs.Location = new System.Drawing.Point(0, 0);
            tabs.Multiline = true;
            tabs.Name = "tabs";
            tabs.Padding = new System.Drawing.Point(20, 8);
            tabs.SelectedIndex = 0;
            tabs.Size = new System.Drawing.Size(1077, 770);
            tabs.TabIndex = 0;
            tabs.SelectedIndexChanged += Tabs_SelectedIndexChanged;
            // 
            // tabWorkloadGroups
            // 
            tabWorkloadGroups.Controls.Add(resourceGovernorWorkloadGroupsMetrics1);
            tabWorkloadGroups.Location = new System.Drawing.Point(4, 39);
            tabWorkloadGroups.Name = "tabWorkloadGroups";
            tabWorkloadGroups.Padding = new System.Windows.Forms.Padding(3);
            tabWorkloadGroups.Size = new System.Drawing.Size(1069, 727);
            tabWorkloadGroups.TabIndex = 0;
            tabWorkloadGroups.Text = "Workload Groups";
            tabWorkloadGroups.UseVisualStyleBackColor = true;
            // 
            // resourceGovernorWorkloadGroupsMetrics1
            // 
            resourceGovernorWorkloadGroupsMetrics1.Dock = System.Windows.Forms.DockStyle.Fill;
            resourceGovernorWorkloadGroupsMetrics1.Location = new System.Drawing.Point(3, 3);
            resourceGovernorWorkloadGroupsMetrics1.Name = "resourceGovernorWorkloadGroupsMetrics1";
            resourceGovernorWorkloadGroupsMetrics1.Size = new System.Drawing.Size(1063, 721);
            resourceGovernorWorkloadGroupsMetrics1.TabIndex = 0;
            // 
            // tabPools
            // 
            tabPools.Controls.Add(resourceGovernorResourcePools1);
            tabPools.Location = new System.Drawing.Point(4, 39);
            tabPools.Name = "tabPools";
            tabPools.Padding = new System.Windows.Forms.Padding(3);
            tabPools.Size = new System.Drawing.Size(1069, 727);
            tabPools.TabIndex = 1;
            tabPools.Text = "Pools";
            tabPools.UseVisualStyleBackColor = true;
            // 
            // resourceGovernorResourcePools1
            // 
            resourceGovernorResourcePools1.Dock = System.Windows.Forms.DockStyle.Fill;
            resourceGovernorResourcePools1.Location = new System.Drawing.Point(3, 3);
            resourceGovernorResourcePools1.Name = "resourceGovernorResourcePools1";
            resourceGovernorResourcePools1.Size = new System.Drawing.Size(1063, 721);
            resourceGovernorResourcePools1.TabIndex = 0;
            // 
            // ResourceGovernorPerformance
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(tabs);
            Name = "ResourceGovernorPerformance";
            Size = new System.Drawing.Size(1077, 770);
            tabs.ResumeLayout(false);
            tabWorkloadGroups.ResumeLayout(false);
            tabPools.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Theme.ThemedTabControl tabs;
        private System.Windows.Forms.TabPage tabWorkloadGroups;
        private System.Windows.Forms.TabPage tabPools;
        private ResourceGovernorWorkloadGroupsMetrics resourceGovernorWorkloadGroupsMetrics1;
        private ResourceGovernorResourcePools resourceGovernorResourcePools1;
    }
}
