using DBADash;
using DBADashGUI.Theme;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace DBADashServiceConfig;

public partial class ServiceConfig
{
    private Panel pnlAzure;
    private Panel pnlAnthropic;
    private ComboBox cboAiProvider;
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

        // ── Section 1: Service Control ─────────────────────────────────────
        var grpService = new GroupBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(AiGrpX, 12),
            Size = new Size(AiGrpW, 180),
            TabStop = false,
            Text = IsAdmin
                ? "DBADashAI Windows Service"
                : "DBADashAI Windows Service (Run as Administrator to manage)",
            Enabled = IsAdmin
        };
        grpService.Controls.Add(new Label { AutoSize = true, Location = new Point(AiLabelX, 50), Text = $"Service Name: {AiServiceName}" });
        grpService.Controls.Add(new Label { AutoSize = true, Location = new Point(AiLabelX, 85), Text = $"Service URL:  {AiServiceUrl}" });

        lblAiServiceStatus = new Label { AutoSize = true, Location = new Point(AiLabelX, 120), Name = "lblAiServiceStatus", Text = "AI Service Status: ..." };
        grpService.Controls.Add(lblAiServiceStatus);

        lnkStartAi = new LinkLabel
        {
            Image = Properties.Resources.StartWithoutDebug_16x,
            ImageAlign = ContentAlignment.MiddleLeft,
            LinkColor = Color.FromArgb(0, 79, 131),
            Location = new Point(AiGrpX + 10, 650),
            Name = "lnkStartAi",
            Size = new Size(160, 28),
            Padding = new Padding(20, 0, 0, 0),
            TabStop = true,
            Text = "Start",
            TextAlign = ContentAlignment.MiddleLeft,
            Enabled = IsAdmin
        };
        lnkStartAi.LinkClicked += async (_, _) => await StartAiServiceAsync();

        lnkStopAi = new LinkLabel
        {
            Image = Properties.Resources.Stop_16x,
            ImageAlign = ContentAlignment.MiddleLeft,
            LinkColor = Color.FromArgb(0, 79, 131),
            Location = new Point(AiGrpX + 180, 650),
            Name = "lnkStopAi",
            Size = new Size(160, 28),
            Padding = new Padding(20, 0, 0, 0),
            TabStop = true,
            Text = "Stop",
            TextAlign = ContentAlignment.MiddleLeft,
            Enabled = IsAdmin
        };
        lnkStopAi.LinkClicked += async (_, _) => await StopAiServiceAsync();

        lnkInstallAi = new LinkLabel
        {
            Image = Properties.Resources.install,
            ImageAlign = ContentAlignment.MiddleLeft,
            LinkColor = Color.FromArgb(0, 79, 131),
            Location = new Point(AiGrpX + 350, 650),
            Name = "lnkInstallAi",
            Size = new Size(360, 28),
            Padding = new Padding(20, 0, 0, 0),
            TabStop = true,
            Text = "Install AI service",
            TextAlign = ContentAlignment.MiddleLeft,
            Enabled = IsAdmin
        };
        lnkInstallAi.LinkClicked += async (_, _) =>
        {
            if (IsAiServiceInstalled()) await UninstallAiServiceAsync();
            else await InstallAiServiceAsync();
        };

        // ── Section 2: Provider selection ──────────────────────────────────
        var grpSettings = new GroupBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(AiGrpX, 222),
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



        // ── Section 3: Provider configuration ──────────────────────────────
        // Each provider has its own Panel placed at the same location inside
        // the group box. Only one panel is visible at a time.
        var grpConfig = new GroupBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Location = new Point(AiGrpX, 362),
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
        tabAiService.Controls.Add(lnkStartAi);
        tabAiService.Controls.Add(lnkStopAi);
        tabAiService.Controls.Add(lnkInstallAi);

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

    private void LoadAiSettings()
    {
        var provider = collectionConfig.AIProvider ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(provider) && cboAiProvider.Items.Contains(provider))
            cboAiProvider.SelectedItem = provider;
        else
            cboAiProvider.SelectedIndex = 0;

        txtAiAzureEndpoint.Text   = collectionConfig.AzureOpenAIEndpoint ?? string.Empty;
        txtAiAzureApiKey.Text     = collectionConfig.AzureOpenAIApiKeyDecrypted ?? string.Empty;
        txtAiAzureDeployment.Text = collectionConfig.AzureOpenAIDeployment ?? string.Empty;
        txtAiAzureApiVersion.Text = collectionConfig.AzureOpenAIApiVersion ?? string.Empty;

        txtAiAnthropicBaseUrl.Text   = collectionConfig.AnthropicBaseUrl ?? string.Empty;
        txtAiAnthropicApiKey.Text    = collectionConfig.AnthropicApiKeyDecrypted ?? string.Empty;
        txtAiAnthropicModel.Text     = collectionConfig.AnthropicModel ?? string.Empty;
        txtAiAnthropicVersion.Text   = collectionConfig.AnthropicVersion ?? string.Empty;
        txtAiAnthropicMaxTokens.Text = collectionConfig.AnthropicMaxTokens ?? string.Empty;
    }

    private void ApplyAiSettingsToConfig()
    {
        if (cboAiProvider == null) return;

        collectionConfig.AIProvider           = cboAiProvider.SelectedItem?.ToString() ?? string.Empty;
        collectionConfig.AzureOpenAIEndpoint   = txtAiAzureEndpoint.Text;
        collectionConfig.AzureOpenAIApiKey     = txtAiAzureApiKey.Text;
        collectionConfig.AzureOpenAIDeployment = txtAiAzureDeployment.Text;
        collectionConfig.AzureOpenAIApiVersion = txtAiAzureApiVersion.Text;

        collectionConfig.AnthropicBaseUrl   = txtAiAnthropicBaseUrl.Text;
        collectionConfig.AnthropicApiKey    = txtAiAnthropicApiKey.Text;
        collectionConfig.AnthropicModel     = txtAiAnthropicModel.Text;
        collectionConfig.AnthropicVersion   = txtAiAnthropicVersion.Text;
        collectionConfig.AnthropicMaxTokens = txtAiAnthropicMaxTokens.Text;
    }
}
