using DBADash;
using DBADashGUI.Theme;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DBADashServiceConfig;

public partial class ServiceConfig
{
    private Panel pnlAzure;
    private Panel pnlAnthropic;
    private ComboBox cboAiProvider;
    private NumericUpDown numAiPort;
    private TextBox txtAiServiceUrl;
    private ToolTip _aiToolTip;
    private CheckBox chkAiRequireHttps;
    private PictureBox picAiSecurityWarning;
    private Label lblAiSecurityWarning;
    private TextBox txtAiAzureEndpoint;
    private TextBox txtAiAzureApiKey;
    private TextBox txtAiAzureDeployment;
    private TextBox txtAiAzureApiVersion;
    private TextBox txtAiAnthropicBaseUrl;
    private TextBox txtAiAnthropicApiKey;
    private TextBox txtAiAnthropicModel;
    private TextBox txtAiAnthropicVersion;
    private TextBox txtAiAnthropicMaxTokens;

    private const int AiLabelX = 14;
    private const int AiInputX = 220;
    private const int AiRowH = 34;
    private const int AiCtrlH = 27;
    private const int AiGrpX = 8;
    private const int AiGrpW = 1113;

    /// <summary>
    /// Path to the appsettings.json for DBADashAI, expected alongside this tool.
    /// </summary>
    private static string AiAppSettingsPath =>
        Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    /// <summary>
    /// Path to the local overrides file.  This is where the service config tool writes
    /// all settings so that the default appsettings.json is never modified and can be
    /// safely overwritten by future builds.
    /// </summary>
    private static string AiLocalAppSettingsPath =>
        Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");

    private void InitializeAiServiceControls()
    {
        if (tabAiService != null) return;

        tabAiService = new TabPage
        {
            Name = "tabAiService",
            Text = "AI Service",
            UseVisualStyleBackColor = true,
            Padding = new Padding(3, 4, 3, 4),
            AutoScroll = true
        };

        // ── Section 4: Service Control ─────────────────────────────────────
        const int svcY = 35;
        const int svcH = 21;

        lblAiServiceStatus = new Label
        {
            AutoSize = true,
            Location = new Point(AiLabelX, svcY + 2),
            Name = "lblAiServiceStatus",
            Text = "AI Service Status: ..."
        };

        lnkStartAi = new LinkLabel
        {
            Image = Properties.Resources.StartWithoutDebug_16x,
            ImageAlign = ContentAlignment.MiddleLeft,
            LinkColor = Color.FromArgb(0, 79, 131),
            Location = new Point(270, svcY),
            Name = "lnkStartAi",
            Size = new Size(69, svcH),
            TabStop = true,
            Text = "Start",
            TextAlign = ContentAlignment.TopRight,
            Enabled = IsAdmin
        };
        lnkStartAi.LinkClicked += async (_, _) => await StartAiServiceAsync();
        toolTip1.SetToolTip(lnkStartAi, "Start the DBADashAI service");

        lnkStopAi = new LinkLabel
        {
            Image = Properties.Resources.Stop_16x,
            ImageAlign = ContentAlignment.MiddleLeft,
            LinkColor = Color.FromArgb(0, 79, 131),
            Location = new Point(367, svcY),
            Name = "lnkStopAi",
            Size = new Size(62, svcH),
            TabStop = true,
            Text = "Stop",
            TextAlign = ContentAlignment.TopRight,
            Enabled = IsAdmin
        };
        lnkStopAi.LinkClicked += async (_, _) => await StopAiServiceAsync();
        toolTip1.SetToolTip(lnkStopAi, "Stop the DBADashAI service");

        lnkRefreshAi = new LinkLabel
        {
            Image = Properties.Resources._112_RefreshArrow_Green_16x16_72,
            ImageAlign = ContentAlignment.MiddleLeft,
            LinkColor = Color.FromArgb(0, 79, 131),
            Location = new Point(457, svcY),
            Name = "lnkRefreshAi",
            Size = new Size(80, svcH),
            TabStop = true,
            Text = "Refresh",
            TextAlign = ContentAlignment.TopRight
        };
        lnkRefreshAi.LinkClicked += async (_, _) => await RefreshAiServiceStatusAsync();
        toolTip1.SetToolTip(lnkRefreshAi, "Refresh the DBADashAI service status");

        lnkInstallAi = new LinkLabel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Image = Properties.Resources.install,
            ImageAlign = ContentAlignment.MiddleLeft,
            LinkColor = Color.FromArgb(0, 79, 131),
            Location = new Point(937, svcY),
            Name = "lnkInstallAi",
            Size = new Size(160, svcH),
            TabStop = true,
            Text = "Install AI service",
            TextAlign = ContentAlignment.TopRight,
            Enabled = IsAdmin
        };
        lnkInstallAi.LinkClicked += async (_, _) =>
        {
            if (IsAiServiceInstalled()) await UninstallAiServiceAsync();
            else await InstallAiServiceAsync();
        };
        toolTip1.SetToolTip(lnkInstallAi, "Install DBADashAI API as a Windows service");

