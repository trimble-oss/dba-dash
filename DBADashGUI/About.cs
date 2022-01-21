using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Octokit;

namespace DBADashGUI
{
    partial class About : Form
    {
        public About()
        {
            InitializeComponent();
            this.Text = String.Format("About {0}", AssemblyTitle);
            this.labelVersion.Text =  AssemblyVersion;
            this.labelCopyright.Text = AssemblyCopyright;
            this.labelCompanyName.Text = AssemblyCompany;
        }

        /// <summary>Get latest release from github</summary> 
        private async Task<Release> getLatestVersionAsync()
        {
            var client = new GitHubClient(new ProductHeaderValue("dba-dash"));
            return await client.Repository.Release.GetLatest("trimble-oss","dba-dash");         
        }

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                    {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }

        #endregion

        private void lnkDBADash_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Common.OpenURL("http://dbadash.com");
        }


        private async void About_Load(object sender, EventArgs e)
        {
            setDBVersion();
            await setLatestVersionAsync(); // Display the latest version from github       
        }

        /// <summary>Get version information from the database</summary> 
        private void setDBVersion()
        {
            try
            {
                var dbVersion = DBADash.DBValidations.GetDBVersion(Common.ConnectionString);
                lblRepoVersion.Text = dbVersion.ToString();
            }
            catch (Exception ex)
            {
                lblRepoVersion.Text = String.Format("DB Repository Version {0}", ex.Message);
            }
        }

        /// <summary>Update about box with latest version info</summary> 
        private async Task setLatestVersionAsync()
        {
            string latest;
            try
            {
                var release = await getLatestVersionAsync();
                latest = release.TagName;
            }
            catch(Exception ex)
            {
                lnkLatestRelease.Text = "???";
                toolTip1.SetToolTip(lnkLatestRelease, ex.Message);
                return;
            }
            lnkLatestRelease.Text = latest;
            try
            {
                var latestVersion = new Version(latest);
                var currentVersion = new Version(AssemblyVersion);
                if (currentVersion.CompareTo(latestVersion) < 0)
                {
                    lnkLatestRelease.Font = new Font(lnkLatestRelease.Font, FontStyle.Bold);
                    lblLatest.Font = new Font(lblLatest.Font, FontStyle.Bold);
                    lblLatest.Text = "Latest Version (Upgrade Available):";
                }
            }
            catch
            {
                lblLatest.Text = "Latest Version (Unable to Compare):";
            }

        }

        private void lnkLatestRelease_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Common.OpenURL("https://github.com/trimble-oss/dba-dash/releases/latest");
        }

        private void lnkAuthor_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Common.OpenURL("https://github.com/DavidWiseman");
        }

        private void lnkLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("notepad.exe", "LICENSE");
        }
    }
}
