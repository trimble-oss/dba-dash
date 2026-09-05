namespace DBADash.Deadlock.Model
{
    /// <summary>
    /// A single frame from a process's execution stack.
    /// </summary>
    public sealed class DeadlockFrame
    {
        /// <summary>Procedure the frame executed in.  "adhoc" for statements not in a module.</summary>
        public string? ProcedureName { get; internal set; }

        public int? Line { get; internal set; }

        /// <summary>Statement start offset in bytes, as reported by SQL Server.</summary>
        public int? StatementStart { get; internal set; }

        /// <summary>Statement end offset in bytes.  -1 means "to the end of the batch".</summary>
        public int? StatementEnd { get; internal set; }

        public string? SqlHandle { get; internal set; }

        /// <summary>The statement text carried in the frame, trimmed of surrounding whitespace.</summary>
        public string? Sql { get; internal set; }

        /// <summary>
        /// True when <see cref="ProcedureName"/> names a real module rather than one of the placeholders
        /// SQL Server uses for a statement that isn't in one ("adhoc", "unknown").
        /// </summary>
        public bool IsModule =>
            !string.IsNullOrWhiteSpace(ProcedureName) &&
            !ProcedureName!.Equals("adhoc", System.StringComparison.OrdinalIgnoreCase) &&
            !ProcedureName!.Equals("unknown", System.StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The database part of a three-part <see cref="ProcedureName"/> (e.g. "Sales" from
        /// "Sales.dbo.usp_UpdateOrder").  Null when the frame isn't a module or the name isn't qualified.
        /// </summary>
        public string? ModuleDatabaseName => NamePart(3);

        /// <summary>The schema part of a qualified <see cref="ProcedureName"/>, or null.</summary>
        public string? ModuleSchemaName => NamePart(2);

        /// <summary>
        /// The bare object name from <see cref="ProcedureName"/> (e.g. "usp_UpdateOrder").  This is the form
        /// Query Store reports through OBJECT_NAME(), so it is what a Query Store lookup filters on.
        /// </summary>
        public string? ModuleObjectName => NamePart(1);

        /// <summary>
        /// Returns the requested part of the procedure name counting back from the object name, so 1 is the
        /// object, 2 the schema and 3 the database.  Null when the name has no such part.
        /// </summary>
        private string? NamePart(int partsFromEnd)
        {
            if (!IsModule) return null;
            var parts = ProcedureName!.Split('.');
            var index = parts.Length - partsFromEnd;
            if (index < 0) return null;
            var part = parts[index].Trim('[', ']');
            return string.IsNullOrWhiteSpace(part) ? null : part;
        }
    }
}
