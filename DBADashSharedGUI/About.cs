using DBADash;
using DBADashSharedGUI;
using Octokit;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using DBADashGUI.Theme;

namespace DBADashGUI
{
    public partial class About : Form
    {
        [SupportedOSPlatform("windows")]
        public About()
        {
            InitializeComponent();
            labelVersion.Text = AssemblyVersion;
            labelCopyright.Text = AssemblyCopyright;
            labelCompanyName.Text = AssemblyCompany;
            this.ApplyTheme();
        }

        #region Assembly Attribute Accessors

        public static string AssemblyTitle
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

        public static string AssemblyVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return version == null ? "???" : version.ToString();
            }
        }

        public static string AssemblyDescription
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

        public static string AssemblyProduct
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

        public static string AssemblyCopyright
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

        public static string AssemblyCompany
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

        #endregion Assembly Attribute Accessors

        public Version DBVersion = new();
        public bool upgradeAvailable;
        public string upgradeMessage = "The upgrade check hasn't completed yet.\nIf this server doesn't have internet access, an offline upgrade can be performed. See:\nhttps://dbadash.com/upgrades";
        public bool StartGUIOnUpgrade;
        private MessageBoxIcon upgradeIcon = MessageBoxIcon.Warning;

        private void LnkDBADash_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CommonShared.OpenURL(Upgrade.AppURL);
        }

        private async void About_Load(object sender, EventArgs e)
        {
            lblDeploymentType.Text = Upgrade.DeploymentType.ToString();
            lblRepoVersion.Text = DBVersion.ToString();
            await SetLatestVersionAsync(); // Display the latest version from github
        }

        /// <summary>Update about box with latest version info</summary>
        private async Task SetLatestVersionAsync()
        {
            Release release;
            Version releaseVersion;
            try
            {
                release = await Upgrade.GetLatestVersionAsync();
                releaseVersion = new Version(release.TagName);
            }
            catch (Exception ex)
            {
                lnkLatestRelease.Text = "???";
                toolTip1.SetToolTip(lnkLatestRelease, ex.Message);
                upgradeMessage = "Error checking for the latest version.\nIf this server doesn't have internet access, an offline upgrade can be performed. See:\nhttps://dbadash.com/upgrades";
                return;
            }
            lnkLatestRelease.Text = release.TagName;
            try
            {
                if (Upgrade.IsUpgradeAvailable(release))
                {
                    if (Upgrade.DeploymentType == Upgrade.DeploymentTypes.GUI && (DBVersion.Major != releaseVersion.Major || DBVersion.Minor != releaseVersion.Minor || DBVersion.Build != releaseVersion.Build))
                    {
                        upgradeMessage = "An upgrade is available.  Please upgrade the DBA Dash service first.";
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
                    upgradeMessage = "No upgrades are available at this time";
                    upgradeIcon = MessageBoxIcon.Information;
                }
            }
            catch
            {
                lblLatest.Text = "Latest Version (Unable to Compare):";
                upgradeMessage = "Error comparing versions.  Upgrade is not available at this time. See:\nhttps://dbadash.com/upgrades";
            }
        }

        private void LnkLatestRelease_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CommonShared.OpenURL(Upgrade.LatestVersionLink);
        }

        private void LnkAuthor_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            CommonShared.OpenURL(Upgrade.AuthorURL);
        }

        private void LnkLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("notepad.exe", "LICENSE");
        }

        private async void BttnUpgrade_Click(object sender, EventArgs e)
        {
            if (!upgradeAvailable)
            {
                MessageBox.Show(upgradeMessage, "Upgrade", MessageBoxButtons.OK, upgradeIcon);
                return;
            }
            if (MessageBox.Show("Run script to upgrade to latest version of DBA Dash?", "Upgrade", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            try
            {
                await Upgrade.UpgradeDBADashAsync(startGUI: StartGUIOnUpgrade);
            }
            catch (NotFoundException ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Upgrade script is not available.", "Script not found", TaskDialogIcon.Warning, "Please check the upgrade instructions at https://dbadash.com");
                CommonShared.OpenURL(Upgrade.LatestVersionLink);
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error running upgrade script");
            }
        }
    }
}