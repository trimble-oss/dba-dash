namespace DBADashGUI.XETrace
{
    partial class XEWatchControl
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
            if (disposing)
            {
                components?.Dispose();
                _heartbeatTimer?.Dispose();
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
            _stopButton = new System.Windows.Forms.ToolStripButton();
            _clearButton = new System.Windows.Forms.ToolStripButton();
            _statusStrip = new System.Windows.Forms.StatusStrip();
            _status = new System.Windows.Forms.ToolStripStatusLabel();
            _results = new XEResultsControl();
            _toolbar.SuspendLayout();
            _statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // _toolbar
            // 
            _toolbar.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            _toolbar.ImageScalingSize = new System.Drawing.Size(20, 20);
            _toolbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _stopButton, _clearButton });
            _toolbar.Location = new System.Drawing.Point(0, 0);
            _toolbar.Name = "_toolbar";
            _toolbar.Size = new System.Drawing.Size(1371, 27);
            _toolbar.TabIndex = 0;
            // 
            // _stopButton
            // 
            _stopButton.Enabled = false;
            _stopButton.Image = Properties.Resources.StatusAnnotations_Stop_32xLG_color;
            _stopButton.Name = "_stopButton";
            _stopButton.Size = new System.Drawing.Size(64, 24);
            _stopButton.Text = "Stop";
            // 
            // _clearButton
            // 
            _clearButton.Image = Properties.Resources.Eraser_16x;
            _clearButton.Name = "_clearButton";
            _clearButton.Size = new System.Drawing.Size(67, 24);
            _clearButton.Text = "Clear";
            // 
            // _statusStrip
            // 
            _statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            _statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _status });
            _statusStrip.Location = new System.Drawing.Point(0, 1111);
            _statusStrip.Name = "_statusStrip";
            _statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
            _statusStrip.Size = new System.Drawing.Size(1371, 22);
            _statusStrip.TabIndex = 1;
            // 
            // _status
            // 
            _status.Name = "_status";
            _status.Size = new System.Drawing.Size(0, 16);
            // 
            // _results
            // 
            _results.Dock = System.Windows.Forms.DockStyle.Fill;
            _results.Location = new System.Drawing.Point(0, 27);
            _results.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            _results.Name = "_results";
            _results.Size = new System.Drawing.Size(1371, 1084);
            _results.TabIndex = 2;
            // 
            // XEWatchControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(_results);
            Controls.Add(_toolbar);
            Controls.Add(_statusStrip);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "XEWatchControl";
            Size = new System.Drawing.Size(1371, 1133);
            _toolbar.ResumeLayout(false);
            _toolbar.PerformLayout();
            _statusStrip.ResumeLayout(false);
            _statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip _toolbar;
        private System.Windows.Forms.ToolStripButton _stopButton;
        private System.Windows.Forms.ToolStripButton _clearButton;
        private System.Windows.Forms.StatusStrip _statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel _status;
        private XEResultsControl _results;
    }
}
