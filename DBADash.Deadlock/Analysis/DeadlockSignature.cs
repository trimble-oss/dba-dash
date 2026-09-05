using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DBADash.Deadlock.Model;

namespace DBADash.Deadlock.Analysis
{
    /// <summary>
    /// A stable identifier for the <em>shape</em> of a deadlock, so the same deadlock happening two
    /// hundred times is recognisable as one problem rather than two hundred incidents.
    ///
    /// Two deadlocks share a signature when the same code deadlocks over the same objects in the same
    /// way.  Everything that varies between occurrences of one problem is deliberately left out:
    /// spids, transaction ids, timestamps, hosts, logins, wait times, and the literal values in the
    /// statements.  Everything that identifies the problem is in: the modules or statements
    /// involved, the objects and indexes they collided on, and the lock modes.
    ///
    /// The value is what a history groups by, and what an analysis is cached against - the answer to
    /// "why does this deadlock" is the same answer every time it recurs.
    /// </summary>
    public sealed class DeadlockSignature
    {
        private DeadlockSignature(string value, string components)
        {
            Value = value;
            Components = components;
        }

        /// <summary>The signature as a hex string, e.g. "0x3f2a9c81b4d5e607".</summary>
        public string Value { get; }

        /// <summary>
        /// The canonical text the value was hashed from.  Kept so that two graphs that unexpectedly
        /// do or do not group can be compared without guessing, and so a viewer can show what the
        /// grouping is actually based on.
        /// </summary>
        public string Components { get; }

        /// <summary>Version prefix, so a change to what goes into a signature is visible rather than silent.</summary>
        private const string Version = "v1";

        public static DeadlockSignature Compute(DeadlockGraph graph)
        {
            ArgumentNullException.ThrowIfNull(graph);

            var parts = new List<string> { Version };

            // Sorted, so which process the graph happens to list first cannot change the signature.
            parts.AddRange(graph.Processes.Select(Describe).OrderBy(p => p, StringComparer.Ordinal));
            parts.AddRange(graph.Resources.Select(Describe).OrderBy(r => r, StringComparer.Ordinal));

            var components = string.Join("\n", parts);
            return new DeadlockSignature(Hash(components), components);
        }

        /// <summary>
        /// What a process contributes: the code it was running.  A module is named, because that is
        /// stable and readable; ad-hoc SQL falls back to a fingerprint of the statement with its
        /// literals removed, so the same statement over different rows still groups.
        /// </summary>
        private static string Describe(DeadlockProcess process)
        {
            var frame = process.PrimaryFrame;

            var code = frame is { IsModule: true }
                ? $"module={frame.ProcedureName!.ToLowerInvariant()}"
                : $"statement={Fingerprint(process.PrimaryStatement)}";

            return $"process|{code}|isolation={Normalise(process.IsolationLevel)}|victim={process.IsVictim}";
        }

        /// <summary>
        /// What a resource contributes: what was contended, and how.  Hobt ids and lock ids are left
        /// out - they identify this occurrence, not this problem, and change when the table is
        /// rebuilt.
        /// </summary>
        private static string Describe(DeadlockResource resource)
        {
            var modes = resource.Owners.Concat(resource.Waiters)
                .Select(p => p.Mode)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => m!.ToLowerInvariant())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(m => m, StringComparer.Ordinal);

            return string.Join("|", new[]
            {
                "resource",
                $"type={resource.TypeName.ToLowerInvariant()}",
                $"object={Normalise(resource.ObjectName)}",
                $"index={Normalise(resource.IndexName)}",
                $"modes={string.Join(",", modes)}"
            });
        }

        /// <summary>
        /// A statement with its literals replaced, so the same code against different rows produces
        /// the same text.  Deliberately crude - this is a grouping key, not a parser: it does not
        /// need to understand the SQL, only to stop 8812 and 8813 looking like different problems.
        /// </summary>
        private static string Fingerprint(string? sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return string.Empty;

            var text = QuotedLiterals.Replace(sql, "?");
            text = Numbers.Replace(text, "?");
            text = Whitespace.Replace(text, " ");

            return text.Trim().ToLowerInvariant();
        }

        private static string Normalise(string? value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();

        private static string Hash(string components)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(components));

            // Eight bytes is plenty to tell deadlock patterns apart, and short enough to read out,
            // put in a grid column, or paste into a ticket - the same trade SQL Server makes for
            // query_hash.
            var builder = new StringBuilder("0x", 18);
            foreach (var b in bytes.Take(8))
            {
                builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static readonly Regex QuotedLiterals =
            new("'([^']|'')*'", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex Numbers =
            new(@"\b\d+(\.\d+)?\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex Whitespace =
            new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public override string ToString() => Value;
    }
}
