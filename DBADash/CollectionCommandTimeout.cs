using System.Collections.Generic;

namespace DBADash
{
    internal static class CollectionCommandTimeout
    {
        private static readonly Dictionary<CollectionType, int> collectionCommandTimeouts = new() {
            { CollectionType.DatabasePermissions,900 },
            { CollectionType.DatabasePrincipals,900 },
            { CollectionType.DatabaseRoleMembers,900 },
            { CollectionType.IdentityColumns,900 },
            { CollectionType.SlowQueries, 90 }
        };

        public static int DefaultCommandTimeout = 60;

        public static int GetCommandTimeout(this CollectionType type)
        {
            return collectionCommandTimeouts.TryGetValue(type, out var value) ? value : DefaultCommandTimeout;
        }
    }
}