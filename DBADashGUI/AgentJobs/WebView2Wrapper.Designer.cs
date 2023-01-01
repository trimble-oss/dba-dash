namespace DBADashGUI.AgentJobs
{
    partial class WebView2Wrapper
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
            this.lnkDownload = new System.Windows.Forms.LinkLabel();
            this.txtLink = new System.Windows.Forms.TextBox();
            this.pnlWebView2Required = new System.Windows.Forms.Panel();
            this.lblNotice = new System.Windows.Forms.Label();
            this.WebViewCtrl = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.pnlWebView2Required.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.WebViewCtrl)).BeginInit();
            this.SuspendLayout();
            // 
            // lnkDownload
            // 
            this.lnkDownload.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lnkDownload.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lnkDownload.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lnkDownload.Location = new System.Drawing.Point(0, 0);
            this.lnkDownload.Name = "lnkDownload";
            this.lnkDownload.Size = new System.Drawing.Size(640, 442);
            this.lnkDownload.TabIndex = 2;
            this.lnkDownload.TabStop = true;
            this.lnkDownload.Text = "Install WebView2 Runtime";
            this.lnkDownload.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lnkDownload.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.InstallWebView2);
            // 
            // txtLink
            // 
            this.txtLink.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.txtLink.Location = new System.Drawing.Point(0, 442);
            this.txtLink.Name = "txtLink";
            this.txtLink.Size = new System.Drawing.Size(640, 27);
            this.txtLink.TabIndex = 3;
            this.txtLink.Text = "https://go.microsoft.com/fwlink/p/?LinkId=2124703";
            // 
            // pnlWebView2Required
            // 
            this.pnlWebView2Required.Controls.Add(this.lblNotice);
            this.pnlWebView2Required.Controls.Add(this.lnkDownload);
            this.pnlWebView2Required.Controls.Add(this.txtLink);
            this.pnlWebView2Required.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlWebView2Required.Location = new System.Drawing.Point(0, 0);
            this.pnlWebView2Required.Name = "pnlWebView2Required";
            this.pnlWebView2Required.Size = new System.Drawing.Size(640, 469);
            this.pnlWebView2Required.TabIndex = 4;
            this.pnlWebView2Required.Visible = false;
            // 
            // lblNotice
            // 
            this.lblNotice.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblNotice.Location = new System.Drawing.Point(0, 392);
            this.lblNotice.Name = "lblNotice";
            this.lblNotice.Size = new System.Drawing.Size(640, 50);
            this.lblNotice.TabIndex = 4;
            this.lblNotice.Text = "WebView2 Runtime is required to display this control.  Click the link to download" +
    " and install or copy the link below to download manually.";
            this.lblNotice.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // WebViewCtrl
            // 
            this.WebViewCtrl.AllowExternalDrop = true;
            this.WebViewCtrl.CreationProperties = null;
            this.WebViewCtrl.DefaultBackgroundColor = System.Drawing.Color.White;
            this.WebViewCtrl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.WebViewCtrl.Location = new System.Drawing.Point(0, 0);
            this.WebViewCtrl.Name = "WebViewCtrl";
            this.WebViewCtrl.Size = new System.Drawing.Size(640, 469);
            this.WebViewCtrl.TabIndex = 5;
            this.WebViewCtrl.ZoomFactor = 1D;
            // 
            // WebView2Wrapper
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlWebView2Required);
            this.Controls.Add(this.WebViewCtrl);
            this.Name = "WebView2Wrapper";
            this.Size = new System.Drawing.Size(640, 469);
            this.pnlWebView2Required.ResumeLayout(false);
            this.pnlWebView2Required.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.WebViewCtrl)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.LinkLabel lnkDownload;
        private System.Windows.Forms.TextBox txtLink;
        private System.Windows.Forms.Panel pnlWebView2Required;
        private Microsoft.Web.WebView2.WinForms.WebView2 WebViewCtrl;
        private System.Windows.Forms.Label lblNotice;
    }
}
