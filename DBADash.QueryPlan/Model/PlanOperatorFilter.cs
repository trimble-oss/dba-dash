using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DBADash.QueryPlan.Model
{
    /// <summary>
    /// Which operators the operator list shows.  Every condition set has to hold; within
    /// <see cref="Groups"/>, an operator in any one of them is enough - ticking Scans and Lookups
    /// is asking for both.
    ///
    /// Here rather than in the viewer so the rules can be tested without a window.
    /// </summary>
    public sealed class PlanOperatorFilter
    {
        /// <summary>The groups to show.  Empty shows every operator.</summary>
        public IReadOnlyCollection<PlanOperatorGroup> Groups { get; init; } = [];

        /// <summary>One operator by its caption - Index Seek, Hash Match - or null for any.</summary>
        public string? OperatorName { get; init; }

        /// <summary>
        /// Only operators with something to say about them - see <see cref="PlanInsights.ForOperator"/>:
        /// a warning, a missing index on the table read, or one of DBA Dash's own findings.  Without a
        /// statement to judge them against, only the operator's own warnings count.
        /// </summary>
        public bool InsightsOnly { get; init; }

        /// <summary>
        /// Text to find, ignoring case, in the operator's names, the objects it touches or its
        /// predicates - or a node id, matched exactly.  Null or blank for no text filter.
        /// </summary>
        public string? Text { get; init; }

        /// <summary>True when the filter lets everything through.</summary>
        public bool IsEmpty => Groups.Count == 0 && OperatorName is null && !InsightsOnly && string.IsNullOrWhiteSpace(Text);

        public bool Matches(PlanOperator op, PlanStatement? statement = null)
        {
            ArgumentNullException.ThrowIfNull(op);

            if (Groups.Count > 0 && !Groups.Any(group => group.Contains(op))) return false;

            if (OperatorName is not null && !string.Equals(op.DisplayName, OperatorName, StringComparison.OrdinalIgnoreCase)) return false;

            if (InsightsOnly && (statement is null ? !op.HasWarnings : PlanInsights.ForOperator(op, statement).Count == 0)) return false;

            return string.IsNullOrWhiteSpace(Text) || MatchesText(op, Text.Trim());
        }

        public IEnumerable<PlanOperator> Apply(PlanStatement statement) =>
            statement.Operators.Where(op => Matches(op, statement));

        private static bool MatchesText(PlanOperator op, string text)
        {
            // A node id is what the cards and lists name an operator by, so typing one finds it - and
            // only it, rather than every operator with that digit somewhere in a predicate.
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var nodeId))
            {
                return op.NodeId == nodeId;
            }

            return SearchableText(op).Any(value => value?.Contains(text, StringComparison.OrdinalIgnoreCase) == true);
        }

        private static IEnumerable<string?> SearchableText(PlanOperator op)
        {
            yield return op.DisplayName;
            yield return op.PhysicalOp;
            yield return op.LogicalOp;
            yield return op.Predicate;
            yield return op.SeekPredicate;

            // Unbracketed as well as bracketed, so dbo.Orders and [dbo].[Orders] both find it.
            foreach (var target in op.Objects)
            {
                yield return target.ToString();
                yield return target.QualifiedTableName;
                yield return target.Index;
            }
        }
    }
}
