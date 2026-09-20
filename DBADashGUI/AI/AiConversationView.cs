#nullable enable
using DBADashGUI.AgentJobs;
using DBADashSharedGUI;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DBADashGUI.AI
{
    /// <summary>
    /// A conversation on screen: everything said so far, rendered, and a box to ask the next thing.
    ///
    /// Shared by the deadlock and query plan viewers.  Both of them start the same way - an artifact
    /// goes up, an answer comes back - and the reason for a follow-up is the same in both: the answer
    /// raises a question, or answers a different one from the one the reader had.  Keeping one control
    /// means the two cannot drift into behaving differently over the same interaction.
    ///
    /// The whole conversation is re-rendered on every turn rather than appended to.  It is one
    /// Markdown document, an answer can revise what an earlier one said, and a few turns of text is
    /// nothing to render.
    /// </summary>
    internal sealed class AiConversationView : UserControl
    {
        private readonly WebView2Wrapper _rendered = new() { Dock = DockStyle.Fill, Visible = false };
        private readonly TextBox _plain;

        private readonly TextBox _question = new()
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Enabled = false
        };

        private readonly Button _ask = new()
        {
            Text = "Ask",
            Dock = DockStyle.Fill,
            Enabled = false,
            UseVisualStyleBackColor = true
        };

        private readonly Label _hint = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "Ask a follow-up question.  Ctrl+Enter sends it."
        };

        private readonly Panel _ribbon = new() { Dock = DockStyle.Bottom, Height = 96, Visible = false };

        /// <summary>Raised when the reader asks something.  The text has already been trimmed and checked for emptiness.</summary>
        internal event EventHandler<string>? QuestionAsked;

        internal AiConversationView()
        {
            _plain = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                WordWrap = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9f)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(4)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            layout.Controls.Add(_hint, 0, 0);
            layout.SetColumnSpan(_hint, 2);
            layout.Controls.Add(_question, 0, 1);
            layout.Controls.Add(_ask, 1, 1);

            _ribbon.Controls.Add(layout);

            Controls.Add(_rendered);
            Controls.Add(_plain);
            Controls.Add(_ribbon);

            _ask.Click += (_, _) => Ask();

            // Ctrl+Enter rather than Enter: the box is multi-line because a question about a plan is
            // often a paragraph, and a box you cannot press Enter in is not a box you can write a
            // paragraph in.
            _question.KeyDown += (_, e) =>
            {
                if (e.KeyCode != Keys.Enter || !e.Control) return;

                e.SuppressKeyPress = true;
                Ask();
            };
            _question.TextChanged += (_, _) => UpdateAskEnabled();
        }

        /// <summary>Whether there is anything on screen to read - and so anything to ask a follow-up about.</summary>
        internal bool HasConversation { get; private set; }

        /// <summary>
        /// Whether a follow-up can be asked at all.  False while a request is in flight, when there is
        /// no service to ask, and before anything has been analysed.
        /// </summary>
        internal bool CanAsk { get; private set; }

        /// <summary>Clears the view back to the state it opens in, with nothing said and nothing to say it about.</summary>
        internal void Clear()
        {
            HasConversation = false;
            _plain.Clear();
            _plain.Visible = true;
            _rendered.Visible = false;
            _ribbon.Visible = false;
            _question.Clear();
            SetCanAsk(false);
        }

        /// <summary>
        /// Shows a conversation, rendered from the Markdown the service asks the model for.  Falls back
        /// to the text as it came when the WebView2 runtime is missing - a supported state here, not an
        /// error.
        /// </summary>
        /// <returns>False when the conversation shown is no longer the one wanted, so a caller that
        /// changed artifacts while this was rendering can stop rather than write over the new one.</returns>
        internal async Task<bool> ShowAsync(AiConversation conversation, Func<bool> stillWanted)
        {
            _plain.Text = conversation.ToPlainText();

            var renderedOk = await _rendered.NavigateToLargeString(MarkdownRenderer.ToThemedHtml(conversation.ToMarkdown()));

            if (IsDisposed || !stillWanted()) return false;

            _rendered.Visible = renderedOk;
            _plain.Visible = !renderedOk;

            HasConversation = conversation.Count > 0;
            _ribbon.Visible = HasConversation;

            return true;
        }

        /// <summary>
        /// Turns the ask box on or off, saying why when it is off.  Called as the service is found, as
        /// a request starts and finishes, and as the reader moves to a different artifact.
        /// </summary>
        internal void SetCanAsk(bool canAsk, string? reason = null)
        {
            CanAsk = canAsk;
            _question.Enabled = canAsk;
            _hint.Text = canAsk
                ? "Ask a follow-up question.  Ctrl+Enter sends it."
                : reason ?? "Ask a follow-up question.";
            UpdateAskEnabled();
        }

        /// <summary>Puts the cursor where the next question goes, for a caller that has just finished one.</summary>
        internal void FocusQuestion()
        {
            if (_question.Enabled && _question.CanFocus) _question.Focus();
        }

        /// <summary>
        /// Puts a question back in the box after a request for it failed.
        ///
        /// The box is emptied when the question is asked, because a question left sitting there while
        /// its answer arrives invites asking it twice.  When no answer arrives, though, the reader is
        /// left having typed a paragraph that went nowhere - so it comes back, selected, ready to send
        /// again or to be typed over.  Anything typed while waiting wins: it is newer.
        /// </summary>
        internal void RestoreQuestion(string question)
        {
            if (_question.Text.Trim().Length > 0 || string.IsNullOrWhiteSpace(question)) return;

            _question.Text = question;
            _question.SelectAll();
            FocusQuestion();
        }

        private void UpdateAskEnabled() => _ask.Enabled = CanAsk && _question.Text.Trim().Length > 0;

        private void Ask()
        {
            var question = _question.Text.Trim();
            if (!CanAsk || question.Length == 0) return;

            // Cleared here rather than when the answer arrives: the question is on screen in the
            // transcript a moment later, and leaving it in the box invites asking it twice.
            _question.Clear();
            QuestionAsked?.Invoke(this, question);
        }
    }
}
