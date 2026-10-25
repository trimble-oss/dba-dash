using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash
{
    /// <summary>
    /// The collections running for each source, so a change that drops a source's extended events sessions can wait for
    /// them first.  A collection that started with the old configuration - before the source was removed, or before slow
    /// query capture was switched off - would otherwise recreate a session after it had been dropped, and nothing would
    /// remove it again until the service restarts.
    ///
    /// <para>A collection registers (<see cref="Begin"/>) before it checks that its source is still collected, and a
    /// change removes or reconfigures the source before it waits (<see cref="WaitForIdleAsync"/>).  So either the wait sees
    /// the collection, or the collection sees the change.</para>
    /// </summary>
    public static class CollectionActivity
    {
        private static readonly ConcurrentDictionary<string, int> Running = new(StringComparer.Ordinal);

        /// <summary>The key a source is tracked by - its connection string, which every copy of the source shares.</summary>
        public static string GetKey(DBADashSource source) => source.ConnectionString;

        /// <summary>Records a collection for <paramref name="source"/> as running until the result is disposed.</summary>
        public static IDisposable Begin(DBADashSource source)
        {
            var key = GetKey(source);
            Running.AddOrUpdate(key, 1, (_, count) => count + 1);
            return new Registration(key);
        }

        /// <summary>True if a collection is running for any of <paramref name="sources"/>.</summary>
        public static bool IsRunning(IEnumerable<DBADashSource> sources) =>
            sources.Any(s => Running.TryGetValue(GetKey(s), out var count) && count > 0);

        /// <summary>
        /// Waits until no collection is running for <paramref name="sources"/>, or <paramref name="timeout"/> passes.
        /// Returns false on a timeout.
        /// </summary>
        public static async Task<bool> WaitForIdleAsync(IReadOnlyCollection<DBADashSource> sources, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            while (IsRunning(sources))
            {
                if (sw.Elapsed >= timeout) return false;
                await Task.Delay(250, cancellationToken);
            }
            return true;
        }

        private sealed class Registration : IDisposable
        {
            private string key;

            public Registration(string key) => this.key = key;

            public void Dispose()
            {
                var k = Interlocked.Exchange(ref key, null);
                if (k == null) return;
                // Removed when the count reaches zero, so the dictionary doesn't grow with removed sources
                while (true)
                {
                    if (!Running.TryGetValue(k, out var count)) return;
                    if (count <= 1)
                    {
                        if (Running.TryRemove(new KeyValuePair<string, int>(k, count))) return;
                    }
                    else if (Running.TryUpdate(k, count - 1, count))
                    {
                        return;
                    }
                }
            }
        }
    }
}
