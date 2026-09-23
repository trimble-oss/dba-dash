using System.Collections.Generic;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// A name and value pair from a plan, optionally with children.
    ///
    /// Showplan carries far more per operator than any node caption could hold, and which of it
    /// matters depends entirely on what is being investigated - so the parser keeps all of it as a
    /// tree of these and the viewer shows it in a properties panel, the way SSMS does.
    ///
    /// Values are pre-formatted strings rather than objects.  The formatting rules belong with the
    /// plan (invariant culture, bytes in sensible units, row counts grouped) and doing it once here
    /// keeps every consumer - panel, tooltip, clipboard, AI payload - showing the same text.
    /// </summary>
    public sealed class PlanProperty
    {
        internal PlanProperty(string name, string? value, IReadOnlyList<PlanProperty>? children = null, bool isExpression = false, string? script = null)
        {
            Name = name;
            Value = value;
            Children = children ?? [];
            IsExpression = isExpression;
            Script = script;
        }

        /// <summary>
        /// T-SQL that acts on this property - the SET statements for the set options - for a viewer
        /// to offer as a View Script action.  Null when there is none.
        /// </summary>
        public string? Script { get; }

        public string Name { get; }

        /// <summary>
        /// The value is an expression or a list of them - a predicate, defined values, sort keys -
        /// which is read rather than glanced at, so a viewer offers it laid out in full.  See
        /// <see cref="ReadableValue"/>.
        /// </summary>
        public bool IsExpression { get; }

        /// <summary>
        /// The value laid out for reading: each item of a list on its own line, each AND and OR of a
        /// predicate starting one.  Null for a heading.
        /// </summary>
        public string? ReadableValue => Value is null ? null : PlanFormat.ForReading(Value);

        /// <summary>
        /// The formatted value, or null for a property that is only a heading for its children.
        /// </summary>
        public string? Value { get; }

        public IReadOnlyList<PlanProperty> Children { get; }

        public bool HasChildren => Children.Count > 0;

        public override string ToString() => Value is null ? Name : Name + ": " + Value;
    }
}
