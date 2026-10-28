using DBADash;
using DBADashGUI.Theme;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

namespace DBADashServiceConfig;

public partial class ServiceConfig
{
    private Panel pnlAzure;
    private Panel pnlAnthropic;
    private Panel pnlBedrock;
    private Panel pnlOllama;
    private ComboBox cboAiProvider;
    private NumericUpDown numAiPort;
    private TextBox txtAiServiceUrl;
    private ToolTip _aiToolTip;
    private CheckBox chkAiUseHttps;
    private ComboBox cboAiCertificate;
    private string _aiCertificateStoreError;
    // Kestrel endpoints or a listen URL are configured, so the service ignores Use HTTPS and keeps the URL scheme.
    private bool _aiCustomHosting;
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
    private TextBox txtAiBedrockRegion;
    private TextBox txtAiBedrockApiKey;
    private TextBox txtAiBedrockProfile;
    private TextBox txtAiBedrockModel;
    private TextBox txtAiBedrockMaxTokens;
    private TextBox txtAiOllamaBaseUrl;
    private TextBox txtAiOllamaApiKey;
    private TextBox txtAiOllamaModel;
    private TextBox txtAiOllamaContextLength;
    private TextBox txtAiOllamaMaxTokens;
    private TextBox txtAiOllamaTimeoutSeconds;

    private const int AiLabelX = 14;
    private const int AiInputX = 220;
    private const int AiRowH = 34;
    private const int AiCtrlH = 27;
    private const int AiGrpX = 8;
    private const int AiGrpW = 1113;
    // AutoScaleDimensions from ServiceConfig.Designer.cs. The form overwrites AutoScaleDimensions once it has scaled.
    private static readonly SizeF AiDesignAutoScaleDimensions = new(8F, 20F);

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

        // Sections stack top to bottom: provider, registration, service control.
        const int providerY = 12;
        const int providerH = 30 + 7 * AiRowH + 16;      // provider row, then up to six provider setting rows
        const int registrationY = providerY + providerH + 10;
        const int registrationPanelH = 3 * AiRowH + 58;  // three rows, then a two-line warning
        const int registrationH = 30 + registrationPanelH + 6;
        const int serviceY = registrationY + registrationH + 10;

