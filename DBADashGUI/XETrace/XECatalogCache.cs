using DBADash.XE;
using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Linq;

namespace DBADashGUI.XETrace
{
    /// <summary>
    /// On-disk cache of the XE object catalog, one JSON file per SQL build + edition, under the app data folder.
    /// The catalog is effectively static for a given build/edition, so persisting it avoids re-querying the monitored
    /// instance (and the Service Broker round-trip) on every app restart.  Sits behind the in-memory cache in
    /// <see cref="XETraceController"/>; all failures are swallowed (the caller just re-queries).
    /// </summary>
    internal static class XECatalogCache
    {
        // Bump when the serialized shape of XEObjectCatalog changes, so stale-format files are ignored (not loaded).
        private const int FormatVersion = 1;

        // Cap the number of cached builds so old versions don't accumulate forever (least-recently-used pruned).
        private const int MaxCachedFiles = 50;

        private static string CacheDir
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DBADashGUI", "XECatalogCache");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        /// <summary>Loads the cached catalog for a key, or null if not cached / unreadable.</summary>
        public static XEObjectCatalog TryLoad(string key)
        {
            try
            {
                var path = PathFor(key);
                if (!File.Exists(path)) return null;
                var catalog = JsonConvert.DeserializeObject<XEObjectCatalog>(File.ReadAllText(path));
                if (catalog is not { Events: { Count: > 0 } }) return null;
                try { File.SetLastAccessTimeUtc(path, DateTime.UtcNow); } catch { /* touch for LRU only */ }
                return catalog;
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Unable to read XE catalog cache for {key}", key);
                return null;
            }
        }

        /// <summary>Persists the catalog for a key (no-op for an empty catalog).</summary>
        public static void Save(string key, XEObjectCatalog catalog)
        {
            if (catalog is not { Events: { Count: > 0 } }) return;
            try
            {
                var path = PathFor(key);
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, JsonConvert.SerializeObject(catalog));
                File.Copy(tmp, path, overwrite: true); // write-then-replace so a crash can't leave a half file
                File.Delete(tmp);
                Prune();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Unable to write XE catalog cache for {key}", key);
            }
        }

        private static string PathFor(string key) =>
            Path.Combine(CacheDir, $"v{FormatVersion}_{Sanitize(key)}.json");

        private static string Sanitize(string key)
        {
            key = key.Replace('|', '_');
            return Path.GetInvalidFileNameChars().Aggregate(key, (current, c) => current.Replace(c, '_'));
        }

        private static void Prune()
        {
            try
            {
                var files = new DirectoryInfo(CacheDir).GetFiles("*.json");
                if (files.Length <= MaxCachedFiles) return;
                foreach (var f in files.OrderByDescending(f => f.LastAccessTimeUtc).Skip(MaxCachedFiles))
                {
                    try { f.Delete(); } catch { /* best-effort */ }
                }
            }
            catch { /* best-effort */ }
        }
    }
}
