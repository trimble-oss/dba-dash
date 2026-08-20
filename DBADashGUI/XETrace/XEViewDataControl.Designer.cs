namespace DBADashGUI.XETrace
{
    partial class XEViewDataControl
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
            _toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            _rangeLabel = new System.Windows.Forms.ToolStripLabel();
            _rangeCombo = new System.Windows.Forms.ToolStripComboBox();
            _maxRowsLabel = new System.Windows.Forms.ToolStripLabel();
            _maxRowsCombo = new System.Windows.Forms.ToolStripComboBox();
            _toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
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
            _toolbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _refreshButton, _toolStripSeparator1, _rangeLabel, _rangeCombo, _maxRowsLabel, _maxRowsCombo, _toolStripSeparator2, _clearButton });
            _toolbar.Location = new System.Drawing.Point(0, 0);
            _toolbar.Name = "_toolbar";
            _toolbar.Size = new System.Drawing.Size(1371, 28);
            _toolbar.TabIndex = 0;
            // 
            // _refreshButton
            // 
            _refreshButton.Image = Properties.Resources.ProjectSystemModelRefresh_16x;
            _refreshButton.Name = "_refreshButton";
            _refreshButton.Size = new System.Drawing.Size(82, 25);
            _refreshButton.Text = "Refresh";
            // 
            // _toolStripSeparator1
            // 
            _toolStripSeparator1.Name = "_toolStripSeparator1";
            _toolStripSeparator1.Size = new System.Drawing.Size(6, 28);
            // 
            // _rangeLabel
            // 
            _rangeLabel.Name = "_rangeLabel";
            _rangeLabel.Size = new System.Drawing.Size(54, 25);
            _rangeLabel.Text = "Range:";
            // 
            // _rangeCombo
            // 
            _rangeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _rangeCombo.Name = "_rangeCombo";
            _rangeCombo.Size = new System.Drawing.Size(140, 28);
            _rangeCombo.ToolTipText = "Only load events within this time range";
            // 
            // _maxRowsLabel
            // 
            _maxRowsLabel.Name = "_maxRowsLabel";
            _maxRowsLabel.Size = new System.Drawing.Size(75, 25);
            _maxRowsLabel.Text = "Max rows:";
            // 
            // _maxRowsCombo
            // 
            _maxRowsCombo.AutoSize = false;
            _maxRowsCombo.Name = "_maxRowsCombo";
            _maxRowsCombo.Size = new System.Drawing.Size(80, 28);
            _maxRowsCombo.ToolTipText = "Maximum number of (newest) rows to load";
            // 
            // _toolStripSeparator2
            // 
            _toolStripSeparator2.Name = "_toolStripSeparator2";
            _toolStripSeparator2.Size = new System.Drawing.Size(6, 28);
            // 
            // _clearButton
            // 
            _clearButton.Image = Properties.Resources.Eraser_16x;
            _clearButton.Name = "_clearButton";
            _clearButton.Size = new System.Drawing.Size(67, 25);
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
            _results.Location = new System.Drawing.Point(0, 28);
            _results.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            _results.Name = "_results";
            _results.Size = new System.Drawing.Size(1371, 1083);
            _results.TabIndex = 2;
            // 
            // XEViewDataControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(_results);
            Controls.Add(_statusStrip);
            Controls.Add(_toolbar);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            Name = "XEViewDataControl";
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
        private System.Windows.Forms.ToolStripButton _refreshButton;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator1;
        private System.Windows.Forms.ToolStripLabel _rangeLabel;
        private System.Windows.Forms.ToolStripComboBox _rangeCombo;
        private System.Windows.Forms.ToolStripLabel _maxRowsLabel;
        private System.Windows.Forms.ToolStripComboBox _maxRowsCombo;
        private System.Windows.Forms.ToolStripSeparator _toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton _clearButton;
        private System.Windows.Forms.StatusStrip _statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel _status;
        private XEResultsControl _results;
    }
}
