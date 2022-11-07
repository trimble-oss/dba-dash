using Azure.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADash
{
    internal static class CollectionCommandTimeout
    {
        private static readonly Dictionary<CollectionType, int> collectionCommandTimeouts = new() {
            { CollectionType.DatabasePermissions,300 },
            { CollectionType.DatabasePrincipals,300 },
            { CollectionType.DatabaseRoleMembers,300 },
            { CollectionType.IdentityColumns,300 },
            { CollectionType.SlowQueries, 90 }
        };
        public static int DefaultCommandTimeout = 60;
      
         public static int GetCommandTimeout(this CollectionType type)
        {
            return collectionCommandTimeouts.TryGetValue(type, out var value) ? value : DefaultCommandTimeout;
        }
    }
}
