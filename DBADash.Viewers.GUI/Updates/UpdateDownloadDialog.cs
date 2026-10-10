using Velopack;

namespace DBADashGUI.Viewers
{
    /// <summary>
    /// Downloads an update for a copy installed by its setup program, with a progress bar the user can cancel.  Used by the
    /// Visualizer and the DBA Dash GUI, which differ in which version they want and in what they do once it's downloaded.
    /// </summary>
    public static class UpdateDownloadDialog
    {
        /// <summary>
        /// True when the update was downloaded.  False when the user cancelled, or it failed - then <paramref name="failure"/>
        /// says why, for the caller to report in its own words.
        /// </summary>
        public static bool Download(IWin32Window owner, UpdateManager manager, UpdateInfo info, string caption, string heading,
            string text, out Exception failure)
        {
            var cancel = TaskDialogButton.Cancel;
            var bar = new TaskDialogProgressBar { Minimum = 0, Maximum = 100 };
            using var cancellation = new CancellationTokenSource();
            var completed = false;
            Exception error = null;
            var ui = SynchronizationContext.Current;

            var page = new TaskDialogPage
            {
                Caption = caption,
                Heading = heading,
                Text = text,
                ProgressBar = bar,
                Buttons = { cancel },
                AllowCancel = true
            };

            cancel.Click += (_, _) => cancellation.Cancel();
            page.Created += async (_, _) =>
            {
                try
                {
                    // Progress is reported from the download's thread, and the dialog belongs to this one.
                    await manager.DownloadUpdatesAsync(info,
                        percent => ui?.Post(_ => bar.Value = Math.Clamp(percent, 0, 100), null),
                        cancelToken: cancellation.Token);
                    completed = true;
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    // Closes the dialog: the button's own answer isn't wanted.
                    if (!cancellation.IsCancellationRequested) cancel.PerformClick();
                }
            };

            if (owner != null)
            {
                TaskDialog.ShowDialog(owner, page);
            }
            else
            {
                TaskDialog.ShowDialog(page);
            }

            failure = error;
            return completed && error == null;
        }
    }
}
