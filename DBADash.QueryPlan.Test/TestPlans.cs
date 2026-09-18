using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DBADash.QueryPlan;
using DBADash.QueryPlan.Model;

namespace DBADash.QueryPlan.Test
{
    /// <summary>
    /// Loads the embedded sample plans.
    ///
    /// Embedded rather than copied to the output directory so the tests do not depend on where the
    /// build put them, which is what the deadlock tests do for the same reason.
    /// </summary>
    internal static class TestPlans
    {
        public const string KeyLookupSeek = "KeyLookupSeek";

        public const string ParallelSpill = "ParallelSpill";

        public const string Batch = "Batch";

        public static string Xml(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var resource = assembly.GetManifestResourceNames()
                               .FirstOrDefault(n => n.EndsWith("." + name + ".sqlplan", StringComparison.Ordinal))
                           ?? throw new InvalidOperationException(
                               $"The sample plan '{name}' is not embedded.  Available: " +
                               string.Join(", ", assembly.GetManifestResourceNames()));

            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public static ExecutionPlan Load(string name) => PlanParser.Parse(Xml(name));

        /// <summary>The statement the viewer would open on, which is what most tests are about.</summary>
        public static PlanStatement Statement(string name) =>
            Load(name).PrimaryStatement ?? throw new InvalidOperationException("No statement in " + name);

        /// <summary>The operator with a given node id, so a test can name the one it means.</summary>
        public static PlanOperator Operator(PlanStatement statement, int nodeId) =>
            statement.Operators.FirstOrDefault(o => o.NodeId == nodeId)
            ?? throw new InvalidOperationException($"No operator with node id {nodeId}.");
    }
}