        var grpService = new GroupBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Location = new Point(AiGrpX, 592),
            Size = new Size(AiGrpW, 70),
            TabStop = false,
            Text = IsAdmin
                ? "DBADashAI Windows Service"
                : "DBADashAI Windows Service (Run as Administrator to manage)"
        };
        grpService.Controls.AddRange(new Control[]
        {
            lblAiServiceStatus, lnkStartAi, lnkStopAi, lnkRefreshAi, lnkInstallAi
        });

        // ── Section 3: Registration (optional) ────────────────────────────────────────
        _aiToolTip = new ToolTip { AutoPopDelay = 10000, InitialDelay = 400, ReshowDelay = 200 };

        var grpRegistration = new GroupBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(AiGrpX, 382),
            Size = new Size(AiGrpW, 205),
            TabStop = false,
            Text = "Registration (Optional)"
        };

        var pnlReg = new Panel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(5, 30),
            Size = new Size(AiGrpW - 30, 4 * AiRowH + 20)
        };

        pnlReg.Controls.Add(new Label { AutoSize = true, Location = new Point(AiLabelX, 4 + 4), Text = "Port:" });
        numAiPort = new NumericUpDown
        {
            Location = new Point(AiInputX, 4),
            Size = new Size(100, AiCtrlH),
            Minimum = 1,
            Maximum = 65535,
            Value = 5055,
            DecimalPlaces = 0
        };
        _aiToolTip.SetToolTip(numAiPort, "TCP port the AI service listens on. Default: 5055.");
        pnlReg.Controls.Add(numAiPort);

        var lblServiceUrl = new Label { AutoSize = true, Location = new Point(AiLabelX, AiRowH + 4 + 4), Text = "Service URL:" };
        pnlReg.Controls.Add(lblServiceUrl);
        txtAiServiceUrl = new TextBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Location = new Point(AiInputX, AiRowH + 4),
            Size = new Size(pnlReg.Width - AiInputX - 60, AiCtrlH)
        };
        const string serviceUrlTooltip =
            "Optional. The external URL where this AI service is reachable by GUI clients on other machines\n" +
            "(e.g., http://myserver:5055).\n\n" +
            "When set, the service binds to all network interfaces and registers itself in the\n" +
            "repository so that remote GUI clients can discover and connect to it.\n\n" +
            "Leave blank to run in local-only mode — the service binds to loopback only and\n" +
            "authentication is disabled (safe because no remote access is possible).";
        txtAiServiceUrl.TextChanged += (_, _) => ValidateAiServiceUrl();
        _aiToolTip.SetToolTip(txtAiServiceUrl, serviceUrlTooltip);
        _aiToolTip.SetToolTip(lblServiceUrl, serviceUrlTooltip);
        pnlReg.Controls.Add(txtAiServiceUrl);

        chkAiRequireHttps = new CheckBox
        {
            AutoSize = true,
            Location = new Point(AiInputX, 2 * AiRowH + 6),
            Text = "Require HTTPS"
        };
        const string requireHttpsTooltip =
            "Redirect HTTP requests to HTTPS for the AI service. Enable this only when the service is configured to listen on HTTPS with a valid certificate.";
        chkAiRequireHttps.CheckedChanged += (_, _) => UpdateAiServiceUrlState();
        _aiToolTip.SetToolTip(chkAiRequireHttps, requireHttpsTooltip);
        pnlReg.Controls.Add(chkAiRequireHttps);

        picAiSecurityWarning = new PictureBox
        {
            Image = Properties.Resources.Warning_yellow_7231_16x16,
            Location = new Point(AiInputX, 3 * AiRowH + 10),
            SizeMode = PictureBoxSizeMode.AutoSize,
            Visible = false
        };
        pnlReg.Controls.Add(picAiSecurityWarning);

        lblAiSecurityWarning = new Label
        {
            AutoSize = false,
            Location = new Point(AiInputX + 24, 3 * AiRowH + 6),
            Size = new Size(pnlReg.Width - AiInputX - 84, 44),
            ForeColor = Color.DarkGoldenrod,
            Visible = false
        };
        pnlReg.Controls.Add(lblAiSecurityWarning);

        grpRegistration.Controls.Add(pnlReg);
        tabAiService.Controls.Add(grpRegistration);

        // ── Section 1: Provider selection ──────────────────────────────────────────
        var grpSettings = new GroupBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(AiGrpX, 12),
            Size = new Size(AiGrpW, 110),
            TabStop = false,
            Text = "AI Settings"
        };
        grpSettings.Controls.Add(new Label { AutoSize = true, Location = new Point(AiLabelX, 50), Text = "AI Provider:" });

        cboAiProvider = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(AiInputX, 46),
            Size = new Size(260, AiCtrlH)
        };
        cboAiProvider.Items.AddRange(new object[] { "AzureOpenAI", "Anthropic" });
        cboAiProvider.SelectedIndexChanged += (_, _) => UpdateProviderFieldVisibility();
        grpSettings.Controls.Add(cboAiProvider);

        // ── Section 2: Provider configuration ──────────────────────────────
        var grpConfig = new GroupBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(AiGrpX, 122),
            Size = new Size(AiGrpW, 50 + 5 * AiRowH + 30),
            TabStop = false,
            Text = "Provider Configuration"
        };

        var panelWidth = grpConfig.Width - 30;

        pnlAzure = new Panel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(5, 40),
            Size = new Size(panelWidth, 4 * AiRowH + 10),
            Visible = false
        };
        txtAiAzureEndpoint   = AddPanelRow(pnlAzure, "Endpoint:",   0);
        txtAiAzureApiKey     = AddPanelRow(pnlAzure, "ApiKey:",     1, true);
        txtAiAzureDeployment = AddPanelRow(pnlAzure, "Deployment:", 2);
        txtAiAzureApiVersion = AddPanelRow(pnlAzure, "ApiVersion:", 3);

        pnlAnthropic = new Panel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(5, 40),
            Size = new Size(panelWidth, 5 * AiRowH + 10),
            Visible = false
        };
        txtAiAnthropicBaseUrl   = AddPanelRow(pnlAnthropic, "BaseUrl:",   0);
        txtAiAnthropicApiKey    = AddPanelRow(pnlAnthropic, "ApiKey:",    1, true);
        txtAiAnthropicModel     = AddPanelRow(pnlAnthropic, "Model:",     2);
        txtAiAnthropicVersion   = AddPanelRow(pnlAnthropic, "Version:",   3);
        txtAiAnthropicMaxTokens = AddPanelRow(pnlAnthropic, "MaxTokens:", 4);

        grpConfig.Controls.Add(pnlAzure);
        grpConfig.Controls.Add(pnlAnthropic);

        tabAiService.Controls.Add(grpService);
        tabAiService.Controls.Add(grpSettings);
        tabAiService.Controls.Add(grpConfig);

        var idx = tab1.TabPages.IndexOf(tabJson);
        tab1.TabPages.Insert(idx >= 0 ? idx : tab1.TabPages.Count, tabAiService);

        LoadAiSettings();
        UpdateProviderFieldVisibility();
        tabAiService.ApplyTheme();
    }

    private static TextBox AddPanelRow(Panel panel, string labelText, int row, bool secret = false)
    {
        int y = row * AiRowH + 4;
        panel.Controls.Add(new Label { AutoSize = true, Location = new Point(AiLabelX, y + 4), Text = labelText });
        var txt = new TextBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Location = new Point(AiInputX, y),
            Size = new Size(panel.Width - AiInputX - 60, AiCtrlH),
            UseSystemPasswordChar = secret
        };
        panel.Controls.Add(txt);
        return txt;
    }

    private void UpdateProviderFieldVisibility()
    {
        var p = cboAiProvider.SelectedItem?.ToString() ?? string.Empty;
        pnlAzure.Visible     = p.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase);
        pnlAnthropic.Visible = p.Equals("Anthropic",   StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Reads AI provider settings by merging appsettings.json (base defaults) with
    /// appsettings.local.json (user overrides).  Local values take priority, matching
    /// the runtime configuration chain used by the AI service.
    /// </summary>
    private void LoadAiSettings()
    {
        var j = ReadAiAppSettings();

        var port = j["Registration"]?["Port"]?.Value<int?>() ?? 5055;
        numAiPort.Value = Math.Clamp(port, 1, 65535);
        txtAiServiceUrl.Text = j["Registration"]?["ServiceUrl"]?.Value<string>() ?? string.Empty;
        chkAiRequireHttps.Checked = j["Security"]?["RequireHttps"]?.Value<bool?>() ?? false;

        var provider = j["AI"]?["Provider"]?.Value<string>() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(provider) && cboAiProvider.Items.Contains(provider))
            cboAiProvider.SelectedItem = provider;
        else
            cboAiProvider.SelectedIndex = 0;

        txtAiAzureEndpoint.Text   = j["AzureOpenAI"]?["Endpoint"]?.Value<string>()   ?? string.Empty;
        txtAiAzureApiKey.Text     = GetPlainText(j["AzureOpenAI"]?["ApiKey"]?.Value<string>());
        txtAiAzureDeployment.Text = j["AzureOpenAI"]?["Deployment"]?.Value<string>() ?? string.Empty;
        txtAiAzureApiVersion.Text = j["AzureOpenAI"]?["ApiVersion"]?.Value<string>() ?? string.Empty;

        txtAiAnthropicBaseUrl.Text   = j["Anthropic"]?["BaseUrl"]?.Value<string>()   ?? string.Empty;
        txtAiAnthropicApiKey.Text    = GetPlainText(j["Anthropic"]?["ApiKey"]?.Value<string>());
        txtAiAnthropicModel.Text     = j["Anthropic"]?["Model"]?.Value<string>()     ?? string.Empty;
        txtAiAnthropicVersion.Text   = j["Anthropic"]?["Version"]?.Value<string>()   ?? string.Empty;
        txtAiAnthropicMaxTokens.Text = j["Anthropic"]?["MaxTokens"]?.Value<string>() ?? string.Empty;
        UpdateAiServiceUrlState();
    }

    /// <summary>
    /// Returns the plain-text representation of a configuration value.
    /// Decrypts DPAPI-protected values (dpapi: prefix) for display in the UI.
    /// </summary>
