using System;
using System.IO;
using System.Reflection;

namespace DBADash.Deadlock.Test
{
    /// <summary>
    /// Loads the sample deadlock graphs embedded in this assembly.  Embedded rather than copied to
    /// the output directory so the tests do not depend on the build layout, and so the samples
    /// travel with the project if it is ever extracted into its own repository.
    /// </summary>
    internal static class TestGraphs
    {
        public const string KeyLock = "KeyLockDeadlock";
        public const string DeadlockListWrapper = "DeadlockListWrapper";
        public const string Parallel = "ParallelDeadlock";
        public const string XeEventEnvelope = "XeEventEnvelope";
        public const string Truncated = "TruncatedGraph";
        public const string Multiple = "MultipleDeadlocks";
        public const string ThreeWay = "ThreeWayDeadlock";

        /// <summary>Two indexes of one table locked in opposite order - the key-lookup deadlock.</summary>
        public const string KeyLookup = "KeyLookupDeadlock";

        /// <summary>Processes at different DEADLOCK_PRIORITY, so priority chose the victim.</summary>
        public const string Priority = "DeadlockPriority";

        /// <summary>A victim that had written a lot of transaction log to roll back.</summary>
        public const string ExpensiveVictim = "ExpensiveVictim";

        /// <summary>A victim that had written very little, so its rollback is not remarked on.</summary>
        public const string SmallVictim = "SmallVictim";

        /// <summary>An IX object lock, which must not be mistaken for a whole-table lock.</summary>
        public const string IntentObjectLock = "IntentObjectLock";

        /// <summary>A single wait that does not close into a cycle - half a deadlock.</summary>
        public const string NoCycle = "NoCycleGraph";

        /// <summary>A resource with two owners, only one of which is on the cycle.</summary>
        public const string SharedLock = "SharedLockDeadlock";

        /// <summary>Both processes own and wait on the one resource - an S to X conversion.</summary>
        public const string Conversion = "ConversionDeadlock";

        /// <summary>
        /// A parallel DELETE against one table, with the resource-list written the way SQL Server
        /// writes a busy one: the same page lock listed under one lock id several times over, and an
        /// owner repeated within an entry.
        /// </summary>
        public const string ParallelPageLocks = "ParallelPageDeadlock";

        public static string Load(string name)
        {
            // .xdl - the extension SQL Server and SSMS save a deadlock graph under - so the samples
            // are files you can open in the viewer, or in SSMS, exactly as they are.
            var resourceName = $"DBADash.Deadlock.Test.Graphs.{name}.xdl";
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded test graph '{resourceName}' was not found.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
