#nullable enable
using DBADash;
using DBADashGUI.Theme;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AI
{
    /// <summary>
    /// Where a user looks after their own AI conversations: how many there are, and how to move them.
    ///
    /// Follow-up questions and the answers to them stay on the machine they were asked from, encrypted
    /// to the Windows account that asked them.  That keeps them genuinely private - nobody
    /// administering the repository can read them, because they are not in it - and the price is that
    /// they do not follow the user about.  This dialog is that price made visible and payable: export
    /// before replacing a computer, import after.
    ///
    /// Export is deliberately plain JSON unless a passphrase is asked for.  The user has asked for
    /// their own conversations in a file; one they can open, search and keep is more use than one only
    /// this application can read, and it doubles as a way to send an exchange to a colleague.
    /// </summary>
    internal sealed class AiConversationsForm : Form
    {
        private const string FileFilter =
            "DBA Dash conversations (*.json;*.dbadashchat)|*.json;*.dbadashchat|All files (*.*)|*.*";

        private readonly Label _status = new()
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            TextAlign = ContentAlignment.TopLeft
        };

        private readonly Button _export = new() { Text = "Export...", AutoSize = true, Enabled = false };
        private readonly Button _import = new() { Text = "Import...", AutoSize = true };
        private readonly Button _close = new() { Text = "Close", AutoSize = true, DialogResult = DialogResult.OK };

        private AiConversationsForm()
        {
            Text = "My AI conversations";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(580, 250);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8),
                AutoSize = true
            };

            buttons.Controls.Add(_close);
            buttons.Controls.Add(_import);
            buttons.Controls.Add(_export);

            Controls.Add(_status);
            Controls.Add(buttons);

            AcceptButton = _close;
            CancelButton = _close;

            _export.Click += async (_, _) => await ExportAsync();
            _import.Click += async (_, _) => await ImportAsync();

            this.ApplyTheme();
        }

        internal static async Task OpenAsync(IWin32Window? owner)
        {
            using var form = new AiConversationsForm();
            await form.RefreshAsync();
            form.ShowDialog(owner);
        }

        private async Task RefreshAsync()
        {
            var all = await AiLocalConversationStore.AllAsync();
            var turns = all.Sum(c => c.Turns.Count);

            _export.Enabled = all.Count > 0;

            _status.Text = all.Count == 0
                ? "You have no saved conversations yet." + Environment.NewLine + Environment.NewLine +
                  "When you ask a follow-up question about a query plan or a deadlock, the question and " +
                  "the answer are saved on this computer and shown to you again next time you open it.  " +
                  "The opening analysis is shared with everyone; your follow-ups are not." +
                  Environment.NewLine + Environment.NewLine +
                  "If you have conversations exported from another computer, import them here."
                : $"{Describe(all.Count, "conversation")} saved on this computer, {Describe(turns, "answer")} in total." +
                  Environment.NewLine + Environment.NewLine +
                  "They are encrypted to your Windows account and never leave this computer, so nobody " +
                  "administering the DBA Dash repository can read them." +
                  Environment.NewLine + Environment.NewLine +
                  "That also means they are not backed up and will not follow you to another computer.  " +
                  "Export them before this one is replaced.";
        }

        private static string Describe(int count, string noun) =>
            count == 1 ? $"1 {noun}" : $"{count:N0} {noun}s";

        private async Task ExportAsync()
        {
            var passphrase = PassphrasePrompt.Ask(this,
                "You can protect the exported file with a passphrase." + Environment.NewLine +
                Environment.NewLine +
                "Leave it blank to export readable JSON - useful if you want to search or keep the file " +
                "yourself.  Set one if the file is going somewhere you would rather it could not be read.");

            // Cancelled, as distinct from deliberately left blank.
            if (passphrase is null) return;

            using var dialog = new SaveFileDialog
            {
                Filter = FileFilter,
                FileName = passphrase.Length == 0
                    ? $"DBADash-conversations-{DateTime.Now:yyyy-MM-dd}.json"
                    : $"DBADash-conversations-{DateTime.Now:yyyy-MM-dd}.dbadashchat",
                Title = "Export my AI conversations"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                await File.WriteAllBytesAsync(dialog.FileName,
                    await AiLocalConversationStore.ExportAsync(passphrase.Length == 0 ? null : passphrase));

                MessageBox.Show(this,
                    passphrase.Length == 0
                        ? "Your conversations have been exported." + Environment.NewLine + Environment.NewLine +
                          "The file is readable by anyone who opens it - keep it somewhere you are happy for it to be."
                        : "Your conversations have been exported." + Environment.NewLine + Environment.NewLine +
                          "You will need the passphrase to import them, and there is no way to recover it.",
                    "Exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "The file could not be written: " + ex.Message,
                    "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ImportAsync()
        {
            using var dialog = new OpenFileDialog
            {
                Filter = FileFilter,
                Title = "Import AI conversations"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            byte[] file;

            try
            {
                file = await File.ReadAllBytesAsync(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "The file could not be read: " + ex.Message,
                    "Import failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Only asked for when the file turns out to want one, so importing a plain export is one
            // step rather than two.
            string? passphrase = null;

            if (DBADash.PassphraseProtection.IsWrapped(file))
            {
                passphrase = PassphrasePrompt.Ask(this, "This export is protected.  Enter its passphrase.");
                if (passphrase is null) return;
            }

            var result = await AiLocalConversationStore.ImportAsync(file, passphrase);

            if (!result.Success)
            {
                MessageBox.Show(this, result.Error, "Import failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await RefreshAsync();

            var unchanged = result.Skipped == 0
                ? string.Empty
                : $"{Environment.NewLine}{result.Skipped:N0} were already here and were left alone.";

            MessageBox.Show(this,
                $"{result.Added:N0} added, {result.Updated:N0} updated.{unchanged}" +
                Environment.NewLine + Environment.NewLine +
                "Reopen any plan or deadlock you were looking at to see them.",
                "Imported", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Asks for a passphrase.  Small enough to live here: it is only ever used by the two buttons
        /// above, and a blank one is a valid answer - it means "do not protect this file".
        /// </summary>
        private sealed class PassphrasePrompt : Form
        {
            private readonly TextBox _passphrase = new() { UseSystemPasswordChar = true, Width = 380 };

            private PassphrasePrompt(string message)
            {
                Text = "Passphrase";
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MinimizeBox = false;
                MaximizeBox = false;
                ClientSize = new Size(440, 210);

                var layout = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.TopDown,
                    Padding = new Padding(12),
                    WrapContents = false
                };

                layout.Controls.Add(new Label { Text = message, AutoSize = false, Width = 400, Height = 90 });
                layout.Controls.Add(new Label { Text = "Passphrase", AutoSize = true });
                layout.Controls.Add(_passphrase);

                var ok = new Button { Text = "OK", AutoSize = true, DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };

                var buttons = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(8),
                    AutoSize = true
                };

                buttons.Controls.Add(cancel);
                buttons.Controls.Add(ok);

                Controls.Add(layout);
                Controls.Add(buttons);

                AcceptButton = ok;
                CancelButton = cancel;

                this.ApplyTheme();
            }

            /// <summary>The passphrase, empty for "do not protect", or null when the user cancelled.</summary>
            internal static string? Ask(IWin32Window owner, string message)
            {
                using var prompt = new PassphrasePrompt(message);
                return prompt.ShowDialog(owner) == DialogResult.OK ? prompt._passphrase.Text : null;
            }
        }
    }
}
