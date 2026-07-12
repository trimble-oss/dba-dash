using DBADashService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace DBADash.Test
{
    [TestClass]
    public class CollectionSchedulesSerializationTests
    {
        [TestMethod]
        public void Drivers_CustomSchedule_RoundTrips()
        {
            var schedules = new CollectionSchedules
            {
                [CollectionType.Drivers] = new CollectionSchedule { Schedule = "0 0 22 * * ?", RunOnServiceStart = false }
            };

            var json = JsonConvert.SerializeObject(schedules);
            // Persisted using the current member name, not the collection method.
            StringAssert.Contains(json, "\"Drivers\"");

            var restored = JsonConvert.DeserializeObject<CollectionSchedules>(json)!;
            Assert.IsTrue(restored.ContainsKey(CollectionType.Drivers));
            Assert.AreEqual("0 0 22 * * ?", restored[CollectionType.Drivers].Schedule);
            Assert.IsFalse(restored[CollectionType.Drivers].RunOnServiceStart);
        }

        [TestMethod]
        public void LegacyDriversWMI_Key_MapsToDrivers()
        {
            // A config saved before the DriversWMI -> Drivers rename.
            const string legacyJson = "{\"DriversWMI\":{\"Schedule\":\"0 0 22 * * ?\",\"RunOnServiceStart\":false}}";

            var restored = JsonConvert.DeserializeObject<CollectionSchedules>(legacyJson)!;

            Assert.IsTrue(restored.ContainsKey(CollectionType.Drivers), "Legacy DriversWMI schedule should map to Drivers");
            Assert.AreEqual("0 0 22 * * ?", restored[CollectionType.Drivers].Schedule);
        }

        [TestMethod]
        public void UnknownCollectionType_IsSkipped_NotThrown()
        {
            // A removed/renamed collection type must not fail the whole config load.
            const string json = "{\"SomeRemovedType\":{\"Schedule\":\"0 0 22 * * ?\"},\"CPU\":{\"Schedule\":\"0 0/1 * * * ?\"}}";

            var restored = JsonConvert.DeserializeObject<CollectionSchedules>(json)!;

            Assert.AreEqual(1, restored.Count);
            Assert.IsTrue(restored.ContainsKey(CollectionType.CPU));
        }
    }
}
