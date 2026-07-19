using System;
using System.Linq;
using DBADash;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DBADash.Test
{
    [TestClass]
    public class PerfmonCounterTests
    {
        [TestMethod]
        public void ParseSpec_ClassAndProperty_DefaultsInstanceToStar()
        {
            var c = PerfmonCounter.ParseSpec("Win32_PerfRawData_PerfOS_Processor:PercentProcessorTime");

            Assert.AreEqual("Win32_PerfRawData_PerfOS_Processor", c.WmiClass);
            Assert.AreEqual("PercentProcessorTime", c.WmiProperty);
            Assert.AreEqual("*", c.InstanceName);
            // Object name is derived from the class so the stored name is friendly.
            Assert.AreEqual("Processor", c.EffectiveObjectName);
            // Counter type is resolved by the collector at collection time, not at config time.
            Assert.AreEqual(0, c.CounterType);
            Assert.IsNull(c.BaseWmiProperty);
        }

        [TestMethod]
        public void ParseSpec_ExplicitInstance_IsUsed()
        {
            var c = PerfmonCounter.ParseSpec("Win32_PerfRawData_PerfOS_Processor:PercentProcessorTime:_Total");
            Assert.AreEqual("_Total", c.InstanceName);
        }

        [TestMethod]
        public void ParseSpec_InstanceContainingColon_IsPreserved()
        {
            // A drive instance name legitimately contains a colon - only the first two colons delimit.
            var c = PerfmonCounter.ParseSpec("Win32_PerfRawData_PerfDisk_LogicalDisk:AvgDisksecPerRead:C:");
            Assert.AreEqual("AvgDisksecPerRead", c.WmiProperty);
            Assert.AreEqual("C:", c.InstanceName);
        }

        [TestMethod]
        public void ParseSpec_WhitespaceIsTrimmed()
        {
            var c = PerfmonCounter.ParseSpec("  Win32_PerfRawData_PerfOS_System : ProcessorQueueLength ");
            Assert.AreEqual("Win32_PerfRawData_PerfOS_System", c.WmiClass);
            Assert.AreEqual("ProcessorQueueLength", c.WmiProperty);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("Win32_PerfRawData_PerfOS_Processor")]              // missing property
        [DataRow("Win32_PerfRawData_PerfOS_Processor:")]             // empty property
        [DataRow(":PercentProcessorTime")]                          // empty class
        [DataRow("Win32_PerfFormattedData_PerfOS_Processor:PercentProcessorTime")] // not a raw class
        public void ParseSpec_InvalidInput_Throws(string spec)
        {
            Assert.ThrowsExactly<ArgumentException>(() => PerfmonCounter.ParseSpec(spec));
        }

        [TestMethod]
        public void ParseSpecList_ParsesMultiple_AndSkipsBlankEntries()
        {
            var list = PerfmonCounter.ParseSpecList(
                "Win32_PerfRawData_PerfOS_Processor:PercentProcessorTime:_Total, ,Win32_PerfRawData_PerfOS_Memory:PagesPersec");

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("Processor", list[0].EffectiveObjectName);
            Assert.AreEqual("Memory", list[1].EffectiveObjectName);
        }

        [TestMethod]
        public void ParseSpecList_Empty_ReturnsEmptyList()
        {
            Assert.AreEqual(0, PerfmonCounter.ParseSpecList("").Count);
            Assert.AreEqual(0, PerfmonCounter.ParseSpecList(null).Count);
        }

        [TestMethod]
        public void BuildList_DefaultsOnly_ReturnsAllDefaults()
        {
            var list = PerfmonCounter.BuildList(includeDefaults: true, specs: null);
            Assert.AreEqual(PerfmonCounter.DefaultCounters().Count, list.Count);
        }

        [TestMethod]
        public void BuildList_SpecsOnly_ReturnsParsedSpecs()
        {
            var list = PerfmonCounter.BuildList(includeDefaults: false,
                specs: "Win32_PerfRawData_PerfOS_Processor:PercentProcessorTime:_Total,Win32_PerfRawData_PerfOS_Memory:PagesPersec");
            Assert.AreEqual(2, list.Count);
        }

        [TestMethod]
        public void BuildList_DefaultsPlusOverlappingSpec_DeDuplicatesByWmiIdentity()
        {
            // Re-specify a default counter by its WMI identity (class / property / instance).  Even though the
            // spec would store a different display name, it is the same counter, so it is de-duplicated.
            var list = PerfmonCounter.BuildList(includeDefaults: true,
                specs: "Win32_PerfRawData_PerfOS_Processor:PercentProcessorTime:_Total");

            Assert.AreEqual(PerfmonCounter.DefaultCounters().Count, list.Count);
        }

        [TestMethod]
        public void ResolveStorageNames_KnownCounter_UsesCanonicalName()
        {
            // A well-known counter added by its raw WMI property name still stores under the curated display name.
            var (objectName, counterName) =
                PerfmonCounter.ResolveStorageNames("Win32_PerfRawData_PerfOS_Processor", "PercentProcessorTime");
            Assert.AreEqual("Processor", objectName);
            Assert.AreEqual("% Processor Time", counterName);
        }

        [TestMethod]
        public void ResolveStorageNames_UnknownCounter_DerivesFromWmiIdentity()
        {
            var (objectName, counterName) =
                PerfmonCounter.ResolveStorageNames("Win32_PerfRawData_PerfProc_Process", "HandleCount");
            Assert.AreEqual("Process", objectName);   // derived from the class name
            Assert.AreEqual("HandleCount", counterName); // falls back to the WMI property
        }

        [TestMethod]
        public void ResolveStorageNames_IsDeterministic_RegardlessOfConfigNames()
        {
            // The same WMI identity always resolves to the same stored name, so it can't fragment.
            var viaProperty = PerfmonCounter.ResolveStorageNames("Win32_PerfRawData_PerfOS_Memory", "PagesPersec");
            var defMemory = PerfmonCounter.DefaultCounters()
                .Single(c => c.WmiClass == "Win32_PerfRawData_PerfOS_Memory" && c.WmiProperty == "PagesPersec");
            Assert.AreEqual(defMemory.EffectiveObjectName, viaProperty.ObjectName);
            Assert.AreEqual(defMemory.EffectiveCounterName, viaProperty.CounterName);
        }

        [TestMethod]
        public void BuildList_InvalidSpec_Throws()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => PerfmonCounter.BuildList(includeDefaults: true, specs: "not_a_raw_class:Foo"));
        }

        [TestMethod]
        public void DefaultCounters_AllUseRawClasses_AndAreUnique()
        {
            var defaults = PerfmonCounter.DefaultCounters();
            Assert.IsTrue(defaults.Count > 0);

            // Every default must be a raw perf class (the cooking in PerfmonCounters_Upd assumes raw values).
            Assert.IsTrue(defaults.All(c => c.WmiClass.StartsWith(PerfmonCounter.RawClassPrefix, StringComparison.OrdinalIgnoreCase)),
                "All default counters must use the raw Win32_PerfRawData_* classes.");

            // No duplicate (object, counter, instance) - that key is unique in dbo.Counters.
            var distinct = defaults
                .Select(c => (c.EffectiveObjectName, c.EffectiveCounterName, c.InstanceName))
                .Distinct()
                .Count();
            Assert.AreEqual(defaults.Count, distinct, "Default counters contain a duplicate (object, counter, instance).");
        }
    }
}
