using DBADash;
using DBADash.Messaging;
using DBADashGUI.Messaging;
using DBADashGUI.Theme;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.CollectionDates
{
    /// <summary>
    /// Pause all scheduled collections for one or more instances for a period, or resume them.  Sent to the collecting
    /// service(s) as a <see cref="CollectionPauseMessage"/>.  Alerts are muted for the instances while paused.
    /// </summary>
    public partial class PauseCollections : Form
    {
        public PauseCollections()
        {
            InitializeComponent();
        }

        private static readonly (string Name, TimeSpan Duration)[] Durations =
        {
            ("15 minutes", TimeSpan.FromMinutes(15)),
            ("30 minutes", TimeSpan.FromMinutes(30)),
            ("1 hour", TimeSpan.FromHours(1)),
            ("2 hours", TimeSpan.FromHours(2)),
            ("4 hours", TimeSpan.FromHours(4)),
            ("8 hours", TimeSpan.FromHours(8)),
            ("12 hours", TimeSpan.FromHours(12)),
            ("1 day", TimeSpan.FromDays(1)),
            ("2 days", TimeSpan.FromDays(2)),
            ("7 days", TimeSpan.FromDays(7))
        };

        private const int DefaultDurationIndex = 2; // 1 hour

        /// <summary>
        /// Raised when collections are paused or resumed from the GUI, with the instances the services accepted and the
        /// new pause end (UTC, null when resumed), so views showing the pause - the tree - can update without a refresh.
        /// </summary>
        public static event Action<IReadOnlyCollection<int>, DateTime?> PauseChanged;

        /// <summary>A pause end (UTC) for display: the time alone if it's today, otherwise the date and time.</summary>
        public static string FormatPausedUntil(DateTime pausedUntilUtc)
        {
            var local = pausedUntilUtc.ToAppTimeZone();
            return local.Date == DateHelper.AppNow.Date ? local.ToString("HH:mm") : local.ToString("yyyy-MM-dd HH:mm");
        }

        /// <summary>Tooltip text explaining a pause.</summary>
        public static string PausedToolTip(DateTime pausedUntilUtc) =>
            $"Scheduled collections are paused until {pausedUntilUtc.ToAppTimeZone():yyyy-MM-dd HH:mm}.  Alerts are muted and collections aren't reported as overdue until then.";

        /// <summary>Instances to pause, from dbo.CollectionsPaused_Get (pause times converted to the app time zone).</summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<DataRow> Instances { get; set; } = new();

        /// <summary>True if the user chose to resume collections rather than pause them.</summary>
        public bool Resume { get; private set; }

        /// <summary>
        /// When the pause ends (UTC), or null if the time chosen doesn't exist in the app time zone (skipped when the
        /// clocks go forward).  Only meaningful when <see cref="Resume"/> is false.
        /// </summary>
        public DateTime? PauseUntilUtc => rbFor.Checked
            ? DateTime.UtcNow.Add(Durations[cboDuration.SelectedIndex].Duration)
            : dtpUntil.Value.TryAppTimeZoneToUtc(out var utc) ? utc : null;

        private void PauseCollections_Load(object sender, EventArgs e)
        {
            cboDuration.Items.AddRange(Durations.Select(d => (object)d.Name).ToArray());
            cboDuration.SelectedIndex = DefaultDurationIndex;
            dtpUntil.Value = DateHelper.AppNow.AddHours(1);

            var names = Instances.Select(r => r.Field<string>("InstanceDisplayName")).ToList();
            var paused = Instances.Where(r => r["CollectionsPausedUntil"] != DBNull.Value).ToList();
            var text = "Instances: " + (names.Count <= 5 ? string.Join(", ", names) : string.Join(", ", names.Take(5)) + $" and {names.Count - 5} more");
            if (paused.Count == 1 && Instances.Count == 1)
            {
                text += $"\n\nPaused until {paused[0].Field<DateTime>("CollectionsPausedUntil"):yyyy-MM-dd HH:mm}";
            }
            else if (paused.Count > 0)
            {
                text += $"\n\n{paused.Count} of {Instances.Count} instances are paused";
            }
            lblInstances.Text = text;
            // Resume is always available: the repository can lag the service (e.g. the pause hasn't been reported or
            // imported yet), and resuming an instance that isn't paused does nothing
            UpdateControls();
            this.ApplyTheme();
        }

        private void UpdateControls()
        {
            cboDuration.Enabled = rbFor.Checked;
            dtpUntil.Enabled = rbUntil.Checked;
            if (cboDuration.SelectedIndex < 0) return;
            var duration = PauseUntilUtc - DateTime.UtcNow;
            bttnPause.Enabled = duration > TimeSpan.Zero && duration <= CollectionPauseMessage.MaxPause; // False for a time that doesn't exist
        }

        private void Pause_Changed(object sender, EventArgs e) => UpdateControls();

        private void BttnPause_Click(object sender, EventArgs e)
        {
            Resume = false;
            DialogResult = DialogResult.OK;
        }

        private void BttnResume_Click(object sender, EventArgs e)
        {
            Resume = true;
            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Prompts to pause or resume collections for <paramref name="instanceIDs"/> and sends the change to the services
        /// that collect them - one message per service.  Instances whose service doesn't allow schedule changes are
        /// skipped.  Returns null if nothing was sent, otherwise the outcome to show the user.
        /// </summary>
        internal static async Task<ScheduleActionResult> PauseOrResumeAsync(IWin32Window owner, IEnumerable<int> instanceIDs,
            MessagingHelper.SetStatusDelegate setStatus)
        {
            if (!DBADashUser.AllowScheduleChanges)
            {
                MessageBox.Show(owner, "You don't have permission to pause collections.  Membership of the AllowScheduleChanges role is required.",
                    "Pause Collections", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            var instances = GetInstances(instanceIDs);
            var eligible = instances.Where(r => r.Field<bool>("ScheduleChangesEnabled")).ToList();
            if (eligible.Count == 0)
            {
                MessageBox.Show(owner, "Collections can't be paused for the selected instance(s).\n\nSchedule changes must be allowed in the service configuration tool for the service that collects the instance.",
                    "Pause Collections", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            using var frm = new PauseCollections { Instances = eligible };
            if (frm.ShowDialog(owner) != DialogResult.OK) return null;
            var pauseUntilUtc = frm.Resume ? null : frm.PauseUntilUtc;
            var action = frm.Resume ? "resume" : "pause";

            var failed = 0;
            string lastError = null;
            var warnings = new List<string>();
            var changedInstanceIDs = new List<int>();
            var groups = eligible.GroupBy(r => r.Field<int>("CollectAgentID")).ToList();
            foreach (var group in groups)
            {
                var row = group.First();
                var importAgentID = row.Field<int>("ImportAgentID");
                var message = new CollectionPauseMessage
                {
                    ConnectionIDs = group.Select(r => r.Field<string>("ConnectionID")).ToList(),
                    PauseUntilUtc = pauseUntilUtc,
                    CollectAgent = DBADashAgent.GetDBADashAgent(Common.ConnectionString, group.Key),
                    ImportAgent = DBADashAgent.GetDBADashAgent(Common.ConnectionString, importAgentID)
                };
                setStatus($"Sending {action} request...", null, DashColors.Information);
                var outcome = await UserActionLog.SendLoggedAsync(message, importAgentID, group.Key,
                    group.Select(r => r.Field<int>("InstanceID")),
                    frm.Resume ? UserActionLog.ActionResume : UserActionLog.ActionPause,
                    pauseUntilUtc.HasValue ? new { PauseUntilUtc = pauseUntilUtc } : null, setStatus);
                if (outcome.Succeeded)
                {
                    changedInstanceIDs.AddRange(group.Select(r => r.Field<int>("InstanceID")));
                    warnings.AddRange(outcome.Warnings);
                }
                else
                {
                    failed++;
                    lastError = outcome.Error;
                }
            }
            if (changedInstanceIDs.Count > 0)
            {
                PauseChanged?.Invoke(changedInstanceIDs, pauseUntilUtc);
            }

            var skipped = instances.Count - eligible.Count;
            var skippedText = skipped > 0 ? $"  {skipped} instance(s) skipped as their service doesn't allow schedule changes." : string.Empty;
            var warningText = warnings.Count > 0 ? "  Warning: " + string.Join("  ", warnings) : string.Empty;
            if (failed > 0)
            {
                return new ScheduleActionResult(false, $"Failed to {action} collections for {failed} of {groups.Count} service(s): {lastError}{skippedText}{warningText}");
            }
            var done = frm.Resume
                ? "Collections resumed"
                : $"Collections paused until {FormatPausedUntil(pauseUntilUtc!.Value)}";
            return new ScheduleActionResult(true, $"{done} for {eligible.Count} instance(s).{skippedText}{warningText}", IsWarning: warnings.Count > 0);
        }

        /// <summary>Instances with what's needed to send a schedule change or pause to their collecting service (dbo.CollectionsPaused_Get).</summary>
        internal static List<DataRow> GetInstances(IEnumerable<int> instanceIDs)
        {
            using var cn = new SqlConnection(Common.ConnectionString);
            using var cmd = new SqlCommand("dbo.CollectionsPaused_Get", cn) { CommandType = CommandType.StoredProcedure };
            using var da = new SqlDataAdapter(cmd);
            cmd.Parameters.AddWithValue("InstanceIDs", string.Join(",", instanceIDs.Distinct()));
            var dt = new DataTable();
            da.Fill(dt);
            DateHelper.ConvertUTCToAppTimeZone(ref dt);
            return dt.Rows.Cast<DataRow>().ToList();
        }
    }
}