private static string GetPlainText(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        try { return value.FromMachineProtectedConfigValue(); }
        catch { return value; }
    }

    /// <summary>
    /// Returns the value to write to appsettings.json.
    /// On Windows, secrets are protected with DPAPI LocalMachine scope so the key is never stored
    /// in plaintext on disk.  On other platforms the value is written as-is (use environment
    /// variables or a dedicated secret store for secret protection on non-Windows hosts).
    /// </summary>
    private static string GetStoredValue(string value, bool isSecret)
    {
        if (!isSecret || string.IsNullOrEmpty(value))
            return value;
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? value.ToMachineProtectedConfigValue()
            : value;
    }

    /// <summary>
    /// Writes AI provider settings to appsettings.local.json, preserving all other keys.
    /// The base appsettings.json is never modified — it remains a clean default template
    /// that can be safely overwritten by future builds.
    /// Secret fields (API keys) are protected with DPAPI LocalMachine scope on Windows.
    /// </summary>
    private void ApplyAiSettingsToConfig()
    {
        if (cboAiProvider == null) return;

        try
        {
            // Start from the local file if it exists, otherwise an empty object.
            // We do NOT seed from appsettings.json — only values the user has
            // explicitly configured belong in the local override file.
            var j = ReadAiLocalAppSettings();

            j["Registration"] ??= new JObject();
            j["Registration"]!["Port"] = (int)numAiPort.Value;
            j["Registration"]!["ServiceUrl"] = txtAiServiceUrl.Text;

            j["Security"] ??= new JObject();
            j["Security"]!["RequireHttps"] = chkAiRequireHttps.Checked;

            j["AI"] ??= new JObject();
            j["AI"]!["Provider"] = cboAiProvider.SelectedItem?.ToString() ?? string.Empty;

            j["AzureOpenAI"] ??= new JObject();
            j["AzureOpenAI"]!["Endpoint"]   = txtAiAzureEndpoint.Text;
            j["AzureOpenAI"]!["ApiKey"]      = GetStoredValue(txtAiAzureApiKey.Text, isSecret: true);
            j["AzureOpenAI"]!["Deployment"]  = txtAiAzureDeployment.Text;
            j["AzureOpenAI"]!["ApiVersion"]  = txtAiAzureApiVersion.Text;

            j["Anthropic"] ??= new JObject();
            j["Anthropic"]!["BaseUrl"]   = txtAiAnthropicBaseUrl.Text;
            j["Anthropic"]!["ApiKey"]    = GetStoredValue(txtAiAnthropicApiKey.Text, isSecret: true);
            j["Anthropic"]!["Model"]     = txtAiAnthropicModel.Text;
            j["Anthropic"]!["Version"]   = txtAiAnthropicVersion.Text;
            j["Anthropic"]!["MaxTokens"] = txtAiAnthropicMaxTokens.Text;

            File.WriteAllText(AiLocalAppSettingsPath, j.ToString(Newtonsoft.Json.Formatting.Indented));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save AI settings to appsettings.local.json:\n{ex.Message}",
                "AI Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private bool IsAiServiceUrlValid()
    {
        if (txtAiServiceUrl == null)
            return true;

        var serviceUrl = txtAiServiceUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(serviceUrl))
            return true;

        return Uri.TryCreate(serviceUrl, UriKind.Absolute, out var uri)
               && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                   || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
    }

    private bool ValidateAiServiceUrl()
    {
        if (txtAiServiceUrl == null)
            return true;

        var isValid = IsAiServiceUrlValid();
        errorProvider1.SetError(txtAiServiceUrl,
            isValid
                ? null
                : "Service URL must be empty or start with http:// or https://");

        UpdateAiSecurityWarning();
        return isValid;
    }

    private void UpdateAiServiceUrlState()
    {
        ValidateAiServiceUrl();
    }

    private string GetAiSecurityWarningMessage()
    {
        if (txtAiServiceUrl == null || chkAiRequireHttps == null)
            return null;

        var serviceUrl = txtAiServiceUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(serviceUrl))
            return null;

        if (!IsAiServiceUrlValid() || !Uri.TryCreate(serviceUrl, UriKind.Absolute, out var uri))
            return null;

        if (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return chkAiRequireHttps.Checked
                ? "The registration URL uses HTTP, so traffic between GUI clients and the AI service may be transmitted unencrypted. Require HTTPS is enabled, but clients using this registration URL may still connect over plain HTTP unless the URL is changed to HTTPS."
                : "The registration URL uses HTTP, so traffic between GUI clients and the AI service may be transmitted unencrypted. Require HTTPS is not enabled, so the service will not redirect clients to HTTPS.";
        }

        if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) && !chkAiRequireHttps.Checked)
        {
            return "The registration URL uses HTTPS, but Require HTTPS is not enabled. Clients may expect HTTPS while the service is not configured to enforce it.";
        }

        return null;
    }

    private void UpdateAiSecurityWarning()
    {
        if (lblAiSecurityWarning == null || picAiSecurityWarning == null)
            return;

        var warning = GetAiSecurityWarningMessage();
        var visible = !string.IsNullOrWhiteSpace(warning);

        lblAiSecurityWarning.Text = warning ?? string.Empty;
        lblAiSecurityWarning.Visible = visible;
        picAiSecurityWarning.Visible = visible;

        _aiToolTip?.SetToolTip(lblAiSecurityWarning, warning ?? string.Empty);
        _aiToolTip?.SetToolTip(picAiSecurityWarning, warning ?? string.Empty);
    }

    /// <summary>
    /// Reads and deep-merges appsettings.json (base) with appsettings.local.json (overrides).
    /// Local values win, matching the runtime priority chain in the AI service.
    /// </summary>
    private static JObject ReadAiAppSettings()
    {
        var base_ = ReadJsonFile(AiAppSettingsPath);
        var local = ReadJsonFile(AiLocalAppSettingsPath);
        base_.Merge(local, new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Replace,
            MergeNullValueHandling = MergeNullValueHandling.Merge
        });
        return base_;
    }

    /// <summary>
    /// Reads appsettings.local.json only, returning an empty object if absent.
    /// </summary>
    private static JObject ReadAiLocalAppSettings() => ReadJsonFile(AiLocalAppSettingsPath);

    private static JObject ReadJsonFile(string path)
    {
        if (!File.Exists(path)) return new JObject();
        try { return JObject.Parse(File.ReadAllText(path)); }
        catch { return new JObject(); }
    }
}
