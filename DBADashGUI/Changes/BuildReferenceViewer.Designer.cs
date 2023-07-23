namespace DBADashGUI.Changes
{
    partial class BuildReferenceViewer
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
            buildReference1 = new BuildReference();
            SuspendLayout();
            // 
            // buildReference1
            // 
            buildReference1.Dock = System.Windows.Forms.DockStyle.Fill;
            buildReference1.Location = new System.Drawing.Point(0, 0);
            buildReference1.Name = "buildReference1";
            buildReference1.SelectedVersion = "";
            buildReference1.Size = new System.Drawing.Size(1162, 521);
            buildReference1.TabIndex = 0;
            // 
            // BuildReferenceViewer
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1162, 521);
            Controls.Add(buildReference1);
            Name = "BuildReferenceViewer";
            Text = "Build Reference Viewer";
            Load += BuildReferenceViewer_Load;
            ResumeLayout(false);
        }

        #endregion

        private BuildReference buildReference1;
    }
}