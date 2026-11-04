using DBADash.JobSchedules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AgentJobs
{
    /// <summary>
    /// Suggest Start Times in the plan editor: moves the previewed jobs (other than those kept where they are) so their
    /// runs overlap as little as possible, within the furthest move and with the gap chosen.  The moves land in the
    /// preview like any other, to adjust or reset.
    /// </summary>
    internal sealed partial class SchedulePlanEditor
    {
        /// <summary>The grid's notes about each job, before any from a suggestion</summary>
        private readonly Dictionary<Guid, string> _baseNotes = new();

        /// <summary>Starts each job loses over the range to its own schedules, moved so they clash (<see cref="SchedulePlan.ClashesAdded"/>) - jobs that lose none aren't in it</summary>
        private Dictionary<Guid, int> _clashes = new();

        /// <summary>From the last suggestion: why jobs couldn't be kept clear, and what might help</summary>
        private IReadOnlyDictionary<Guid, IReadOnlyList<string>> _suggestionNotes;

        private string _suggestionText;
        private int _maxShiftSec = 3600;
        private int _gapSec;

        /// <summary>
        /// Moving a job that shares a schedule gives it its own copy (rather than moving every job on the schedule) - when it's
        /// dragged or a move typed, and in a suggestion.  On by default, with a warning when it does.  The Own Schedule
        /// column says which jobs have a copy, and unticking it puts a job back on the shared schedule.
        /// </summary>
        private bool _splitShared = true;

        /// <summary>
        /// Dragging a run of a job with several schedules that move moves all of them (as typing a move does), rather than
        /// just the schedule that started the run.  Off by default: a clash is usually at one time of day, and moving the
        /// job's other schedules with it can make new ones.
        /// </summary>
        private bool _dragWholeJob;

        private ToolStripSplitButton _suggest;

        /// <summary>Stops the suggestion being worked out.  Null when none is.</summary>
        private CancellationTokenSource _suggestCancel;

        private const string SuggestText = "Suggest Start Times";

        /// <summary>
        /// The step a suggestion moves jobs by.  Each step either way is a move to try for every job, every pass - a second
        /// would be thousands of them.
        /// </summary>
        private const int SuggestStepSec = 60;

        private ToolStripItem[] CreateSuggestItems()
        {
            _suggest = new ToolStripSplitButton(SuggestText, Properties.Resources.Lightbulb_16x)
            {
                ToolTipText = "Move the previewed jobs so their runs overlap as little as possible, moving them as little as possible, in whole minutes (seconds for jobs that run every few minutes or more often).  " +
                              "Jobs ticked Keep stay where they are.  Click again to stop.  The arrow has how far jobs may move and the gap to leave between them."
            };
            _suggest.ButtonClick += async (_, _) => await SuggestAsync();
            var maxItem = new ToolStripMenuItem { ToolTipText = "The furthest a job may be moved either way" };
            FillChoices(maxItem, "Move up to", _maxShiftSec, v => _maxShiftSec = v,
                (300, "5 minutes"), (900, "15 minutes"), (3600, "1 hour"), (4 * 3600, "4 hours"));
            var gapItem = new ToolStripMenuItem { ToolTipText = "Time to leave after a run before another job starts" };
            FillChoices(gapItem, "Gap", _gapSec, v => _gapSec = v,
                (0, "None"), (30, "30 seconds"), (60, "1 minute"), (300, "5 minutes"));
            _suggest.DropDownItems.AddRange(new ToolStripItem[] { maxItem, gapItem });
            var sharedButton = new ToolStripDropDownButton
            {
                ToolTipText = "Jobs that share a schedule start together, and moving the schedule moves them all.  " +
                              "Give Own Copy moves such a job on its own, with a copy of the schedule - when you drag it or type a move, and in a suggestion where that reduces the overlap.  " +
                              "Move All moves every job on the schedule.  The Own Schedule column shows, and changes, which jobs have a copy."
            };
            FillChoices(sharedButton, "Moving Shared Jobs", _splitShared, v => _splitShared = v, (true, "Give Own Copy"), (false, "Move All"));
            var runButton = new ToolStripDropDownButton
            {
                ToolTipText = "For a job with several schedules: Its Schedule moves just the schedule that started the run you drag, leaving the job's other schedules where they are.  " +
                              "Whole Job moves all of them by the same amount.  Typing in Move By, and Suggest Start Times, move the whole job."
            };
            FillChoices(runButton, "Moving a Run", _dragWholeJob, v =>
            {
                _dragWholeJob = v;
                RefreshWhatIf();
            }, (false, "Its Schedule"), (true, "Whole Job"));
            return new ToolStripItem[] { new ToolStripSeparator(), _suggest, sharedButton, runButton };
        }

        private void ClearSuggestion()
        {
            _suggestionNotes = null;
            _suggestionText = null;
        }

        private string JobNote(Guid jobId)
        {
            var notes = new List<string>();
            if (_baseNotes.TryGetValue(jobId, out var note) && note != null) notes.Add(note);
            if (_plan.OwnSchedule.Contains(jobId)) notes.Add("Gets its own copy of the schedule, to move on its own");
            if (_clashes.TryGetValue(jobId, out var clashes)) notes.Add($"Its schedules are moved so they clash: Agent would skip {clashes:N0} {(clashes == 1 ? "start" : "starts")} over the range as the job is still running from another of its schedules");
            if (_suggestionNotes != null && _suggestionNotes.TryGetValue(jobId, out var suggested)) notes.AddRange(suggested);
            return notes.Count == 0 ? null : string.Join(".  ", notes);
        }

        private async Task SuggestAsync()
        {
            if (_suggestCancel != null)
            {
                _suggestCancel.Cancel();
                return;
            }
            if (_plan.IncludedJobs.Count < 2)
            {
                _whatIfWarning = "Tick at least two jobs to preview, then suggest start times for them.";
                RefreshWhatIf();
                return;
            }
            var options = new ScheduleOptimizerOptions
            {
                From = DateTime.UtcNow.AddMinutes(-_whatIfUtcOffset).Date,
                MaxShiftSec = _maxShiftSec,
                StepSec = SuggestStepSec,
                MinGapSec = _gapSec,
                SplitSharedSchedules = _splitShared
            };

            using var cancel = new CancellationTokenSource();
            _suggestCancel = cancel;
            _suggest.Text = "Stop Suggesting";
            UseWaitCursor = true;
            // The suggestion is worked out from the plan as it is now: the moves, the jobs previewed and kept, the data
            var started = (Moves: _plan.Snapshot(), Included: _plan.Included.ToHashSet(), Pinned: _plan.Pinned.ToHashSet(), Jobs: _plan.AllJobs);
            try
            {
                var suggestion = await _plan.SuggestAsync(options, cancel.Token);
                if (!started.Moves.SameAs(_plan.Snapshot()) || !started.Included.SetEquals(_plan.Included) || !started.Pinned.SetEquals(_plan.Pinned) || !ReferenceEquals(started.Jobs, _plan.AllJobs))
                {
                    // Applied to a plan that's changed since, it would move jobs from the wrong place - or a job since kept
                    _whatIfWarning = "The plan changed while start times were being suggested, so nothing was moved.  Suggest again for the plan as it is now.";
                }
                else
                {
                    ApplySuggestion(suggestion);
                    _whatIfWarning = null;
                }
            }
            catch (OperationCanceledException)
            {
                _whatIfWarning = "Stopped suggesting start times: nothing was moved.";
            }
            catch (Exception ex)
            {
                _whatIfWarning = "Couldn't suggest start times: " + ex.Message;
            }
            finally
            {
                _suggestCancel = null;
                UseWaitCursor = false;
                _suggest.Text = SuggestText;
            }
            RefreshWhatIf();
        }

        /// <summary>
        /// Make the suggested moves, and say what they gain - only when they reduce the overlap, so a suggestion never
        /// changes a schedule for nothing.  With copies of shared schedules not allowed, says what allowing them would gain.
        /// </summary>
        private void ApplySuggestion(PlanSuggestion suggestion)
        {
            var (result, alternative) = (suggestion.Result, suggestion.Alternative);
            var period = result.Period.TotalDays >= 7 ? "a week" : "a day";
            var gap = _gapSec > 0 ? $", counting a {ScheduleMath.Span(_gapSec)} gap after each run" : "";
            var notes = result.Notes.ToDictionary(n => n.Key, n => n.Value.ToList());
            var text = new StringBuilder();

            if (result.ConflictSecBefore == 0)
            {
                text.Append($"These jobs aren't expected to run into each other over {period}{gap}.  Nothing to move.");
            }
            else if (!result.HasBenefit)
            {
                text.Append($"No moves found that reduce the {Format(result.ConflictSecBefore)} these jobs run into each other over {period}{gap}, so nothing was moved.");
            }
            else
            {
                // One step to undo, keeping the suggestion's text and notes
                var before = _plan.Snapshot();
                var applied = _plan.Apply(suggestion);
                RecordUndo(before);
                foreach (var id in applied.Changed)
                {
                    var move = result.Shifts.TryGetValue(id, out var delta) ? $"Suggested move {ScheduleMath.Shift(delta)}" : "Suggested staying where it is";
                    var note = $"{move}: runs into other jobs for {Format(result.JobConflictSecAfter[id])} instead of {Format(result.JobConflictSecBefore[id])}";
                    if (!notes.TryGetValue(id, out var list)) notes[id] = list = new List<string>();
                    list.Insert(0, note);
                }
                var copies = applied.OwnSchedule.Count;
                var less = (result.ConflictSecBefore - result.ConflictSecAfter) / result.ConflictSecBefore;
                text.Append($"Suggested changes for {Jobs(applied.Changed.Count)}" + (copies > 0 ? $" (giving {OwnCopy(copies)} of a shared schedule)" : "") +
                            $": over {period} they'd run into each other for {Format(result.ConflictSecAfter)} instead of {Format(result.ConflictSecBefore)}, {less:P0} less{gap}.");
                if (applied.Refused.Count > 0) text.Append($"  Not moved: {string.Join(", ", applied.Refused)}.");
            }

            if (result.SharedConflictSecAfter > 0)
            {
                text.Append($"  {Format(result.SharedConflictSecAfter)} of that is jobs sharing a schedule");
                text.Append(alternative != null && alternative.HasBenefit && alternative.ConflictSecAfter < result.ConflictSecAfter - 0.001
                    ? $": giving {OwnCopy(alternative.OwnSchedule.Count)} would bring it to {Format(alternative.ConflictSecAfter)} (Moving Shared Jobs: Give Own Copy)."
                    : ", and giving them their own copies wouldn't help.");
            }
            else if (result.ConflictSecAfter > 0 && result.Notes.Count > 0)
            {
                text.Append("  See Note for jobs start times can't keep apart.");
            }
            if (result.Ignored.Count > 0)
            {
                text.Append($"  Left out as running nearly all the time: {string.Join(", ", suggestion.Included.Where(j => result.Ignored.Contains(j.JobId)).Select(j => j.Name))}.");
            }
            _suggestionNotes = notes.ToDictionary(n => n.Key, n => (IReadOnlyList<string>)n.Value);
            _suggestionText = text.ToString();
        }

        private static string Jobs(int count) => count == 1 ? "1 job" : $"{count} jobs";

        private static string OwnCopy(int count) => count == 1 ? "1 job its own copy" : $"{count} jobs their own copies";

        private static string Format(double seconds) => seconds == 0 ? "none" : JobTimelineChart.FormatDuration(TimeSpan.FromSeconds(seconds));
    }
}
