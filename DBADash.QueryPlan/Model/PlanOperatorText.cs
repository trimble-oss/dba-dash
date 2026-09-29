using System.Collections.Generic;
using System.Linq;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// Everything one operator says, as text, for the things that search a plan rather than read it:
    /// which operator uses an expression, and which one a plan level warning is really about.
    ///
    /// The whole property list rather than the predicates alone, because what is being looked for
    /// turns up in an output list, a sort key, a hash key, a group by and a probe column as well -
    /// and the properties are built by the time any of this runs and carry all of it as text.
    /// </summary>
    internal static class PlanOperatorText
    {
        /// <summary>The property holding the operator's output columns, each with what it means beside it.</summary>
        public const string OutputList = "Output List";

        public static IEnumerable<string> Of(PlanOperator op)
        {
            foreach (var column in op.OutputList)
            {
                if (column.Column is { } name) yield return name;
            }

            foreach (var text in Of(op.Properties)) yield return text;
        }

        /// <summary>
        /// What the operator itself works out or tests - everything but the output list.
        ///
        /// The output list writes each generated name out with its definition beside it, and that
        /// definition belongs to whichever operator computed the value, often far below.  A value
        /// carried up through a dozen operators would otherwise have its expression found in all of
        /// them, when only one of them evaluates it.
        /// </summary>
        public static IEnumerable<string> Evaluated(PlanOperator op) =>
            Of(op.Properties.Where(property => property.Name != OutputList).ToList());

        private static IEnumerable<string> Of(IReadOnlyList<PlanProperty> properties)
        {
            foreach (var property in properties)
            {
                yield return property.Name;
                if (property.Value is { } value) yield return value;

                foreach (var text in Of(property.Children)) yield return text;
            }
        }
    }
}
