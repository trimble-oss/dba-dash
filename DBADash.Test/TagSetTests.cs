using DBADashGUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace DBADash.Test
{
    [TestClass]
    public class TagSetTests
    {
        private static readonly List<DBADashTag> AllTags = new()
        {
            new DBADashTag { TagID = 1, TagName = "Role", TagValue = "Production" },
            new DBADashTag { TagID = 2, TagName = "Role", TagValue = "UAT" },
            new DBADashTag { TagID = 3, TagName = "Region", TagValue = "APAC" },
        };

        [TestMethod]
        public void NoTagsIsAllInstances()
        {
            var set = DBADashTagSet.AllInstances();
            Assert.IsTrue(set.IsAllInstances);
            Assert.AreEqual(-1, set.ApplyToTagID);
            Assert.AreEqual(string.Empty, set.TagIDs);
            Assert.AreEqual("{All}", set.ToString());
            Assert.IsTrue(DBADashTagSet.FromTag(DBADashTag.AllInstancesTag()).IsAllInstances);
        }

        [TestMethod]
        public void SingleTagUsesTagID()
        {
            var set = DBADashTagSet.FromTagIDs(new[] { 3, 3 }, AllTags);
            Assert.AreEqual(3, set.ApplyToTagID);
            Assert.AreEqual("3", set.TagIDs);
            Assert.AreEqual("Region:APAC", set.ToString());
        }

        [TestMethod]
        public void MultipleTagsAreSortedAndGrouped()
        {
            var set = DBADashTagSet.FromTagIDs(new[] { 3, 2, 1 }, AllTags);
            Assert.AreEqual(DBADashTagSet.MultipleTagsID, set.ApplyToTagID);
            Assert.AreEqual("1,2,3", set.TagIDs);
            Assert.AreEqual("Region:APAC AND (Role:Production OR Role:UAT)", set.ToString());
            Assert.AreEqual(2, set.Groups.Count());
        }

        [TestMethod]
        public void DeletedTagIsKept()
        {
            var set = DBADashTagSet.FromTagIDs(new[] { 1, 99 }, AllTags);
            Assert.AreEqual("1,99", set.TagIDs, "A deleted tag must be kept so saving the rule doesn't widen it");
            Assert.AreEqual("{Deleted tag 99} AND Role:Production", set.ToString());
        }

        [TestMethod]
        public void ParseIgnoresInvalidIDs()
        {
            CollectionAssert.AreEqual(new[] { 5, 3 }, DBADashTagSet.ParseTagIDs(" 5, x,,-1,3 ").ToList());
            Assert.AreEqual(0, DBADashTagSet.ParseTagIDs(null!).Count());
        }
    }
}
