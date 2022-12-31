using System;
using System.Collections.Concurrent;

namespace DBADashService
{
    // http://johnculviner.com/achieving-named-lock-locker-functionality-in-c-4-0/
    public class NamedLocker
    {
        private readonly ConcurrentDictionary<string, object> _lockDict = new();

        //get a lock for use with a lock(){} block
        public object GetLock(string name)
        {
            return _lockDict.GetOrAdd(name, s => new object());
        }

        //run a short lock inline using a lambda
        public TResult RunWithLock<TResult>(string name, Func<TResult> body)
        {
            lock (_lockDict.GetOrAdd(name, s => new object()))
                return body();
        }

        //run a short lock inline using a lambda
        public void RunWithLock(string name, Action body)
        {
            lock (_lockDict.GetOrAdd(name, s => new object()))
                body();
        }

        //remove an old lock object that is no longer needed
        public void RemoveLock(string name)
        {
            _lockDict.TryRemove(name, out _);
        }
    }
}