using System.Collections.Generic;

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
        public static IEnumerable<string> Of(PlanOperator op)
        {
            foreach (var column in op.OutputList)
            {
                if (column.Column is { } name) yield return name;
            }

            foreach (var text in Of(op.Properties)) yield return text;
        }

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
