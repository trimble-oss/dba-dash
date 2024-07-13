using Newtonsoft.Json.Linq;
using System;
using System.IO;
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

        public Microsoft.Web.WebView2.WinForms.WebView2 WebView2 { get; private set; }

        /// <summary>
        /// Run EnsureCoreWebView2Async.  If WebView2 runtime isn't installed a download link will be displayed.  Error will be re-thrown
        /// </summary>
        public async Task EnsureCoreWebView2WrapperAsync()
        {
            try
            {
                await WebView2.EnsureCoreWebView2Async(null);
            }
            catch (Microsoft.Web.WebView2.Core.WebView2RuntimeNotFoundException)
            {
                pnlWebView2Required.Visible = true;
                WebView2.Visible = false;
                throw;
            }
            pnlWebView2Required.Visible = false;
            WebView2.Visible = true;
        }

        private static string WebView2SetupTempPath => Path.Combine(Path.GetTempPath(), Common.TempFilePrefix + "MicrosoftEdgeWebview2Setup.exe");

        /// <summary>
        /// Download & Install WebView2 runtime.  Fire Setup_Completed event when finished.
        /// </summary>
        private void InstallWebView2(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var url = txtLink.Text;
            var path = WebView2SetupTempPath;
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                Common.DownloadFile(path, url);
                var pSetup = System.Diagnostics.Process.Start(path);
                if (pSetup == null) return;
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
                WebView2.NavigateToString(html);
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
                var tempFilePath = Common.GetTempFilePath(".html"); // Generate a unique file.  Setting source to same file doesn't refresh
                File.WriteAllText(tempFilePath, html);
                WebView2.Source = new Uri(tempFilePath);
            }
            catch (Exception ex2)
            {
                WebView2.NavigateToString(
                    $"<html><body style='background-color:{DashColors.Fail.ToHexString()};color:#ffffff'>Error loading HTML:<br/>{HttpUtility.HtmlEncode(ex.ToString()) + "<br/>" + HttpUtility.HtmlEncode(ex2.ToString())}</body></html>");
            }
        }

        public async void CopyImageToClipboard()
        {
            var t = WebView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", "{}");
            var json = await t;
            var base64Image = JObject.Parse(json).Value<string>("data");
            var img = Common.Base64StringAsImage(base64Image);
            Clipboard.SetImage(img);
        }
    }
}