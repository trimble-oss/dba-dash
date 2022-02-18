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
using DBADash;
using DBADashSharedGUI;
using System.Runtime.Versioning;

namespace DBADashGUI
{
    public partial class About : Form
    {
        [SupportedOSPlatform("windows")]
        public About()
        {
            InitializeComponent();
            this.labelVersion.Text =  AssemblyVersion;
            this.labelCopyright.Text = AssemblyCopyright;
            this.labelCompanyName.Text = AssemblyCompany;
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
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version==null ? "???" : version.ToString();
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

        public Version DBVersion=new Version();
        public bool upgradeAvailable=false;
        public string upgradeMessage=String.Empty;

        private void lnkDBADash_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CommonShared.OpenURL(Upgrade.AppURL);
        }


        private async void About_Load(object sender, EventArgs e)
        {
            lblDeploymentType.Text = Upgrade.DeploymentType.ToString();
            lblRepoVersion.Text = DBVersion.ToString();
            await setLatestVersionAsync(); // Display the latest version from github       
        }


        /// <summary>Update about box with latest version info</summary> 
        private async Task setLatestVersionAsync()
        {
            Release release;
            Version releaseVersion;
            try
            {
                release = await Upgrade.GetLatestVersionAsync();
                releaseVersion = new Version(release.TagName);
            }
            catch(Exception ex)
            {
                lnkLatestRelease.Text = "???";
                toolTip1.SetToolTip(lnkLatestRelease, ex.Message);
                return;
            }
            lnkLatestRelease.Text = release.TagName;
            try
            {
                if (Upgrade.IsUpgradeAvailable(release))
                {
                    if (Upgrade.DeploymentType == Upgrade.DeploymentTypes.GUI && (DBVersion.Major != releaseVersion.Major || DBVersion.Minor != releaseVersion.Minor || DBVersion.Build != releaseVersion.Build))
                    {
                        upgradeMessage = "An upgrade is available.  Please upgrade the DBA Dash agent first.";
                    }
                    else
                    {
                        upgradeAvailable = true;
                        lnkLatestRelease.Font = new Font(lnkLatestRelease.Font, FontStyle.Bold);
                        lblLatest.Font = new Font(lblLatest.Font, FontStyle.Bold);
                        lblLatest.Text = "Latest Version (Upgrade Available):";
                        bttnUpgrade.Enabled = true;
                    }
                }
                else
                {
                    upgradeMessage= "No upgrades are available at this time";
                }
            }
            catch
            {
                lblLatest.Text = "Latest Version (Unable to Compare):";
                upgradeMessage = "Error comparing versions.  Upgrade is not available at this time.";
            }

        }

        private void lnkLatestRelease_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CommonShared.OpenURL(Upgrade.LatestVersionLink);
        }

        private void lnkAuthor_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CommonShared.OpenURL(Upgrade.AuthorURL);
        }

        private void lnkLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("notepad.exe", "LICENSE");
        }

        private async void bttnUpgrade_Click(object sender, EventArgs e)
        {
            if (!upgradeAvailable)
            {
                MessageBox.Show(upgradeMessage,"Warning", MessageBoxButtons.OK,MessageBoxIcon.Warning);
                return;
            }
            if(MessageBox.Show("Run script to upgrade to latest version of DBA Dash?","Upgrade",MessageBoxButtons.YesNo,MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            try
            {
                await Upgrade.UpgradeDBADashAsync(startGUI:true);
            }
            catch(Octokit.NotFoundException)
            {
                MessageBox.Show("Upgrade script is not available.  Please check the upgrade instructions on the GitHub page", "Not Available", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                CommonShared.OpenURL(Upgrade.LatestVersionLink);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error running upgrade" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

    }
}
