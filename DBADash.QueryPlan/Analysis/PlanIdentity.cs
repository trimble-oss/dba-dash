using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DBADash.QueryPlan.Model;

namespace DBADash.QueryPlan.Analysis
{
    /// <summary>
    /// What identifies a statement's plan, for storing an analysis of it against and finding one again.
    ///
    /// Two identities, for the same reason a deadlock has two - see DeadlockSignature and DeadlockHash
    /// in DBADash.Deadlock:
    /// <list type="bullet">
    ///   <item><see cref="Query"/> is the statement, whatever plan the optimiser gave it.  It is what
    ///   groups occurrences, so an answer about a query found slow last Tuesday is offered again when
    ///   the same query turns up slow today.</item>
    ///   <item><see cref="Plan"/> is this shape of plan for it.  An answer about this exact plan is the
    ///   better one to lead with, because a query that got a different plan is a different problem -
    ///   often precisely the problem.</item>
    /// </list>
    ///
    /// Both come from showplan where it supplies them: QueryHash and QueryPlanHash are SQL Server's own,
    /// which means an answer stored from a plan captured one way is found again from the same plan
    /// captured another.  Where they are absent - an estimated plan from an older build, a plan saved by
    /// a tool that dropped them - they are computed here instead, from the statement text and from the
    /// plan XML.  A computed identity only ever matches another computed one, which is the honest
    /// outcome: it says "the same text" rather than claiming to be SQL Server's hash of the query.
    /// </summary>
    public sealed class PlanIdentity
    {
        /// <summary>Width in bytes, matching SQL Server's own hashes and the BINARY(8) they are stored in.</summary>
        public const int Bytes = 8;

        private PlanIdentity(string query, string plan, bool queryFromServer, bool planFromServer)
        {
            Query = query;
            Plan = plan;
            QueryFromServer = queryFromServer;
            PlanFromServer = planFromServer;
        }

        /// <summary>The query's identity, in "0x..." hex.  Groups the same statement across plans and runs.</summary>
        public string Query { get; }

        /// <summary>This plan's identity, in "0x..." hex.  Identifies the shape the optimiser produced.</summary>
        public string Plan { get; }

        /// <summary>True when <see cref="Query"/> is SQL Server's QueryHash rather than one computed here.</summary>
        public bool QueryFromServer { get; }

        /// <summary>True when <see cref="Plan"/> is SQL Server's QueryPlanHash rather than one computed here.</summary>
        public bool PlanFromServer { get; }

        /// <summary>What the identities were taken from, so the grouping is inspectable in the preview.</summary>
        public string Components =>
            (QueryFromServer ? "query hash from the plan" : "a hash of the statement text") + ", " +
            (PlanFromServer ? "plan hash from the plan" : "a hash of the plan XML");

        public static PlanIdentity For(PlanStatement statement, string? planXml)
        {
            ArgumentNullException.ThrowIfNull(statement);

            var queryHash = Normalise(statement.QueryHash);
            var planHash = Normalise(statement.QueryPlanHash);

            return new PlanIdentity(
                queryHash ?? Compute(statement.StatementText ?? statement.StatementType ?? string.Empty),
                // The statement text goes into the computed plan identity as well as the XML.  A batch of
                // several statements shares one document, and without it every statement in the batch
                // would come out with the same plan identity.
                planHash ?? Compute((statement.StatementText ?? string.Empty) + "\n" + (planXml ?? string.Empty)),
                queryHash is not null,
                planHash is not null);
        }

        /// <summary>
        /// Showplan writes these as "0x" and sixteen hex digits, but not always: some builds pad
        /// differently, and a plan that has been through another tool can arrive without the prefix.
        /// Anything that is not exactly eight bytes of hex is treated as absent rather than repaired -
        /// the computed identity is a better answer than a half-understood one.
        /// </summary>
        private static string? Normalise(string? hash)
        {
            if (string.IsNullOrWhiteSpace(hash)) return null;

            var digits = hash!.Trim();
            if (digits.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) digits = digits[2..];

            if (digits.Length != Bytes * 2) return null;

            foreach (var c in digits)
            {
                if (!Uri.IsHexDigit(c)) return null;
            }

            return "0x" + digits.ToLower(CultureInfo.InvariantCulture);
        }

        /// <summary>A SHA-256 of the text, truncated to the same width as SQL Server's own hashes.</summary>
        private static string Compute(string text) =>
            "0x" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))[..Bytes])
                .ToLower(CultureInfo.InvariantCulture);
    }
}
