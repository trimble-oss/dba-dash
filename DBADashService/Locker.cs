using AsyncKeyedLock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashService
{
    internal static class Locker
    {
        public static readonly AsyncKeyedLocker<string> AsyncLocker = new(new AsyncKeyedLockOptions(poolSize: 200));
    }
}