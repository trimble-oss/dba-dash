#nullable enable
using DBADash.Mcp;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DBADashGUI.Theme;

namespace DBADashGUI.AI
{
    /// <summary>
    /// Registers DBADashMCP.exe - the local MCP server shipped with the GUI - with the AI tools the user has, so they
    /// can ask Claude Code, Cursor and the like about the repository without editing config files.  The server runs as
    /// the user, with the repository connection chosen here, so the dialog also checks the user can use it.
    /// </summary>
    internal sealed class ConnectAIToolsForm : Form
    {
        private const int ClientColumn = 0;
        private const int StatusColumn = 1;
        private const int AddColumn = 2;
        private const int RemoveColumn = 3;

        private readonly string _exePath = Path.Combine(Application.StartupPath, "DBADashMCP.exe");
        private readonly RepositoryConnectionList _connections = RepositoryConnectionList.GetRepositoryConnectionList();
        private readonly IReadOnlyList<McpClient> _clients = McpClient.All();

        private readonly ComboBox _connection = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
        private readonly Label _access = new() { AutoSize = true, MaximumSize = new Size(700, 0), Padding = new Padding(0, 4, 0, 4) };
        private readonly DataGridView _grid = new()
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ReadOnly = true
        };
        private readonly Label _result = new() { Dock = DockStyle.Bottom, AutoSize = true, MaximumSize = new Size(740, 0), Padding = new Padding(6) };
        private string? _grantStatement;

