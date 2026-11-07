using System;
using System.Collections.Generic;
using System.Linq;

namespace DBADashGUI
{
    /// <summary>
    /// The tags an alert rule applies to.  No tags = all instances.
    /// Tags with different names must all match (AND).  Tags with the same name are alternatives (OR).
    /// e.g. (Role:Production OR Role:UAT) AND Region:APAC
    /// Stored as Alert.Rules.ApplyToTagID (-1 all, a single TagID, or -2 for multiple) + ApplyToTagIDs.
    /// </summary>
    public class DBADashTagSet
    {
        public const int MultipleTagsID = -2;

        public List<DBADashTag> Tags { get; init; } = new();

        public static DBADashTagSet AllInstances() => new();

        public static DBADashTagSet FromTag(DBADashTag tag) => tag == null || tag.TagID <= 0 ? AllInstances() : new DBADashTagSet { Tags = { tag } };

        public bool IsAllInstances => !DistinctTags.Any();

        private IEnumerable<DBADashTag> DistinctTags => Tags.Where(t => t is { TagID: > 0 }).GroupBy(t => t.TagID).Select(g => g.First());

        /// <summary>Value for the ApplyToTagID column: -1 for all instances, the TagID for 1 tag or -2 for multiple</summary>
        public int ApplyToTagID
        {
            get
            {
                var tags = DistinctTags.ToList();
                return tags.Count switch
                {
                    0 => -1,
                    1 => tags[0].TagID,
                    _ => MultipleTagsID
                };
            }
        }

        /// <summary>Sorted, comma-separated TagIDs</summary>
        public string TagIDs => string.Join(",", DistinctTags.Select(t => t.TagID).OrderBy(id => id));

        /// <summary>
        /// Build from the TagIDs stored on the rule.  A tag that no longer exists is kept as a placeholder so saving the rule doesn't widen it.
        /// </summary>
        public static DBADashTagSet FromTagIDs(IEnumerable<int> tagIDs, IEnumerable<DBADashTag> allTags)
        {
            var lookup = allTags.Where(t => t.TagID > 0).GroupBy(t => t.TagID).ToDictionary(g => g.Key, g => g.First());
            return new DBADashTagSet
            {
                Tags = tagIDs.Where(id => id > 0).Distinct()
                    .Select(id => lookup.TryGetValue(id, out var tag) ? tag : new DBADashTag { TagID = id })
                    .ToList()
            };
        }

        /// <summary>Load the tag set stored on a rule or blackout period (ApplyToTagIDs) from the repository</summary>
        public static DBADashTagSet Load(string connectionString, string tagIDs)
        {
            var ids = ParseTagIDs(tagIDs).ToList();
            return FromTagIDs(ids, DBADashTag.GetTagsByID(connectionString, ids));
        }

        public static IEnumerable<int> ParseTagIDs(string tagIDs) =>
            (tagIDs ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0);

        private static bool IsMissing(DBADashTag tag) => tag.TagName == null && tag.TagValue == null;

        private static string GroupKey(DBADashTag tag) => IsMissing(tag) ? "#" + tag.TagID : tag.TagName ?? string.Empty;

        private static string TagText(DBADashTag tag) => IsMissing(tag) ? $"{{Deleted tag {tag.TagID}}}" : tag.TagName + ":" + tag.TagValue;

        /// <summary>The tag groups: an instance needs at least 1 tag from every group</summary>
        public IEnumerable<IGrouping<string, DBADashTag>> Groups =>
            DistinctTags.GroupBy(GroupKey, StringComparer.OrdinalIgnoreCase).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        public override string ToString()
        {
            if (IsAllInstances) return "{All}";
            return string.Join(" AND ", Groups.Select(g =>
            {
                var values = g.Select(TagText).OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
                return values.Count == 1 ? values[0] : "(" + string.Join(" OR ", values) + ")";
            }));
        }
    }
}
