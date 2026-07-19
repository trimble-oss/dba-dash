using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace DBADash
{
    /// <summary>
    /// Enumerates the OS-level (perfmon) counters available on a host via WMI - the
    /// <c>Win32_PerfRawData_*</c> (raw) classes.  We collect raw counters (accumulators + base +
    /// timebase) and cook them ourselves across the collection interval, which is both more accurate
    /// than the cooked <c>Win32_PerfFormattedData_*</c> classes (true interval average vs an aliased
    /// point sample) and preserves fractional values (e.g. disk latency) that the formatted classes
    /// truncate to integer.  Uses the same WSMan-then-DCom fallback as the collector.
    /// </summary>
    public static class PerfmonCounterDiscovery
    {
        private const string PerfClassPrefix = "Win32_PerfRawData_";
        private const string PerfBaseClass = "Win32_PerfRawData";

        /// <summary>Standard properties present on every perf class that are not real counters.</summary>
        private static readonly HashSet<string> NonCounterProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "Caption", "Description", "Name",
            "Frequency_Object", "Frequency_PerfTime", "Frequency_Sys100NS",
            "Timestamp_Object", "Timestamp_PerfTime", "Timestamp_Sys100NS"
        };

        /// <summary>Bound every WMI operation so a slow/unreachable host can't hang the UI indefinitely.</summary>
        private static CimOperationOptions OperationOptions => new() { Timeout = TimeSpan.FromSeconds(30) };

        /// <summary>A single collectable counter within a perf class.</summary>
        public class DiscoveredCounter
        {
            public string WmiProperty { get; set; }
            /// <summary>The PERF_* counter type (from the CounterType qualifier) - drives how it's cooked.</summary>
            public int CounterType { get; set; }
            /// <summary>Companion base property (e.g. for fraction/average/timer counters), else null.</summary>
            public string BaseWmiProperty { get; set; }
        }

        public class DiscoveredCounterClass
        {
            public string WmiClass { get; set; }
            public string ObjectName { get; set; }
            public List<DiscoveredCounter> Counters { get; set; } = new();
        }

        /// <summary>
        /// Derive a friendly perfmon object name from a WMI perf class name,
        /// e.g. "Win32_PerfRawData_PerfOS_Processor" -> "Processor".
        /// </summary>
        public static string DeriveObjectName(string wmiClass)
        {
            if (string.IsNullOrEmpty(wmiClass) ||
                !wmiClass.StartsWith(PerfClassPrefix, StringComparison.OrdinalIgnoreCase))
                return wmiClass;
            var remainder = wmiClass.Substring(PerfClassPrefix.Length); // e.g. PerfOS_Processor
            var idx = remainder.IndexOf('_');
            return idx >= 0 && idx < remainder.Length - 1 ? remainder[(idx + 1)..] : remainder;
        }

        private static CimSession CreateSession(string computerName)
        {
            // Mirror the collector: try WSMan first, fall back to DCom.
            try
            {
                var session = CimSession.Create(computerName, new WSManSessionOptions());
                using (session.GetClass(@"root\cimv2", "Win32_ComputerSystem", OperationOptions)) { }
                return session;
            }
            catch
            {
                return CimSession.Create(computerName, new DComSessionOptions());
            }
        }

        /// <summary>
        /// Read the collectable counters (with their PERF type and base counter) from a raw perf class.
        /// Base properties (named &lt;Counter&gt;_Base) are attached to their primary and not listed
        /// separately.  Used by both discovery (for the picker) and the collector (to cook values).
        /// </summary>
        public static List<DiscoveredCounter> GetCounters(CimClass cimClass)
        {
            var propertyNames = new HashSet<string>(
                cimClass.CimClassProperties.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

            var counters = new List<DiscoveredCounter>();
            foreach (var p in cimClass.CimClassProperties)
            {
                if (NonCounterProperties.Contains(p.Name)) continue;
                if (p.Name.EndsWith("_Base", StringComparison.OrdinalIgnoreCase)) continue; // attached to its primary

                var baseProperty = p.Name + "_Base";
                counters.Add(new DiscoveredCounter
                {
                    WmiProperty = p.Name,
                    CounterType = GetCounterType(p),
                    BaseWmiProperty = propertyNames.Contains(baseProperty) ? baseProperty : null
                });
            }
            return counters;
        }

        private static int GetCounterType(CimPropertyDeclaration property)
        {
            try
            {
                return Convert.ToInt32(property.Qualifiers["CounterType"]?.Value ?? 0);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Resolve each counter's PERF type + base property for a class, keyed by WMI property.  Used by
        /// the collector to know how to cook each value.  Tries MI first (works over WSMan or DCom); if MI
        /// doesn't surface the CounterType qualifiers (they aren't always populated), falls back to
        /// System.Management which reads them reliably (DCom).
        /// </summary>
        public static Dictionary<string, DiscoveredCounter> GetCounterMetadata(CimSession session, string computerName, string wmiClass)
        {
            var result = new Dictionary<string, DiscoveredCounter>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using var cimClass = session.GetClass(@"root\cimv2", wmiClass, OperationOptions);
                foreach (var c in GetCounters(cimClass)) result[c.WmiProperty] = c;
            }
            catch
            {
                // fall through to System.Management below
            }

            // If no counter types came back, the qualifiers weren't populated - use System.Management.
            if (result.Count == 0 || result.Values.All(c => c.CounterType == 0))
            {
                result = GetCounterMetadataViaManagement(computerName, wmiClass);
            }
            return result;
        }

        private static Dictionary<string, DiscoveredCounter> GetCounterMetadataViaManagement(string computerName, string wmiClass)
        {
            var result = new Dictionary<string, DiscoveredCounter>(StringComparer.OrdinalIgnoreCase);
            if (!OperatingSystem.IsWindows()) return result; // System.Management is Windows-only
            var host = string.IsNullOrEmpty(computerName) ? "." : computerName;
            var scope = new ManagementScope($@"\\{host}\root\cimv2");
            scope.Connect();
            using var mc = new ManagementClass(scope, new ManagementPath(wmiClass), null);

            var propertyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (PropertyData p in mc.Properties) propertyNames.Add(p.Name);

            foreach (PropertyData p in mc.Properties)
            {
                if (NonCounterProperties.Contains(p.Name)) continue;
                if (p.Name.EndsWith("_Base", StringComparison.OrdinalIgnoreCase)) continue;

                var counterType = 0;
                try { counterType = Convert.ToInt32(p.Qualifiers["CounterType"].Value); } catch { /* not a counter */ }

                var baseProperty = p.Name + "_Base";
                result[p.Name] = new DiscoveredCounter
                {
                    WmiProperty = p.Name,
                    CounterType = counterType,
                    BaseWmiProperty = propertyNames.Contains(baseProperty) ? baseProperty : null
                };
            }
            return result;
        }

        /// <summary>
        /// Enumerate the perf counter classes (and their counters) available on the host.
        /// Instances are resolved lazily via <see cref="DiscoverInstances"/> when a class is selected.
        /// </summary>
        public static List<DiscoveredCounterClass> DiscoverClasses(string computerName)
        {
            using var session = CreateSession(computerName);
            var result = new List<DiscoveredCounterClass>();
            foreach (var cimClass in session.EnumerateClasses(@"root\cimv2", PerfBaseClass, OperationOptions))
            {
                using (cimClass)
                {
                    var className = cimClass.CimSystemProperties.ClassName;
                    if (string.IsNullOrEmpty(className) ||
                        !className.StartsWith(PerfClassPrefix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var counters = GetCounters(cimClass);
                    if (counters.Count == 0) continue;

                    result.Add(new DiscoveredCounterClass
                    {
                        WmiClass = className,
                        ObjectName = DeriveObjectName(className),
                        Counters = counters.OrderBy(c => c.WmiProperty, StringComparer.OrdinalIgnoreCase).ToList()
                    });
                }
            }
            return result.OrderBy(c => c.ObjectName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Resolve the instance names (the WMI Name property) for a given perf class.
        /// Returns a single empty string for single-instance classes (e.g. Memory).
        /// </summary>
        public static List<string> DiscoverInstances(string computerName, string wmiClass)
        {
            using var session = CreateSession(computerName);
            var instances = new List<string>();
            foreach (var itm in session.QueryInstances(@"root\cimv2", "WQL", "SELECT Name FROM " + wmiClass, OperationOptions))
            {
                using (itm)
                {
                    instances.Add(Convert.ToString(itm.CimInstanceProperties["Name"]?.Value) ?? string.Empty);
                }
            }
            return instances.Distinct().OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
