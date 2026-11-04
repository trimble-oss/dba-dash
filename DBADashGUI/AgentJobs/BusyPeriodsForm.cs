using DBADashGUI.Performance;
using DBADashGUI.Theme;
using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    /// <summary>
    /// Lists the busy periods for an instance - its own, its tags' and all instances' - says which level applies, and adds,
    /// edits, deletes or copies them.  Opened from the job timeline's Busy Hours button and plan mode's toolbar.
    /// </summary>
    internal sealed class BusyPeriodsForm : Form
    {
        private readonly int _instanceId;
        private readonly string _instanceName;
        private readonly Label _applies = new() { Dock = DockStyle.Top, AutoSize = false, Height = 44, Padding = new Padding(8, 6, 8, 4) };
        private readonly DataGridView _grid = new()
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
        };
        private readonly Button _edit = new() { Text = "Edit...", AutoSize = true };
        private readonly Button _delete = new() { Text = "Delete", AutoSize = true };
        private readonly CheckBox _showAll = new() { Text = "Show all configured periods, including other instances' and tags'", AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(8, 0, 8, 4) };
        private DataTable _periods;

        /// <summary>Periods were added, changed, deleted or copied - the host loads them again</summary>
        public bool Changed { get; private set; }

        public BusyPeriodsForm(int instanceId, string instanceName)
        {
            _instanceId = instanceId;
            _instanceName = instanceName;
            Text = "Busy Hours - " + instanceName;
            Size = new Size(820, 420);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var add = new Button { Text = "Add...", AutoSize = true };
            var copy = new Button { Text = "Copy from instance...", AutoSize = true };
            var close = new Button { Text = "Close", AutoSize = true, DialogResult = DialogResult.OK };
            add.Click += async (_, _) => await EditPeriod(new BusyPeriod
            {
                InstanceID = _instanceId,
                TimeZone = DateHelper.AppTimeZone,
                Saturday = false,
                Sunday = false,
                TimeFrom = new TimeOnly(8, 0),
                TimeTo = new TimeOnly(18, 0)
            });
            _edit.Click += async (_, _) => await EditSelected();
            _delete.Click += async (_, _) => await DeleteSelected();
            copy.Click += async (_, _) => await CopyFromInstance();
            _grid.CellDoubleClick += async (_, e) =>
            {
                if (e.RowIndex >= 0) await EditSelected();
            };
            _grid.SelectionChanged += (_, _) => _edit.Enabled = _delete.Enabled = Selected != null;

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(4) };
            buttons.Controls.AddRange(new Control[] { add, _edit, _delete, copy, close });
            _showAll.CheckedChanged += (_, _) => ShowPeriods();
            Controls.Add(_grid);
            Controls.Add(_showAll);
            Controls.Add(_applies);
            Controls.Add(buttons);
            AcceptButton = close;
            CancelButton = close;
            Load += async (_, _) =>
            {
                this.ApplyTheme();
                await LoadPeriods();
            };
        }

        private DataRow Selected => _grid.SelectedRows.Count == 1 && _grid.SelectedRows[0].DataBoundItem is DataRowView { Row: var row } ? row : null;

        private async Task LoadPeriods()
        {
            try
            {
                var now = DateTime.UtcNow;
                var (_, periods) = await Task.Run(() => BusyPeriodsData.Get(_instanceId, now, now));
                _periods = periods;
                ShowPeriods();
                _applies.Text = AppliesText(periods);
            }
            catch (Exception ex)
            {
                _applies.Text = "Couldn't load the busy periods: " + ex.Message;
                _applies.ForeColor = DashColors.Fail;
            }
        }

        /// <summary>The periods in the grid: those relevant to the instance, or all of them with Show all</summary>
        private void ShowPeriods()
        {
            if (_periods == null) return;
            var shown = new DataTable();
            shown.Columns.Add("BusyPeriodID", typeof(int));
            shown.Columns.Add("Applies To", typeof(string));
            shown.Columns.Add("In Use", typeof(bool));
            shown.Columns.Add("Days", typeof(string));
            shown.Columns.Add("From", typeof(string));
            shown.Columns.Add("To", typeof(string));
            shown.Columns.Add("Time Zone", typeof(string));
            shown.Columns.Add("Description", typeof(string));
            foreach (DataRow r in _periods.Rows.Cast<DataRow>().Where(r => _showAll.Checked || r.Field<bool>("Relevant")))
            {
                var period = BusyPeriod.FromRow(r, _instanceId);
                var appliesTo = period.ApplyTo switch
                {
                    BusyPeriodScope.ThisInstance => r["InstanceDisplayName"] as string ?? _instanceName,
                    BusyPeriodScope.Tag => "Tag " + period.ApplyToTag,
                    _ => "All instances"
                };
                var crossesMidnight = period.TimeTo < period.TimeFrom ? " (next day)" : "";
                shown.Rows.Add(period.BusyPeriodID, appliesTo, r.Field<bool>("Applies"), Days(period),
                    period.TimeFrom?.ToString("t", CultureInfo.CurrentCulture), period.TimeTo?.ToString("t", CultureInfo.CurrentCulture) + crossesMidnight,
                    period.TimeZone.Id, period.Description);
            }
            _grid.DataSource = shown;
            _grid.Columns["BusyPeriodID"]!.Visible = false;
            _grid.Columns["Description"]!.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            _grid.Columns["In Use"]!.ToolTipText = $"Used for {_instanceName}.  Unticked: overridden by a more specific level, or for another instance or tag.";
            _edit.Enabled = _delete.Enabled = Selected != null;
        }

        /// <summary>Which level's periods the instance uses, and that the others are listed only to edit</summary>
        private string AppliesText(DataTable periods)
        {
            var using_ = periods.Rows.Cast<DataRow>().FirstOrDefault(r => r.Field<bool>("Applies"))?.Field<string>("Source");
            var text = using_ switch
            {
                "Instance" => $"{_instanceName} uses its own busy periods.",
                "Tag" => $"{_instanceName} has no periods of its own, so it uses those for its tags.",
                "All" => $"{_instanceName} has no periods of its own or for its tags, so it uses those for all instances.",
                _ => $"No busy periods apply to {_instanceName}.  Add one - e.g. weekdays 08:00 to 18:00 - to shade it on the timeline and keep jobs out of it in plan mode."
            };
            if (periods.Rows.Cast<DataRow>().Any(r => r.Field<bool>("Relevant") && !r.Field<bool>("Applies")))
            {
                text += "  Periods not In Use are overridden by a more specific level: an instance's own periods replace its tags', which replace all instances'.";
            }
            return text;
        }

        private static string Days(BusyPeriod p)
        {
            if (p.IsEveryDay) return "Every day";
            if (p is { Monday: true, Tuesday: true, Wednesday: true, Thursday: true, Friday: true, Saturday: false, Sunday: false }) return "Weekdays";
            return string.Join(", ", p.GetDaysOfWeek().Select(d => CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames[(int)d]));
        }

        private async Task EditSelected()
        {
            if (Selected is not { } row) return;
            var id = row.Field<int>("BusyPeriodID");
            var source = _periods.Rows.Cast<DataRow>().First(r => r.Field<int>("BusyPeriodID") == id);
            await EditPeriod(BusyPeriod.FromRow(source, _instanceId));
        }

        private async Task EditPeriod(BusyPeriod period)
        {
            using var frm = new PropertyGridDialog { Text = period.BusyPeriodID.HasValue ? "Edit Busy Period" : "Add Busy Period", SelectedObject = period };
            if (frm.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                await period.Save();
                Changed = true;
                await LoadPeriods();
                // A period for another instance or tag is saved, but isn't listed for this one: show all, so it's seen
                if (!_showAll.Checked && _periods?.Rows.Cast<DataRow>().Any(r => r.Field<int>("BusyPeriodID") == period.BusyPeriodID && !r.Field<bool>("Relevant")) == true)
                {
                    _showAll.Checked = true;
                }
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error saving busy period");
            }
        }

        private async Task DeleteSelected()
        {
            if (Selected is not { } row) return;
            if (MessageBox.Show(this, "Delete this busy period?", "Busy Hours", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                await BusyPeriod.Delete(row.Field<int>("BusyPeriodID"));
                Changed = true;
                await LoadPeriods();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error deleting busy period");
            }
        }

        /// <summary>Replace this instance's own periods with copies of another instance's own periods</summary>
        private async Task CopyFromInstance()
        {
            var instances = CommonData.Instances.Rows.Cast<DataRow>()
                .Where(r => r.Field<int>("InstanceID") != _instanceId)
                .Select(r => new InstanceChoice(r.Field<int>("InstanceID"), r.Field<string>("InstanceDisplayName")))
                .OrderBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            using var pick = new Form { Text = "Copy Busy Hours", Size = new Size(420, 150), StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, FormBorderStyle = FormBorderStyle.FixedDialog, ShowInTaskbar = false };
            var label = new Label { Text = $"Copy the busy periods of (replaces {_instanceName}'s own):", Dock = DockStyle.Top, Height = 28, Padding = new Padding(8, 8, 8, 0) };
            var combo = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
            combo.Items.AddRange(instances.Cast<object>().ToArray());
            var ok = new Button { Text = "Copy", DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
            buttons.Controls.AddRange(new Control[] { cancel, ok });
            pick.Controls.Add(combo);
            pick.Controls.Add(label);
            pick.Controls.Add(buttons);
            pick.AcceptButton = ok;
            pick.CancelButton = cancel;
            pick.ApplyTheme();
            if (pick.ShowDialog(this) != DialogResult.OK || combo.SelectedItem is not InstanceChoice from) return;
            try
            {
                await BusyPeriodsData.Copy(from.InstanceId, _instanceId);
                Changed = true;
                await LoadPeriods();
            }
            catch (Exception ex)
            {
                CommonShared.ShowExceptionDialog(ex, "Error copying busy periods");
            }
        }

        private sealed record InstanceChoice(int InstanceId, string Name)
        {
            public override string ToString() => Name;
        }
    }
}
