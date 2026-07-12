using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace DBADash
{
    /// <summary>
    /// Maps <see cref="CollectionType"/> member names that have been renamed to their current member, so
    /// configuration saved before a rename (custom schedules, command timeouts) still loads and keeps any
    /// customization instead of silently reverting to defaults.
    /// </summary>
    public static class CollectionTypeLegacyNames
    {
        private static readonly Dictionary<string, CollectionType> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            // "DriversWMI" was renamed to Drivers - the name now reflects the data collected rather than the
            // collection method (WMI), so it survives a future change of method.
            { "DriversWMI", CollectionType.Drivers }
        };

        /// <summary>
        /// Parse a CollectionType name, honoring legacy renames.  Returns false for names that match neither
        /// a current member nor a known legacy name.
        /// </summary>
        public static bool TryParse(string name, out CollectionType type)
        {
            return Map.TryGetValue(name, out type) || Enum.TryParse(name, ignoreCase: true, out type);
        }

        /// <summary>
        /// Rewrite any legacy CollectionType names used as property keys in <paramref name="obj"/> to their
        /// current names so it can be deserialized after a member rename.  If a property already exists under
        /// the current name it wins and the legacy one is dropped.
        /// </summary>
        public static void RemapLegacyKeys(JObject obj)
        {
            if (obj == null) return;
            foreach (var (legacyName, currentType) in Map)
            {
                var legacyProperty = obj.Property(legacyName, StringComparison.OrdinalIgnoreCase);
                if (legacyProperty == null) continue;

                var currentName = currentType.ToString();
                if (obj.Property(currentName, StringComparison.OrdinalIgnoreCase) == null)
                {
                    obj.Add(currentName, legacyProperty.Value);
                }
                legacyProperty.Remove();
            }
        }
    }
}
