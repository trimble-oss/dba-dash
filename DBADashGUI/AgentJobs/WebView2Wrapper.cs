using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    public partial class WebView2Wrapper : UserControl
    {
        public WebView2Wrapper()
        {
            InitializeComponent();
            lnkDownload.LinkColor = DashColors.LinkColor;
            lblNotice.ForeColor = DashColors.Fail;
        }

        public delegate void WebView2SetupCompleted();

        public event WebView2SetupCompleted SetupCompleted;

        public Microsoft.Web.WebView2.WinForms.WebView2 WebView2 => WebViewCtrl;

        /// <summary>
        /// Run EnsureCoreWebView2Async.  If WebView2 runtime isn't installed a download link will be displayed.  Error will be re-thrown
        /// </summary>
        public async Task EnsureCoreWebView2WrapperAsync()
        {
            try
            {
                await WebViewCtrl.EnsureCoreWebView2Async(null);
            }
            catch (Microsoft.Web.WebView2.Core.WebView2RuntimeNotFoundException)
            {
                pnlWebView2Required.Visible = true;
                WebViewCtrl.Visible = false;
                throw;
            }
            pnlWebView2Required.Visible = false;
            WebViewCtrl.Visible = true;
        }

        private static string WebView2SetupTempPath => Path.Combine(System.IO.Path.GetTempPath(), Common.TempFilePrefix + "MicrosoftEdgeWebview2Setup.exe");

        /// <summary>
        /// Download & Install WebView2 runtime.  Fire Setup_Completed event when finished.
        /// </summary>
        private void InstallWebView2(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string url = txtLink.Text;
            string path = WebView2SetupTempPath;
            try
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
                Common.DownloadFile(path, url);
                var pSetup = System.Diagnostics.Process.Start(path);
                pSetup.EnableRaisingEvents = true;
                pSetup.Exited += Setup_Completed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error downloading/running webview2 setup:\n" + ex.Message + "\nPlease setup manually from:\n" + url, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Setup_Completed(object sender, EventArgs e)
        {
            SetupCompleted?.Invoke();
        }

        public async Task NavigateToLargeString(string html)
        {
            try
            {
                await EnsureCoreWebView2WrapperAsync();
            }
            catch (Exception)
            {
                return;
            }
            try
            {
                WebViewCtrl.NavigateToString(html);
            }
            catch (Exception ex)
            {
                LoadHTMLFromDisk(ex, html); // NavigateToString might fail if size exceeds 2MB.  Try loading from disk instead.
            }
        }

        // <summary>
        // Load HTML from disk instead of using NavigateToString.  Required if HTML is over 2MB.
        //</summary>
        private void LoadHTMLFromDisk(Exception ex, string html)
        {
            try
            {
                string tempFilePath = Common.GetTempFilePath(".html"); // Generate a unique file.  Setting source to same file doesn't refresh
                System.IO.File.WriteAllText(tempFilePath, html);
                WebViewCtrl.Source = new Uri(tempFilePath);
            }
            catch (Exception ex2)
            {
                WebViewCtrl.NavigateToString(String.Format("<html><body style='background-color:{0};color:#ffffff'>Error loading HTML:<br/>{1}</body></html>", DashColors.Fail.ToHexString(), HttpUtility.HtmlEncode(ex.ToString()) + "<br/>" + HttpUtility.HtmlEncode(ex2.ToString())));
            }
        }

        public async void CopyImageToClipboard()
        {
            Task<string> t = WebViewCtrl.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", "{}");
            string json = await t;
            string base64Image = JObject.Parse(json).Value<string>("data");
            Image img = Common.Base64StringAsImage(base64Image);
            Clipboard.SetImage(img);
        }
    }
}