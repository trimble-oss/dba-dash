namespace DBADashGUI.XETrace
{
    partial class ExtendedEventsViewer
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
            _toolbar = new System.Windows.Forms.ToolStrip();
            _refreshButton = new System.Windows.Forms.ToolStripButton();
            _adhocButton = new System.Windows.Forms.ToolStripButton();
            _list = new System.Windows.Forms.FlowLayoutPanel();
            _statusStrip = new System.Windows.Forms.StatusStrip();
            _status = new System.Windows.Forms.ToolStripStatusLabel();
            _toolbar.SuspendLayout();
            _statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // _toolbar
            // 
            _toolbar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            _toolbar.ImageScalingSize = new System.Drawing.Size(20, 20);
            _toolbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _refreshButton, _adhocButton });
            _toolbar.Location = new System.Drawing.Point(0, 0);
            _toolbar.Name = "_toolbar";
            _toolbar.Size = new System.Drawing.Size(1263, 27);
            _toolbar.TabIndex = 1;
            // 
            // _refreshButton
            // 
            _refreshButton.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            _refreshButton.Name = "_refreshButton";
            _refreshButton.Size = new System.Drawing.Size(82, 24);
            _refreshButton.Text = "Refresh";
            // 
            // _adhocButton
            // 
            _adhocButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            _adhocButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            _adhocButton.Name = "_adhocButton";
            _adhocButton.Size = new System.Drawing.Size(144, 24);
            _adhocButton.Text = "New Ad-hoc Trace...";
            //
            // _list
            //
            _list.AutoScroll = true;
            _list.Dock = System.Windows.Forms.DockStyle.Fill;
            _list.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            _list.Location = new System.Drawing.Point(0, 27);
            _list.Name = "_list";
            _list.Size = new System.Drawing.Size(1263, 719);
            _list.TabIndex = 0;
            _list.WrapContents = false;
            //
            // _statusStrip
            // 
            _statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            _statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _status });
            _statusStrip.Location = new System.Drawing.Point(0, 746);
            _statusStrip.Name = "_statusStrip";
            _statusStrip.Size = new System.Drawing.Size(1263, 22);
            _statusStrip.TabIndex = 2;
            // 
            // _status
            // 
            _status.Name = "_status";
            _status.Size = new System.Drawing.Size(0, 16);
            // 
            // ExtendedEventsViewer
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(_list);
            Controls.Add(_toolbar);
            Controls.Add(_statusStrip);
            Name = "ExtendedEventsViewer";
            Size = new System.Drawing.Size(1263, 768);
            _toolbar.ResumeLayout(false);
            _toolbar.PerformLayout();
            _statusStrip.ResumeLayout(false);
            _statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip _toolbar;
        private System.Windows.Forms.ToolStripButton _refreshButton;
        private System.Windows.Forms.ToolStripButton _adhocButton;
        private System.Windows.Forms.FlowLayoutPanel _list;
        private System.Windows.Forms.StatusStrip _statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel _status;
    }
}
