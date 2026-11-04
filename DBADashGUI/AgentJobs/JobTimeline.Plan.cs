using DBADashGUI.Theme;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    /// <summary>
    /// Plan mode: plan moves to the job schedules on the timeline itself.  The chart shows each job's runs in the period
    /// (one bar per run, not merged), moved as planned, with overlaps in purple; the plan editor below it has the jobs, the
    /// moves, Suggest Start Times and Script Changes.  For one instance, over up to <see cref="MaxPlanDays"/> days - the
    /// last of a longer period.
    /// </summary>
    public partial class JobTimeline
    {
        /// <summary>Plan mode works on every run, so it's kept to a period that stays a sensible size</summary>
        private const int MaxPlanDays = 7;

        /// <summary>
        /// <see cref="MaxPlanDays"/> with an hour to spare: a preset period over 2 hours starts on the hour (DateRange.FromUTC
        /// rounds down) and ends now, so "7 days" is up to 7 days and 59 minutes
        /// </summary>
        private static readonly TimeSpan MaxPlanPeriod = TimeSpan.FromDays(MaxPlanDays) + TimeSpan.FromHours(1);

        private ToolStripButton tsPlan;
        private SchedulePlanEditor planEditor;
        private Splitter planSplitter;
        private ScheduleOverlapData planData;
        private int planGeneration;

        /// <summary>Set while switching plan mode off from code, so the button's change doesn't do it twice</summary>
        private bool settingPlanMode;

        private bool PlanMode => tsPlan?.Checked == true;

        /// <summary>Toolbar items that don't apply to plan mode</summary>
        private ToolStripItem[] NotInPlanMode => new ToolStripItem[] { tsIncludeSteps, tsDateGroup, tsConcurrency, tsCopyDropDown, tsProblemsOnly };

        /// <summary>The Plan button, at <paramref name="index"/> on the toolbar</summary>
        private void AddPlanItems(int index)
        {
            tsPlan = new ToolStripButton("Plan", Properties.Resources.Lightbulb_16x)
            {
                CheckOnClick = true,
                ToolTipText = $"Plan changes to the job schedules here: see where jobs overlap, move jobs (drag their runs), suggest start times and script the changes.  One instance, up to {MaxPlanDays} days.  Nothing changes on the server."
            };
            tsPlan.CheckedChanged += (_, _) =>
            {
                if (!settingPlanMode) SetPlanMode(tsPlan.Checked);
            };
            toolStrip1.Items.Insert(index, tsPlan);
        }

        /// <summary>Why plan mode isn't available for what's shown, or null when it is</summary>
        private string PlanUnavailableReason() =>
            context == null ? "Nothing loaded"
            : MultiInstance ? "Plan mode is for one instance - select an instance"
            : context.JobID != Guid.Empty ? "Plan mode is for an instance's jobs - select the instance rather than a job"
            : null;

        /// <summary>The period plan mode works on: the selected one, or the last <see cref="MaxPlanDays"/> days of a longer one</summary>
        private (DateTime FromUtc, DateTime ToUtc, bool Clamped) PlanPeriodUtc()
        {
            var toUtc = ToDateUtc;
            return toUtc - FromDateUtc > MaxPlanPeriod ? (toUtc.AddDays(-MaxPlanDays), toUtc, true) : (FromDateUtc, toUtc, false);
        }

        private void SetPlanMode(bool on)
        {
            if (on)
            {
                var reason = PlanUnavailableReason();
                if (reason != null)
                {
                    ExitPlanMode(reason);
                    return;
                }
                EnterPlanMode();
            }
            else
            {
                // The moves are kept for when plan mode is back on, for the same instance
                ExitPlanMode(null);
            }
        }

        private void EnterPlanMode()
        {
            if (planEditor == null)
            {
                planEditor = new SchedulePlanEditor
                {
                    Dock = DockStyle.Bottom,
                    Height = LogicalToDeviceUnits(380),
                    ShowAppTimeZone = true,
                    ShowBasisChoice = true
                };
                planEditor.BasisChanged += (_, _) => AnalyzePlan();
                // The runs filter on the toolbar says which jobs the plan lists
                planEditor.DurationFilterChanged += (_, _) => ShowRunFilters();
                planSplitter = new Splitter { Dock = DockStyle.Bottom, Height = LogicalToDeviceUnits(5) };
                // Docked last, so it docks first: the editor at the very bottom, the splitter above it
                Controls.Add(planSplitter);
                Controls.Add(planEditor);
                planEditor.ApplyTheme();
            }
            planEditor.Visible = true;
            planSplitter.Visible = true;
            foreach (var item in NotInPlanMode) item.Enabled = false;
            planEditor.MinDurationSec = minDurationSec;
            ShowRunFilters();
            // A concurrency band still loading for the timeline would replace the plan's
            bandGeneration++;
            chart.Banner = null;
            planEditor.Attach(chart);
            LoadPlan();
        }

        /// <summary>Back to the timeline as it was.  <paramref name="reason"/> says why, when plan mode couldn't stay on.</summary>
        /// <param name="reload">Load the timeline afresh - not when the caller is about to</param>
        private void ExitPlanMode(string reason, bool reload = true)
        {
            planGeneration++;
            if (tsPlan.Checked)
            {
                settingPlanMode = true;
                tsPlan.Checked = false;
                settingPlanMode = false;
            }
            if (planEditor != null)
            {
                planEditor.Detach();
                planEditor.Visible = false;
                planSplitter.Visible = false;
            }
            foreach (var item in NotInPlanMode) item.Enabled = true;
            ShowRunFilters();
            chart.ShowConcurrency = tsConcurrency.Checked && context?.JobID == Guid.Empty;
            if (reload && context != null)
            {
                // The period or category may have changed while planning - load the timeline afresh
                resetView = true;
                RefreshData();
            }
            if (reason != null) MessageBox.Show(reason, "Plan", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Forget the plan - for a different instance it means nothing.  The moves planned can still be scripted first, as
        /// moving to another node can't wait for an answer.  Leaves loading the timeline for the new context to the caller.
        /// </summary>
        private void DiscardPlan()
        {
            if (planEditor is { PendingChanges: > 0 } editor)
            {
                var changes = editor.PendingChanges;
                if (MessageBox.Show($"The {(changes == 1 ? "change" : $"{changes} changes")} planned for {editor.InstanceName} will be discarded.  Script {(changes == 1 ? "it" : "them")} first?",
                        "Plan", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    editor.ScriptChanges();
                }
            }
            if (PlanMode) ExitPlanMode(null, reload: false);
            if (planEditor == null) return;
            Controls.Remove(planEditor);
            Controls.Remove(planSplitter);
            planEditor.Dispose();
            planSplitter.Dispose();
            planEditor = null;
            planSplitter = null;
            planData = null;
        }

        /// <summary>Load the instance's jobs, schedules and runs for the period, keeping the plan's moves</summary>
        private async void LoadPlan()
        {
            var reason = PlanUnavailableReason();
            if (reason != null)
            {
                ExitPlanMode(reason);
                return;
            }
            var generation = ++planGeneration;
            var instanceId = InstanceId;
            var (fromUtc, toUtc, clamped) = PlanPeriodUtc();
            var category = selectedCategory;
            planEditor.ShowMessage("Loading the job schedules...");
            planEditor.PeriodNote = clamped ? $"The last {MaxPlanDays} days of the period selected - plan mode works on up to {MaxPlanDays} days." : null;
            chart.Banner = "Loading the plan...";
            try
            {
                // Analysed there too: a busy week is hundreds of thousands of runs, seconds of work that froze the window
                var basis = planEditor.Basis;
                var (data, analysis) = await Task.Run(() =>
                {
                    var loaded = ScheduleOverlapData.Load(instanceId, fromUtc, toUtc, category);
                    return (loaded, loaded.Analyze(basis));
                });
                if (generation != planGeneration || !PlanMode) return;
                planData = data;
                AnalyzePlan(analysis);
            }
            catch (Exception ex)
            {
                if (generation != planGeneration) return;
                planEditor.ShowMessage("Couldn't load the job schedules: " + ex.Message, error: true);
                chart.Banner = "Couldn't load the plan";
            }
        }

        /// <param name="analysis">The data analysed already, with the editor's duration basis - analysed here when null</param>
        private void AnalyzePlan(ScheduleAnalysis analysis = null)
        {
            if (planData == null || !PlanMode) return;
            chart.Banner = null;
            planEditor.SetData(analysis ?? planData.Analyze(planEditor.Basis), InstanceId, context.InstanceName);
            // The plan's period, and the CPU across it
            FetchCpuForView(true);
        }

        /// <summary>Undo and redo in plan mode, wherever the focus is on the timeline - the chart, after a drag, as much as the editor</summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Text being typed keeps its own undo
            if (PlanMode && planEditor != null && FromHandle(msg.HWnd) is not (TextBoxBase or ComboBox) && planEditor.ProcessUndoKey(keyData)) return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