        // ── Section 3: Service Control ─────────────────────────────────────
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
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(AiGrpX, serviceY),
            Size = new Size(AiGrpW, 70),
            TabIndex = 2,
            TabStop = false,
            Text = IsAdmin
                ? "DBADashAI Windows Service"
                : "DBADashAI Windows Service (Run as Administrator to manage)"
        };
        grpService.Controls.AddRange(new Control[]
        {
            lblAiServiceStatus, lnkStartAi, lnkStopAi, lnkRefreshAi, lnkInstallAi
        });

        // ── Section 2: Registration (optional) ────────────────────────────────────────
        _aiToolTip = new ToolTip { AutoPopDelay = 10000, InitialDelay = 400, ReshowDelay = 200 };

        var grpRegistration = new GroupBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(AiGrpX, registrationY),
            Size = new Size(AiGrpW, registrationH),
            TabIndex = 1,
            TabStop = false,
            Text = "Registration (Optional)"
        };

        var pnlReg = new Panel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(5, 30),
            Size = new Size(AiGrpW - 30, registrationPanelH)
        };

        // Row 0: Port and Use HTTPS (both describe the listener). Row 1: Service URL. Row 2: Certificate. Then the warning.
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

        chkAiUseHttps = new CheckBox
        {
            AutoSize = true,
            Location = new Point(AiInputX + 130, 4 + 2),
            Text = "Use HTTPS"
        };
        const string useHttpsTooltip =
            "Shared mode listens on HTTPS. When no certificate is selected, the service creates a\n" +
            "self-signed certificate and registers its thumbprint in the repository. GUI clients trust that certificate.\n" +
            "Turn this off when a reverse proxy terminates TLS (keep the proxy's https:// URL), or when the network\n" +
            "cannot use HTTPS. Without a proxy, the API key is then sent without encryption.";
        chkAiUseHttps.CheckedChanged += (_, _) =>
        {
            SyncAiServiceUrlScheme();
            UpdateAiCertificateState();
            UpdateAiServiceUrlState();
        };
        _aiToolTip.SetToolTip(chkAiUseHttps, useHttpsTooltip);
        pnlReg.Controls.Add(chkAiUseHttps);

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
            "(e.g., https://myserver:5055).\n\n" +
            "When set, the service binds to all network interfaces and registers itself in the\n" +
            "repository so that remote GUI clients can discover and connect to it.\n" +
            "HTTPS is used unless Use HTTPS is turned off.\n\n" +
            "Leave blank to run in local-only mode — the service binds to loopback only and\n" +
            "authentication is disabled (safe because no remote access is possible).";
        txtAiServiceUrl.TextChanged += (_, _) => ValidateAiServiceUrl();
        _aiToolTip.SetToolTip(txtAiServiceUrl, serviceUrlTooltip);
        _aiToolTip.SetToolTip(lblServiceUrl, serviceUrlTooltip);
        pnlReg.Controls.Add(txtAiServiceUrl);

        pnlReg.Controls.Add(new Label { AutoSize = true, Location = new Point(AiLabelX, 2 * AiRowH + 8), Text = "Certificate:" });
        cboAiCertificate = new ComboBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(AiInputX, 2 * AiRowH + 4),
            Size = new Size(pnlReg.Width - AiInputX - 60, AiCtrlH)
        };
        cboAiCertificate.SelectedIndexChanged += (_, _) => UpdateAiSecurityWarning();
        _aiToolTip.SetToolTip(cboAiCertificate,
            "Automatic creates a self-signed certificate on the AI server.\n" +
            "Choose a certificate from the Local Computer Personal store to use a certificate clients already trust.\n" +
            "The certificate name should match the host in the service URL, and the AI service account needs permission to use its private key.");
        pnlReg.Controls.Add(cboAiCertificate);

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
            Size = new Size(pnlReg.Width - AiInputX - 84, 48),
            ForeColor = Color.DarkGoldenrod,
            Visible = false
        };
        pnlReg.Controls.Add(lblAiSecurityWarning);

        grpRegistration.Controls.Add(pnlReg);
        tabAiService.Controls.Add(grpRegistration);

        // ── Section 1: Provider selection and configuration ──────────────────────────
        // The provider's settings panel sits under the provider row, in the same column layout.
        const int providerPanelY = 30 + AiRowH;
        var grpConfig = new GroupBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(AiGrpX, providerY),
            Size = new Size(AiGrpW, providerH),
            TabIndex = 0,
            TabStop = false,
            Text = "AI Provider"
        };
        grpConfig.Controls.Add(new Label { AutoSize = true, Location = new Point(5 + AiLabelX, 30 + 4 + 4), Text = "AI Provider:" });

        cboAiProvider = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(5 + AiInputX, 30 + 4),
            Size = new Size(260, AiCtrlH)
        };
        cboAiProvider.Items.AddRange(new object[] { "AzureOpenAI", "Anthropic", "Bedrock", "Ollama" });
        cboAiProvider.SelectedIndexChanged += (_, _) => UpdateProviderFieldVisibility();
        grpConfig.Controls.Add(cboAiProvider);

        var panelWidth = grpConfig.Width - 30;

        pnlAzure = new Panel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(5, providerPanelY),
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
            Location = new Point(5, providerPanelY),
            Size = new Size(panelWidth, 5 * AiRowH + 10),
            Visible = false
        };
        txtAiAnthropicBaseUrl   = AddPanelRow(pnlAnthropic, "BaseUrl:",   0);
        txtAiAnthropicApiKey    = AddPanelRow(pnlAnthropic, "ApiKey:",    1, true);
        txtAiAnthropicModel     = AddPanelRow(pnlAnthropic, "Model:",     2);
        txtAiAnthropicVersion   = AddPanelRow(pnlAnthropic, "Version:",   3);
        txtAiAnthropicMaxTokens = AddPanelRow(pnlAnthropic, "MaxTokens:", 4);

        pnlBedrock = new Panel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(5, providerPanelY),
            Size = new Size(panelWidth, 5 * AiRowH + 10),
            Visible = false
        };
        txtAiBedrockRegion    = AddPanelRow(pnlBedrock, "Region:",    0);
        txtAiBedrockModel     = AddPanelRow(pnlBedrock, "Model:",     1);
        txtAiBedrockProfile   = AddPanelRow(pnlBedrock, "Profile:",   2);
        txtAiBedrockApiKey    = AddPanelRow(pnlBedrock, "ApiKey:",    3, true);
        txtAiBedrockMaxTokens = AddPanelRow(pnlBedrock, "MaxTokens:", 4);
        _aiToolTip.SetToolTip(txtAiBedrockRegion, "AWS region to call Bedrock in, e.g. us-east-1.");
        _aiToolTip.SetToolTip(txtAiBedrockProfile,
            "Optional. AWS profile to take credentials from when no ApiKey is set.\n" +
            "Leave blank (and ApiKey blank) to use the default AWS credentials: an IAM role (EC2, ECS, EKS),\n" +
            "the AWS_* environment variables, or the default profile of the account the service runs as.\n" +
            "A short-term Bedrock key is made from the credentials for each call - AWS's recommended setup.");
        _aiToolTip.SetToolTip(txtAiBedrockApiKey,
            "Optional. Amazon Bedrock API key. AWS recommends API keys for exploration only:\n" +
            "leave blank to use AWS credentials instead (see Profile).");
        _aiToolTip.SetToolTip(txtAiBedrockModel,
            "Model ID or inference profile ID to use by default, e.g. us.anthropic.claude-sonnet-4-6.\n" +
            "Most current models must be called through an inference profile (the ID with a region prefix such as us. or eu.).\n" +
            "The GUI's model picker lists the models available to the key in the region.");
        _aiToolTip.SetToolTip(txtAiBedrockMaxTokens, "Optional limit on the length of the answer. Leave blank for the model's default.");

        pnlOllama = new Panel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(5, providerPanelY),
            Size = new Size(panelWidth, 6 * AiRowH + 10),
            Visible = false
        };
        txtAiOllamaBaseUrl       = AddPanelRow(pnlOllama, "BaseUrl:",       0);
        txtAiOllamaModel         = AddPanelRow(pnlOllama, "Model:",         1);
        txtAiOllamaContextLength = AddPanelRow(pnlOllama, "ContextLength:", 2);
        txtAiOllamaMaxTokens     = AddPanelRow(pnlOllama, "MaxTokens:",     3);
        txtAiOllamaTimeoutSeconds = AddPanelRow(pnlOllama, "TimeoutSeconds:", 4);
        txtAiOllamaApiKey        = AddPanelRow(pnlOllama, "ApiKey:",        5, true);
        _aiToolTip.SetToolTip(txtAiOllamaTimeoutSeconds,
            "Seconds to wait for Ollama to answer. Default: 240.\n" +
            "Slow models (large models, or running on CPU) may need longer.\n" +
            "The GUI waits up to 300 seconds for the AI service, so higher values only help other callers.");
        _aiToolTip.SetToolTip(txtAiOllamaBaseUrl, "Ollama server URL. Default: http://localhost:11434");
        _aiToolTip.SetToolTip(txtAiOllamaModel, "Name of a model pulled onto the Ollama server, e.g. llama3.2:3b (see 'ollama list').");
        _aiToolTip.SetToolTip(txtAiOllamaContextLength,
            "Context window in tokens requested from Ollama (num_ctx). Default: 32768.\n" +
            "Ollama's own default is too small for deadlock and query plan analysis.\n" +
            "Larger values use more GPU/system memory.");
        _aiToolTip.SetToolTip(txtAiOllamaMaxTokens, "Optional limit on the length of the answer (num_predict). Leave blank for no limit.");
        _aiToolTip.SetToolTip(txtAiOllamaApiKey, "Optional. Ollama does not use a key - only needed behind an authenticating proxy or for ollama.com.");

        grpConfig.Controls.Add(pnlAzure);
        grpConfig.Controls.Add(pnlAnthropic);
        grpConfig.Controls.Add(pnlBedrock);
        grpConfig.Controls.Add(pnlOllama);

        tabAiService.Controls.Add(grpService);
        tabAiService.Controls.Add(grpConfig);

        // These controls are built in design units after the form has auto-scaled, so scale them
        // the same way. Otherwise they keep their design size at other display scales.
        var scale = new SizeF(
            CurrentAutoScaleDimensions.Width / AiDesignAutoScaleDimensions.Width,
            CurrentAutoScaleDimensions.Height / AiDesignAutoScaleDimensions.Height);
        if (scale != new SizeF(1F, 1F))
        {
            foreach (Control control in tabAiService.Controls)
                control.Scale(scale);
        }

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
        pnlBedrock.Visible   = p.Equals("Bedrock",     StringComparison.OrdinalIgnoreCase);
        pnlOllama.Visible    = p.Equals("Ollama",      StringComparison.OrdinalIgnoreCase);
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
        _aiCustomHosting = AiHostingSettings.IsCustomHostingConfigured(j);
        chkAiUseHttps.Enabled = !_aiCustomHosting;
        // With existing hosting the box only shows the URL scheme. Otherwise, when not set explicitly,
        // match the service's own default, which keeps a reverse-proxy setup (https:// URL on another port) on HTTP.
        chkAiUseHttps.Checked = _aiCustomHosting
            ? txtAiServiceUrl.Text.Trim().StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            : j["Security"]?["EnableHttps"]?.Value<bool?>()
              ?? !AiHttpsCertificate.IsTlsTerminatedUpstream(txtAiServiceUrl.Text, port);

        LoadAiCertificateList(j["Security"]?["CertificateThumbprint"]?.Value<string>());
        SyncAiServiceUrlScheme();

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

        txtAiBedrockRegion.Text    = j["Bedrock"]?["Region"]?.Value<string>()    ?? string.Empty;
        txtAiBedrockApiKey.Text    = GetPlainText(j["Bedrock"]?["ApiKey"]?.Value<string>());
        txtAiBedrockProfile.Text   = j["Bedrock"]?["Profile"]?.Value<string>()   ?? string.Empty;
        txtAiBedrockModel.Text     = j["Bedrock"]?["Model"]?.Value<string>()     ?? string.Empty;
        txtAiBedrockMaxTokens.Text = j["Bedrock"]?["MaxTokens"]?.Value<string>() ?? string.Empty;

        txtAiOllamaBaseUrl.Text       = j["Ollama"]?["BaseUrl"]?.Value<string>()       ?? string.Empty;
        txtAiOllamaApiKey.Text        = GetPlainText(j["Ollama"]?["ApiKey"]?.Value<string>());
        txtAiOllamaModel.Text         = j["Ollama"]?["Model"]?.Value<string>()         ?? string.Empty;
        txtAiOllamaContextLength.Text = j["Ollama"]?["ContextLength"]?.Value<string>() ?? string.Empty;
        txtAiOllamaMaxTokens.Text     = j["Ollama"]?["MaxTokens"]?.Value<string>()     ?? string.Empty;
        txtAiOllamaTimeoutSeconds.Text = j["Ollama"]?["TimeoutSeconds"]?.Value<string>() ?? string.Empty;
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
            SyncAiServiceUrlScheme();
            j["Registration"]!["Port"] = (int)numAiPort.Value;
            j["Registration"]!["ServiceUrl"] = txtAiServiceUrl.Text.Trim();

            j["Security"] ??= new JObject();
            if (!_aiCustomHosting)
            {
                j["Security"]!["EnableHttps"] = chkAiUseHttps.Checked;
                j["Security"]!["CertificateThumbprint"] = (cboAiCertificate?.SelectedItem as AiCertificateListItem)?.Thumbprint ?? string.Empty;
            }

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

            j["Bedrock"] ??= new JObject();
            j["Bedrock"]!["Region"]    = txtAiBedrockRegion.Text.Trim();
            j["Bedrock"]!["ApiKey"]    = GetStoredValue(txtAiBedrockApiKey.Text, isSecret: true);
            j["Bedrock"]!["Profile"]   = txtAiBedrockProfile.Text.Trim();
            j["Bedrock"]!["Model"]     = txtAiBedrockModel.Text;
            j["Bedrock"]!["MaxTokens"] = txtAiBedrockMaxTokens.Text;

            j["Ollama"] ??= new JObject();
            j["Ollama"]!["BaseUrl"]       = txtAiOllamaBaseUrl.Text;
            j["Ollama"]!["ApiKey"]        = GetStoredValue(txtAiOllamaApiKey.Text, isSecret: true);
            j["Ollama"]!["Model"]         = txtAiOllamaModel.Text;
            j["Ollama"]!["ContextLength"] = txtAiOllamaContextLength.Text;
            j["Ollama"]!["MaxTokens"]     = txtAiOllamaMaxTokens.Text;
            j["Ollama"]!["TimeoutSeconds"] = txtAiOllamaTimeoutSeconds.Text;

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

    private void UpdateAiCertificateState()
    {
        if (cboAiCertificate == null || chkAiUseHttps == null)
            return;
        cboAiCertificate.Enabled = chkAiUseHttps.Checked && !_aiCustomHosting;
    }

    /// <summary>
    /// Switches an http:// service URL to https:// while Use HTTPS is on. With it off, the URL is left
    /// alone: an https:// URL then points at a reverse proxy that terminates TLS. With existing
    /// hosting the URL is never changed, because the service keeps that binding's scheme.
    /// </summary>
    private void SyncAiServiceUrlScheme()
    {
        if (txtAiServiceUrl == null || chkAiUseHttps == null || !chkAiUseHttps.Checked || _aiCustomHosting)
            return;

        var text = txtAiServiceUrl.Text.Trim();
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
            return;
        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            return;

        // Keep an explicit port only. Carrying over http's default port 80 would register
        // https://host:80 instead of letting registration fill in the service port.
        var builder = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = uri.IsDefaultPort ? -1 : uri.Port };
        txtAiServiceUrl.Text = builder.Uri.GetComponents(
            UriComponents.Scheme | UriComponents.UserInfo | UriComponents.Host | UriComponents.Port | UriComponents.Path,
            UriFormat.UriEscaped).TrimEnd('/');
    }

    private void LoadAiCertificateList(string selectedThumbprint)
    {
        if (cboAiCertificate == null)
            return;

        foreach (var existing in cboAiCertificate.Items)
            (existing as AiCertificateListItem)?.Dispose();
        cboAiCertificate.Items.Clear();
        _aiCertificateStoreError = null;
        cboAiCertificate.Items.Add(new AiCertificateListItem("Automatic (self-signed)", null));

        try
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            foreach (var cert in store.Certificates.Cast<X509Certificate2>().OrderBy(c => c.Subject, StringComparer.OrdinalIgnoreCase))
            {
                X509Certificate2 publicCert;
                try
                {
                    publicCert = X509CertificateLoader.LoadCertificate(cert.Export(X509ContentType.Cert));
                }
                catch
                {
                    continue;
                }

                var hasPrivateKey = false;
                try
                {
                    hasPrivateKey = cert.HasPrivateKey;
                }
                catch
                {
                    hasPrivateKey = false;
                }

                var expired = cert.NotAfter.ToUniversalTime() <= DateTime.UtcNow;
                var caption = expired
                    ? cert.Subject + " (expired " + cert.NotAfter.ToString("d") + ")"
                    : cert.Subject + " (expires " + cert.NotAfter.ToString("d") + ")";
                if (!hasPrivateKey)
                    caption += " (no private key)";
                cboAiCertificate.Items.Add(new AiCertificateListItem(caption, cert.Thumbprint, publicCert, cert.NotAfter, hasPrivateKey: hasPrivateKey));
            }
        }
        catch (Exception ex)
        {
            _aiCertificateStoreError = ex.Message;
        }

        AiCertificateListItem match = null;
        if (!string.IsNullOrWhiteSpace(selectedThumbprint))
        {
            var normalized = AiHttpsCertificate.NormalizeThumbprint(selectedThumbprint);
            foreach (AiCertificateListItem item in cboAiCertificate.Items)
            {
                if (item.Thumbprint != null && string.Equals(AiHttpsCertificate.NormalizeThumbprint(item.Thumbprint), normalized, StringComparison.OrdinalIgnoreCase))
                {
                    match = item;
                    break;
                }
            }

            if (match == null)
            {
                match = new AiCertificateListItem("Not in certificate store (" + normalized + ")", normalized, missing: true);
                cboAiCertificate.Items.Add(match);
            }
        }

        cboAiCertificate.SelectedItem = match ?? cboAiCertificate.Items[0];
        UpdateAiCertificateState();
    }

    private string GetAiSecurityWarningMessage()
    {
        if (txtAiServiceUrl == null || chkAiUseHttps == null)
            return null;

        var serviceUrl = txtAiServiceUrl.Text.Trim();
        if (string.IsNullOrWhiteSpace(serviceUrl))
            return null;

        if (!IsAiServiceUrlValid() || !Uri.TryCreate(serviceUrl, UriKind.Absolute, out var uri))
            return null;

        if (_aiCustomHosting)
        {
            return "The AI service uses the Kestrel endpoints or listen URLs in its configuration, so Use HTTPS and the certificate come from that configuration. The service URL is registered as entered.";
        }

        if (!chkAiUseHttps.Checked)
        {
            return uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                ? "Use HTTPS is off and the service URL uses HTTPS. The service listens on HTTP, so a reverse proxy must terminate TLS at this URL. The API key is not encrypted between the proxy and the service."
                : "Use HTTPS is turned off, so the API key is sent between GUI clients and the AI service without encryption.";
        }

        if (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return "Use HTTPS is on. The service listens with TLS and will register an https:// address. Save to update this URL.";
        }

        if (!string.IsNullOrWhiteSpace(_aiCertificateStoreError))
        {
            return "The local computer certificate store could not be opened (" + _aiCertificateStoreError + "). Automatic self-signed is still available.";
        }

        var selected = cboAiCertificate?.SelectedItem as AiCertificateListItem;
        if (selected == null || selected.IsAutomatic)
            return null;
        if (selected.Missing)
        {
            return "The selected certificate is not in the Local Computer Personal store. The AI service will not start until it is installed, or Automatic (self-signed) is selected.";
        }

        if (!selected.HasPrivateKey)
        {
            return "The selected certificate has no private key this process can use. The AI service account also needs permission to use the key.";
        }

        if (selected.NotAfter.HasValue && selected.NotAfter.Value.ToUniversalTime() <= DateTime.UtcNow)
        {
            return "The selected certificate expired on " + selected.NotAfter.Value.ToString("d") + ".";
        }

        if (selected.Certificate != null && !AiHttpsCertificate.HostMatches(selected.Certificate, uri.Host))
        {
            return "The certificate name does not match " + uri.Host + ". GUI clients that rely on Windows trust will reject it. Clients can still connect while the repository thumbprint matches this certificate.";
        }

        return null;
    }

    private sealed class AiCertificateListItem : IDisposable
    {
        public AiCertificateListItem(string caption, string thumbprint, X509Certificate2 certificate = null, DateTime? notAfter = null, bool missing = false, bool hasPrivateKey = true)
        {
            Caption = caption;
            Thumbprint = thumbprint;
            Certificate = certificate;
            NotAfter = notAfter;
            Missing = missing;
            HasPrivateKey = hasPrivateKey;
        }

        public string Caption { get; }
        public string Thumbprint { get; }
        public X509Certificate2 Certificate { get; }
        public DateTime? NotAfter { get; }
        public bool Missing { get; }
        public bool HasPrivateKey { get; }
        public bool IsAutomatic => string.IsNullOrEmpty(Thumbprint) && Certificate == null && !Missing;
        public override string ToString() => Caption;
        public void Dispose() => Certificate?.Dispose();
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
