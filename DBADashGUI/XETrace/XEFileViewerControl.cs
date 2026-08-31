using DBADashGUI.CustomReports;
using DBADashGUI.Theme;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// Standalone viewer for XE trace data loaded from a file on disk (a native <c>.xel</c>, or a DBA Dash-native
    /// JSON/XML save).  Provides Open / Save / Clear over a shared <see cref="XEResultsControl"/> so an offline or
    /// shared capture can be reviewed with the same grid (clickable SQL/plan/deadlock columns, Group By, filters) as a
    /// live or history view.  Reading/shredding is done by <see cref="XEFileLoader"/>; saving by
    /// <see cref="GridSerializer"/>.  Hosted in a window by <see cref="XETraceLauncher.LaunchFileViewer"/>.
    /// </summary>
    public sealed class XEFileViewerControl : UserControl
    {
        private readonly ToolStrip _toolbar;
        private readonly ToolStripButton _openButton;
        private readonly ToolStripDropDownButton _saveButton;
        private readonly ToolStripButton _clearButton;
        private readonly StatusStrip _statusStrip;
        private readonly ToolStripStatusLabel _status;
        private readonly XEResultsControl _results;

        private bool _loading;

        public XEFileViewerControl()
        {
            _openButton = new ToolStripButton("Open File...") { Image = Properties.Resources.FolderOpened_16x };
            _saveButton = new ToolStripDropDownButton("Save Events")
            { Image = Properties.Resources.Save_16x, DisplayStyle = ToolStripItemDisplayStyle.ImageAndText, Enabled = false };
            _saveButton.DropDownItems.Add(new ToolStripMenuItem("Save as JSON...", null, (_, _) => Save(GridSerializer.JsonExtension)));
            _saveButton.DropDownItems.Add(new ToolStripMenuItem("Save as Compressed JSON...", null, (_, _) => Save(GridSerializer.CompressedJsonExtension)));
            _saveButton.DropDownItems.Add(new ToolStripMenuItem("Save as XML...", null, (_, _) => Save(GridSerializer.XmlExtension)));
            _saveButton.DropDownItems.Add(new ToolStripMenuItem("Save as Compressed XML...", null, (_, _) => Save(GridSerializer.CompressedXmlExtension)));
            _clearButton = new ToolStripButton("Clear") { Image = Properties.Resources.Eraser_16x, Enabled = false };

            _toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };
            _toolbar.Items.AddRange(new ToolStripItem[]
            {
                _openButton, new ToolStripSeparator(), _saveButton, new ToolStripSeparator(), _clearButton
            });

            _status = new ToolStripStatusLabel();
            _statusStrip = new StatusStrip();
            _statusStrip.Items.Add(_status);

            _results = new XEResultsControl { Dock = DockStyle.Fill };

            Controls.Add(_results);
            Controls.Add(_statusStrip);
            Controls.Add(_toolbar);

            _openButton.Click += async (_, _) => await OpenAsync();
            _clearButton.Click += (_, _) => ClearGrid();

            this.ApplyTheme();
        }

        /// <summary>Prompts for a file (or opens <paramref name="path"/> directly when supplied) and loads it.</summary>
        public async Task OpenAsync(string path = null)
        {
            if (_loading) return;
            if (string.IsNullOrEmpty(path))
            {
                using var dlg = new OpenFileDialog { Filter = XEFileLoader.OpenFilter };
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                path = dlg.FileName;
            }

            _loading = true;
            _openButton.Enabled = false;
            SetStatus($"Loading {Path.GetFileName(path)}...", DashColors.Information);
            try
            {
                var result = await XEFileLoader.LoadAsync(path);
                if (result.Table == null || result.Table.Rows.Count == 0)
                {
                    _results.Clear();
                    UpdateButtons();
                    SetStatus($"No events in {Path.GetFileName(path)}.", DashColors.Warning);
                    return;
                }
                _results.LoadEvents(result.Table, convertTimestampToLocal: result.TimestampsAreUtc, takeOwnership: true);
                UpdateButtons();
                SetStatus($"{_results.RowCount:N0} event(s) loaded from {Path.GetFileName(path)}.", DashColors.Success);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, DashColors.Fail);
                if (!IsDisposed)
                {
                    MessageBox.Show(this, ex.Message, "Open XE File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                _loading = false;
                if (!IsDisposed) _openButton.Enabled = true;
            }
        }

        private void Save(string extension)
        {
            var events = _results.CurrentEvents;
            if (events == null || events.Rows.Count == 0)
            {
                SetStatus("Nothing to save - open a file first.", DashColors.Warning);
                return;
            }
            using var dlg = new SaveFileDialog
            {
                Filter = GridSerializer.SaveFilter,
                FilterIndex = GridSerializer.SaveFilterIndex(extension),
                FileName = "XETrace" + extension
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                GridSerializer.SaveDataTable(events, dlg.FileName);
                SetStatus($"Saved {events.Rows.Count:N0} event(s) to {Path.GetFileName(dlg.FileName)}.", DashColors.Success);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, DashColors.Fail);
                MessageBox.Show(this, ex.Message, "Save XE File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ClearGrid()
        {
            _results.Clear();
            UpdateButtons();
            SetStatus(string.Empty, DashColors.Information);
        }

        private void UpdateButtons()
        {
            var hasData = _results.RowCount > 0;
            _saveButton.Enabled = hasData;
            _clearButton.Enabled = hasData;
        }

        private void SetStatus(string message, Color color)
        {
            if (_statusStrip.InvokeRequired)
            {
                _statusStrip.Invoke(new Action(() => SetStatus(message, color)));
                return;
            }
            _status.Text = message;
            _status.ForeColor = color;
        }
    }
}
