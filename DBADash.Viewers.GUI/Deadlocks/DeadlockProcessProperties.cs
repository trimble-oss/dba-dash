using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using DBADash.Deadlock.Model;

namespace DBADashGUI.Deadlocks
{
    /// <summary>
    /// A read-only PropertyGrid view of a <see cref="DeadlockProcess"/>.  The grid columns only have
    /// room for the fields most people triage on and the single most interesting execution stack frame;
    /// this exposes the rest - every frame in the call stack included, so a nested module execution
    /// (a query calling a function calling a function) can be read in full rather than collapsed to its
    /// innermost statement.
    /// </summary>
    public sealed class DeadlockProcessProperties
    {
        private readonly DeadlockProcess _process;

        public DeadlockProcessProperties(DeadlockProcess process)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            ExecutionStack = process.ExecutionStack
                .Select(frame => new DeadlockFrameProperties(frame, ReferenceEquals(frame, process.PrimaryFrame)))
                .ToArray();
        }

        [Category("Session"), DisplayName("SPID")]
        public int? Spid => _process.Spid;

        [Category("Session"), DisplayName("ECID"),
         Description("Execution context id.  0 is the coordinating task; non-zero is a parallel worker.")]
        public int? Ecid => _process.Ecid;

        [Category("Session")]
        public string Status => _process.Status;

        [Category("Session"), DisplayName("Login")]
        public string LoginName => _process.LoginName;

        [Category("Session"), DisplayName("Host")]
        public string HostName => _process.HostName;

        [Category("Session"), DisplayName("Host PID")]
        public int? HostPid => _process.HostPid;

        [Category("Session"), DisplayName("Application")]
        public string ClientApp => _process.ClientApp;

        [Category("Session"), DisplayName("Is Victim"),
         Description("Whether this process was chosen as the deadlock victim and rolled back.")]
        public bool IsVictim => _process.IsVictim;

        [Category("Database & Isolation"), DisplayName("Database")]
        public string Database => _process.CurrentDatabaseName;

        [Category("Database & Isolation"), DisplayName("Isolation Level")]
        public string IsolationLevel => _process.IsolationLevel;

        [Category("Locking"), DisplayName("Lock Mode")]
        public string LockMode => _process.LockMode;

        [Category("Locking"), DisplayName("Wait Resource")]
        public string WaitResource => _process.WaitResource;

        [Category("Locking"), DisplayName("Wait (ms)")]
        public long? WaitMs => _process.WaitTime is { } w ? (long)w.TotalMilliseconds : null;

        [Category("Locking"), DisplayName("Deadlock Priority")]
        public int? Priority => _process.Priority;

        [Category("Transaction"), DisplayName("Tran Count")]
        public int? TransactionCount => _process.TransactionCount;

        [Category("Transaction"), DisplayName("Log Used (bytes)")]
        public long? LogUsed => _process.LogUsed;

        [Category("Transaction"), DisplayName("Transaction Name")]
        public string TransactionName => _process.TransactionName;

        [Category("Transaction"), DisplayName("Last Transaction Started")]
        public DateTime? LastTransactionStarted => _process.LastTransactionStarted;

        [Category("Transaction"), DisplayName("Last Batch Started")]
        public DateTime? LastBatchStarted => _process.LastBatchStarted;

        [Category("Transaction"), DisplayName("Last Batch Completed")]
        public DateTime? LastBatchCompleted => _process.LastBatchCompleted;

        [Category("Execution"), DisplayName("Input Buffer"),
         Editor(typeof(SqlCodeUITypeEditor), typeof(UITypeEditor))]
        public string InputBuffer => _process.InputBuffer;

        [Category("Execution"), DisplayName("Execution Stack"),
         Description("The call stack innermost first: [0] is the statement that was executing when it blocked, " +
                     "each frame below it the module that called the one above.")]
        public DeadlockFrameProperties[] ExecutionStack { get; }

        public override string ToString() => _process.DisplayName;
    }

    /// <summary>
    /// A read-only PropertyGrid view of a single execution stack frame, shown expandable within the
    /// parent process's Execution Stack.
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public sealed class DeadlockFrameProperties
    {
        private readonly DeadlockFrame _frame;
        private readonly bool _isPrimary;

        public DeadlockFrameProperties(DeadlockFrame frame, bool isPrimary)
        {
            _frame = frame ?? throw new ArgumentNullException(nameof(frame));
            _isPrimary = isPrimary;
        }

        [DisplayName("Procedure")]
        public string ProcedureName => _frame.ProcedureName;

        public int? Line => _frame.Line;

        [DisplayName("Statement Start")]
        public int? StatementStart => _frame.StatementStart;

        [DisplayName("Statement End")]
        public int? StatementEnd => _frame.StatementEnd;

        [DisplayName("SQL Handle")]
        public string SqlHandle => _frame.SqlHandle;

        [DisplayName("SQL"),
         Editor(typeof(SqlCodeUITypeEditor), typeof(UITypeEditor))]
        public string Sql => _frame.Sql;

        [DisplayName("Primary Frame"),
         Description("The frame the viewer treats as the interesting one - the innermost frame carrying " +
                     "statement text.  Its statement drives the Procedure and Plans lookups.")]
        public bool IsPrimaryFrame => _isPrimary;

        public override string ToString()
        {
            var name = string.IsNullOrWhiteSpace(_frame.ProcedureName) ? "(unknown)" : _frame.ProcedureName;
            var summary = _frame.Line.HasValue ? $"{name} (line {_frame.Line})" : name;
            return _isPrimary ? $"{summary}  \u2190 primary" : summary;
        }
    }
}
