namespace DBADashGUI.Performance
{
    partial class QueryStoreViewer
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
            queryStoreTopQueries1 = new QueryStoreTopQueries();
            SuspendLayout();
            // 
            // queryStoreTopQueries1
            // 
            queryStoreTopQueries1.Dock = System.Windows.Forms.DockStyle.Fill;
            queryStoreTopQueries1.Location = new System.Drawing.Point(0, 0);
            queryStoreTopQueries1.Name = "queryStoreTopQueries1";
            queryStoreTopQueries1.PlanHash = null;
            queryStoreTopQueries1.QueryHash = null;
            queryStoreTopQueries1.Size = new System.Drawing.Size(1471, 796);
            queryStoreTopQueries1.TabIndex = 0;
            queryStoreTopQueries1.UseGlobalTime = false;
            // 
            // QueryStoreViewer
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1471, 796);
            Controls.Add(queryStoreTopQueries1);
            Name = "QueryStoreViewer";
            Text = "Query Store Viewer";
            Load += QueryStoreViewer_Load;
            ResumeLayout(false);
        }

        #endregion

        private QueryStoreTopQueries queryStoreTopQueries1;
    }
}