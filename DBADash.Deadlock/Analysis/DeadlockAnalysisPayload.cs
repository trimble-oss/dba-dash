using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBADash.Deadlock.Model;

namespace DBADash.Deadlock.Analysis
{
    /// <summary>
    /// Everything an AI analysis of a deadlock is given, assembled in one place so that what the user
    /// is shown before submitting and what is actually sent cannot drift apart.  The viewer previews
    /// this object; the client serialises the same object.
    ///
    /// It leaves the estate, so what goes in is a deliberate decision rather than a convenience:
    /// <list type="bullet">
    ///   <item>the graph XML, which is the whole point;</item>
    ///   <item>the findings already established locally, so the model elaborates on them rather than
    ///   rediscovering - or contradicting - what the viewer has already told the user;</item>
    ///   <item>the signature, so the answer is recorded against the pattern and shown next time it
    ///   recurs, rather than paid for again;</item>
    ///   <item>the objects and modules involved, named so an enrichment step can be added later
    ///   without changing the shape of the request.</item>
    /// </list>
    /// Note that the graph XML carries the statements as executed, which routinely include parameter
    /// values.  That is the reason the viewer shows this before sending rather than after.
    /// </summary>
    public sealed class DeadlockAnalysisPayload
    {
        /// <summary>
        /// What kind of request this is - which is to say, what the model was given to reason from.
        /// Recorded with the analysis, so a reader looking at a stored answer can tell whether it was
        /// produced without the object definitions that are sent now.
        ///
        /// Bump <see cref="Revision"/> when the content changes materially - not for wording, but for
        /// anything the answer could depend on.
        /// </summary>
        public string Version =>
            Schema.Count > 0 ? $"{Revision}+schema" : Revision;

        /// <summary>2: object definitions added.  1: graph, findings and signature only.</summary>
        private const string Revision = "2";

        /// <summary>Groups occurrences of one deadlock - see <see cref="DeadlockSignature"/>.</summary>
        public string Signature { get; internal set; } = string.Empty;

        /// <summary>What the signature was computed from, so the grouping is inspectable.</summary>
        public string SignatureComponents { get; internal set; } = string.Empty;

        /// <summary>The instance the deadlock came from, where the viewer knows it.  Null for a file.</summary>
        public string? Instance { get; internal set; }

        /// <summary>One line per participant: who, what, and how much it had done.</summary>
        public IReadOnlyList<string> Participants { get; internal set; } = Array.Empty<string>();

        /// <summary>Object and index names the deadlock was over, as they appear in the graph.</summary>
        public IReadOnlyList<string> Objects { get; internal set; } = Array.Empty<string>();

        /// <summary>Modules the statements were running in, for a later schema enrichment to resolve.</summary>
        public IReadOnlyList<string> Modules { get; internal set; } = Array.Empty<string>();

        /// <summary>What the local rules already established, as "severity: title - detail".</summary>
        public IReadOnlyList<string> Findings { get; internal set; } = Array.Empty<string>();

        /// <summary>The deadlock graph itself.</summary>
        public string GraphXml { get; internal set; } = string.Empty;

        /// <summary>
        /// Definitions of the objects involved, as at the deadlock, where the repository has them.
        /// Empty when schema snapshots are not enabled, when the graph came from a file, or when the
        /// user has chosen not to include them - all ordinary states, not failures.
        /// </summary>
        public IReadOnlyList<DeadlockObjectDefinition> Schema { get; internal set; }
            = Array.Empty<DeadlockObjectDefinition>();