        private readonly ComboBox _mcpDetail = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
        private readonly Label _mcpDetailText = new() { AutoSize = true, MaximumSize = new Size(700, 0), Padding = new Padding(0, 4, 0, 4) };
        private readonly ComboBox _builtInDetail = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 340 };
        private readonly Label _builtInDetailText = new() { AutoSize = true, MaximumSize = new Size(700, 0), Padding = new Padding(0, 4, 0, 4) };
        private readonly Button _builtInSave = new() { Text = "Save", AutoSize = true, Enabled = false };
        private bool _loadingBuiltIn;

        /// <summary>Counts the reads of the built-in setting, so one that finishes after the repository has changed is ignored.</summary>
        private int _builtInLoad;

        public ConnectAIToolsForm()
        {
            Text = "Connect AI Tools";
            Width = 860;
            Height = 660;
            MinimumSize = new Size(700, 560);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var intro = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                MaximumSize = new Size(750, 0),
                Padding = new Padding(6),
                Text = "Lets Claude Code, Cursor and other AI tools read this DBA Dash repository through DBA Dash's MCP server " +
                       "(DBADashMCP.exe).  It runs on this machine as you, with your permissions, and only reads.  The tool can " +
                       "also see what you have open here - the selected instance and time window, or an open deadlock or plan.  " +
                       "What it reads goes to that tool's AI model."
            };

            var top = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(6, 0, 6, 0), FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
            top.Controls.Add(new Label { Text = "Repository connection:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            top.Controls.Add(_connection);
            top.SetFlowBreak(_connection, true);
            top.Controls.Add(_access);

            // What the AI tools send on.  The MCP setting is this user's, in a file their registered servers read on each call; the built-in AI's is
            // the repository's, changed here by a db_owner.
            var detail = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(6, 0, 6, 0) };
            detail.Controls.Add(new Label { Text = "Masking for MCP tools:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, 0, 0);
            detail.Controls.Add(_mcpDetail, 1, 0);
            // The descriptions span both columns, so they wrap to the window rather than running off it past the labels.
            detail.Controls.Add(_mcpDetailText, 0, 1);
            detail.SetColumnSpan(_mcpDetailText, 2);
            detail.Controls.Add(new Label { Text = "Least masking for built-in AI:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) }, 0, 2);
            var builtInRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = Padding.Empty };
            builtInRow.Controls.Add(_builtInDetail);
            builtInRow.Controls.Add(_builtInSave);
            detail.Controls.Add(builtInRow, 1, 2);
            detail.Controls.Add(_builtInDetailText, 0, 3);
            detail.SetColumnSpan(_builtInDetailText, 2);

            foreach (var level in DataDetailLevels.All)
            {
                _mcpDetail.Items.Add(new DataDetailItem(level.Word, level.Label));
                _builtInDetail.Items.Add(new DataDetailItem(level.Word, level.Label));
            }

            // Wide enough for the longest level's label.
            var comboWidth = DataDetailLevels.All.Max(l => TextRenderer.MeasureText(l.Label, Font).Width) + SystemInformation.VerticalScrollBarWidth + 16;
            _mcpDetail.Width = comboWidth;
            _builtInDetail.Width = comboWidth;

            _mcpDetail.SelectedItem = _mcpDetail.Items.Cast<DataDetailItem>().FirstOrDefault(i => i.Word == DataDetailLevels.McpCurrent());
            DescribeMcpDetail();
            _mcpDetail.SelectedIndexChanged += (_, _) =>
            {
                if (_mcpDetail.SelectedItem is not DataDetailItem item) return;
                try
                {
                    DataDetailLevels.SetMcp(item.Word);
                }
                catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
                {
                    MessageBox.Show(this, $"Couldn't save the masking setting: {ex.Message}", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                DescribeMcpDetail();
            };
            _builtInDetail.SelectedIndexChanged += (_, _) =>
            {
                if (_builtInDetail.SelectedItem is not DataDetailItem item) return;
                _builtInDetailText.Text = DataDetailLevels.Describe(item.Word) + "  A request can mask more than this, never less.";
                _builtInSave.Enabled = !_loadingBuiltIn;
            };
            _builtInSave.Click += async (_, _) => await SaveBuiltInAsync();

            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "AI tool", FillWeight = 22 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", FillWeight = 58 });
            _grid.Columns.Add(new DataGridViewButtonColumn { HeaderText = "", FillWeight = 10 });
            _grid.Columns.Add(new DataGridViewButtonColumn { HeaderText = "", FillWeight = 10 });
            _grid.CellContentClick += async (_, e) => await CellClickAsync(e);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(6) };
            var close = new Button { Text = "Close", AutoSize = true, DialogResult = DialogResult.Cancel };
            var copy = new Button { Text = "Copy config JSON", AutoSize = true };
            var copyGrant = new Button { Text = "Copy GRANT", AutoSize = true, Visible = false };
            copy.Click += (_, _) =>
            {
                Clipboard.SetText(Entry().ToConfigJson());
                _result.Text = "Copied the mcpServers JSON for DBA Dash.  Most AI tools that take MCP servers accept it.";
            };
            copyGrant.Click += (_, _) =>
            {
                if (_grantStatement is null) return;
                Clipboard.SetText(_grantStatement);
                _result.Text = "Copied.  A DBA with rights on the repository database needs to run it there.";
            };
            buttons.Controls.Add(close);
            buttons.Controls.Add(copy);
            buttons.Controls.Add(copyGrant);
            CancelButton = close;

            // Wrapped text follows the window's width.
            void Wrap()
            {
                var width = Math.Max(200, ClientSize.Width - 24);
                intro.MaximumSize = new Size(width, 0);
                _access.MaximumSize = new Size(width, 0);
                _mcpDetailText.MaximumSize = new Size(width, 0);
                _builtInDetailText.MaximumSize = new Size(width, 0);
                _result.MaximumSize = new Size(width, 0);
            }
            Resize += (_, _) => Wrap();
            Wrap();

            Controls.Add(_grid);
            Controls.Add(detail);
            Controls.Add(top);
            Controls.Add(intro);
            Controls.Add(_result);
            Controls.Add(buttons);

            foreach (var connection in _connections)
            {
                _connection.Items.Add(connection.Name);
            }

            var current = Common.ConnectionString is { } cs ? _connections.FindByConnectionString(cs) : null;
            _connection.SelectedItem = current?.Name ?? _connections.GetDefaultConnection()?.Name;
            _connection.SelectedIndexChanged += async (_, _) =>
            {
                RefreshStatus();
                await CheckAccessAsync(copyGrant);
                await LoadBuiltInAsync();
            };

            Load += async (_, _) =>
            {
                if (!File.Exists(_exePath))
                {
                    _result.Text = $"DBADashMCP.exe isn't installed with this copy of DBA Dash ({_exePath}), so there's nothing to register.";
                    _grid.Enabled = false;
                    copy.Enabled = false;
                    return;
                }

                RefreshStatus();
                await CheckAccessAsync(copyGrant);
                await LoadBuiltInAsync();
            };

            this.ApplyTheme();
        }

        private void DescribeMcpDetail() =>
            _mcpDetailText.Text = _mcpDetail.SelectedItem is DataDetailItem item
                ? DataDetailLevels.Describe(item.Word) + "  Your registered AI tools mask their next call with it, sessions already open included.  The Ask button's Claude Code window can change it for one request."
                : string.Empty;

        /// <summary>No masking level: a registered server follows the setting above on each call.</summary>
        private McpServerEntry Entry() => McpServerEntry.ForConnection(_exePath, _connection.SelectedItem as string);

        private sealed record DataDetailItem(string Word, string Label)
        {
            public override string ToString() => Label;
        }

        /// <summary>
        /// Reads the built-in AI's setting from the chosen repository.  A repository without the procedure, or a
        /// connection that fails, says so and leaves the Save button off.
        /// </summary>
        private async Task LoadBuiltInAsync()
        {
            var load = ++_builtInLoad;
            _loadingBuiltIn = true;
            _builtInSave.Enabled = false;
            try
            {
                var name = _connection.SelectedItem as string;
                var connection = name is null ? null : _connections.FindByName(name);
                if (connection is null)
                {
                    _builtInDetailText.Text = string.Empty;
                    return;
                }

                _builtInDetailText.Text = "Reading the setting...";
                var word = DataDetailLevels.Normalise(await DataDetailLevels.ReadBuiltInAsync(connection.ConnectionString));
                if (load != _builtInLoad) return; // another repository was chosen meanwhile, and its read is under way
                if (word is null)
                {
                    _builtInDetailText.Text = "This repository doesn't have the built-in AI data setting yet - upgrade the repository to the same version as this copy of DBA Dash.";
                    _builtInDetailText.ForeColor = DashColors.Warning;
                    _builtInDetail.Enabled = false;
                    return;
                }

                _builtInDetail.Enabled = true;
                _builtInDetail.SelectedItem = _builtInDetail.Items.Cast<DataDetailItem>().First(i => i.Word == word);
                _builtInDetailText.ForeColor = SystemColors.ControlText;
                _builtInDetailText.Text = DataDetailLevels.Describe(word) + "  A request can mask more than this, never less.";
            }
            catch (Exception ex)
            {
                if (load != _builtInLoad) return;
                _builtInDetailText.Text = $"Couldn't read the built-in AI setting: {ex.Message}";
                _builtInDetailText.ForeColor = DashColors.Warning;
                _builtInDetail.Enabled = false;
            }
            finally
            {
                if (load == _builtInLoad) _loadingBuiltIn = false;
            }
        }

        /// <summary>Saves the built-in AI's setting to the chosen repository.  Only a db_owner can; the procedure says so if not.</summary>
        private async Task SaveBuiltInAsync()
        {
            var name = _connection.SelectedItem as string;
            var connection = name is null ? null : _connections.FindByName(name);
            if (connection is null || _builtInDetail.SelectedItem is not DataDetailItem item) return;

            if (item.Word == DataDetailLevels.Full &&
                MessageBox.Show(this, "Full lets a built-in AI request turn masking off, sending statement text, parameter values, logins, hosts and application names to the AI provider.  Requests are still masked unless the user unticks masking for that request.  Save it?",
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                await LoadBuiltInAsync();
                return;
            }

            try
            {
                await DataDetailLevels.WriteBuiltInAsync(connection.ConnectionString, item.Word);
                _result.Text = $"Saved.  The built-in AI uses '{item.Label}' within a minute.";
                _builtInSave.Enabled = false;
            }
            catch (Exception ex)
            {
                _result.Text = $"Not saved: {ex.Message}";
                await LoadBuiltInAsync();
            }
        }

        private void RefreshStatus()
        {
            var entry = Entry();
            _grid.Rows.Clear();
            foreach (var client in _clients)
            {
                var status = client.GetStatus(entry);
                var index = _grid.Rows.Add(client.Name, status.Detail,
                    status.State switch
                    {
                        McpClientState.NotRegistered or McpClientState.NotInstalled => "Add",
                        McpClientState.RegisteredDifferently => "Update",
                        _ => string.Empty
                    },
                    status.State is McpClientState.Registered or McpClientState.RegisteredDifferently ? "Remove" : string.Empty);
                var row = _grid.Rows[index];
                row.Tag = (client, status);
                if (status.State == McpClientState.NotInstalled)
                {
                    row.DefaultCellStyle.ForeColor = SystemColors.GrayText;
                }
            }
        }

        private async Task CellClickAsync(DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex is not (AddColumn or RemoveColumn)) return;
            if (_grid.Rows[e.RowIndex].Tag is not (McpClient client, McpClientStatus status)) return;
            if (string.IsNullOrEmpty(_grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value as string)) return;

            var adding = e.ColumnIndex == AddColumn;
            if (adding && status.State == McpClientState.NotInstalled &&
                MessageBox.Show(this, $"{client.Name} doesn't look to be installed.  Register DBA Dash with it anyway?", Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            if (!adding &&
                MessageBox.Show(this, $"Remove DBA Dash's MCP server from {client.Name}?", Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            UseWaitCursor = true;
            _grid.Enabled = false;
            try
            {
                _result.Text = adding
                    ? await client.RegisterAsync(Entry(), CancellationToken.None)
                    : await client.UnregisterAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _result.Text = $"{client.Name}: {ex.Message}";
            }
            finally
            {
                UseWaitCursor = false;
                _grid.Enabled = true;
                RefreshStatus();
            }
        }

        /// <summary>
        /// Whether the chosen connection's login can use the MCP tools: it needs the McpUser role, which db_owner (and so
        /// sysadmin) covers.  The role not existing means the repository predates the MCP server.
        /// </summary>
        private async Task CheckAccessAsync(Button copyGrant)
        {
            _grantStatement = null;
            copyGrant.Visible = false;
            var name = _connection.SelectedItem as string;
            var connection = name is null ? null : _connections.FindByName(name);
            if (connection is null)
            {
                _access.Text = string.Empty;
                return;
            }

            _access.Text = "Checking your access...";
            try
            {
                await using var cn = new SqlConnection(connection.ConnectionString);
                await cn.OpenAsync();
                await using var cmd = new SqlCommand(
                    "SELECT DATABASE_PRINCIPAL_ID('McpUser'), ISNULL(IS_ROLEMEMBER('McpUser'), 0), ISNULL(IS_ROLEMEMBER('db_owner'), 0), USER_NAME()", cn);
                await using var reader = await cmd.ExecuteReaderAsync();
                await reader.ReadAsync();

                var roleExists = !reader.IsDBNull(0);
                var member = reader.GetInt32(1) == 1 || reader.GetInt32(2) == 1;
                var user = reader.GetString(3);

                if (!roleExists)
                {
                    _access.Text = "This repository's DBA Dash database is older than the MCP server - upgrade it to the same version before using the AI tools.";
                    _access.ForeColor = DashColors.Warning;
                }
                else if (member)
                {
                    _access.Text = $"You ({user}) can use the MCP tools on this repository.";
                    _access.ForeColor = DashColors.Success;
                }
                else
                {
                    _grantStatement = $"ALTER ROLE McpUser ADD MEMBER [{user.Replace("]", "]]")}];";
                    _access.Text = $"You ({user}) aren't in the repository's McpUser role, so the tools will be refused.  A DBA needs to run: {_grantStatement}";
                    _access.ForeColor = DashColors.Warning;
                    copyGrant.Visible = true;
                }
            }
            catch (Exception ex)
            {
                _access.Text = $"Couldn't check your access: {ex.Message}";
                _access.ForeColor = DashColors.Warning;
            }
        }
    }
}