        public static DeadlockAnalysisPayload Build(
            DeadlockGraph graph,
            IReadOnlyList<DeadlockFinding> findings,
            string? instance = null,
            IReadOnlyList<DeadlockObjectDefinition>? schema = null)
        {
            ArgumentNullException.ThrowIfNull(graph);
            ArgumentNullException.ThrowIfNull(findings);

            var signature = DeadlockSignature.Compute(graph);

            return new DeadlockAnalysisPayload
            {
                Signature = signature.Value,
                SignatureComponents = signature.Components,
                Instance = instance,
                Participants = graph.Processes.Select(Describe).ToList(),
                Objects = graph.Resources
                    .Select(r => r.ObjectDisplayName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Modules = graph.Processes
                    .Select(p => p.PrimaryFrame)
                    .Where(f => f is { IsModule: true })
                    .Select(f => f!.ProcedureName!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Findings = findings
                    .Select(f => $"{f.Severity}: {f.Title} - {f.Detail}")
                    .ToList(),
                GraphXml = graph.Xml,
                Schema = schema ?? Array.Empty<DeadlockObjectDefinition>()
            };
        }

        /// <summary>
        /// The payload as something a person can actually read before deciding to send it.
        ///
        /// It is rendered rather than shown as the raw JSON on purpose: JSON escaping turns a
        /// deadlock graph into one unreadable line, and a preview nobody can read is not a preview.
        /// Every field that goes on the wire appears here - a test holds that true - so what is shown
        /// and what is sent stay the same thing in different clothes.
        /// </summary>
        public string ToPreview()
        {
            var preview = new StringBuilder();

            preview.AppendLine("This is everything that would be sent to the AI service.");
            preview.AppendLine("Nothing leaves this machine until you press Submit for analysis.");
            preview.AppendLine();

            Section(preview, "Signature", Signature);
            Section(preview, "Signature is based on", SignatureComponents.Split('\n'));
            Section(preview, "Instance", Instance);
            Section(preview, "Participants", Participants);
            Section(preview, "Objects contended", Objects);
            Section(preview, "Modules involved", Modules);
            Section(preview, "Findings already established locally", Findings);

            if (Schema.Count > 0)
            {
                preview.AppendLine("Object definitions, as at the deadlock:");
                foreach (var definition in Schema)
                {
                    var asAt = definition.AsAt is { } date ? $", snapshot {date:yyyy-MM-dd HH:mm}" : string.Empty;
                    var truncated = definition.Truncated ? ", truncated" : string.Empty;

                    preview.AppendLine();
                    preview.AppendLine(
                        $"  {definition.Database}.{definition.Name} ({definition.ObjectType}{asAt}{truncated})");
                    preview.AppendLine("  " + new string('-', 60));

                    foreach (var line in definition.Ddl.Split('\n'))
                    {
                        preview.AppendLine("  " + line.TrimEnd('\r'));
                    }
                }
                preview.AppendLine();
            }

            preview.AppendLine();
            preview.AppendLine("Deadlock graph XML");
            preview.AppendLine("------------------");
            preview.AppendLine(
                "Note: the graph carries the statements as they ran, which often include parameter values.");
            preview.AppendLine();
            preview.AppendLine(GraphXml);

            return preview.ToString();
        }

        private static void Section(StringBuilder preview, string heading, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            preview.AppendLine($"{heading}: {value}");
            preview.AppendLine();
        }

        private static void Section(StringBuilder preview, string heading, IReadOnlyList<string> values)
        {
            if (values.Count == 0) return;

            preview.AppendLine($"{heading}:");
            foreach (var value in values)
            {
                preview.AppendLine($"  - {value}");
            }
            preview.AppendLine();
        }

        private static string Describe(DeadlockProcess process)
        {
            var parts = new List<string> { process.DisplayName };

            if (process.IsVictim) parts.Add("victim");
            if (!string.IsNullOrWhiteSpace(process.CurrentDatabaseName)) parts.Add($"database {process.CurrentDatabaseName}");
            if (!string.IsNullOrWhiteSpace(process.ClientApp)) parts.Add($"application {process.ClientApp}");
            if (!string.IsNullOrWhiteSpace(process.IsolationLevel)) parts.Add($"isolation {process.IsolationLevel}");
            if (process.WaitTime is { } wait) parts.Add($"waited {DeadlockFormat.Duration(wait)}");
            if (process.LogUsed is { } log) parts.Add($"log used {DeadlockFormat.Bytes(log)}");
            if (process.TransactionCount is { } tran) parts.Add($"tran count {tran}");

            return string.Join(", ", parts);
        }
    }
}
