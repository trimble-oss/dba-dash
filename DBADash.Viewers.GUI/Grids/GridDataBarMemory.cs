using DBADashGUI.CustomReports;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DBADashGUI.Grids
{
    /// <summary>
    /// Remembers the data bars added to a result grid, so the same query's results get them back the next time they're
    /// opened - from SSMS, or from a file.  "The same" is the shape of the result set: its columns' names and types, in
    /// order.  A query that returns other columns is a different grid, whatever it's called, and starts without bars.
    ///
    /// A cache rather than a setting: a file per shape in local app data, named for its <see cref="Signature"/>.  Opening
    /// a grid reads only its own file and a change writes only that one, so copies of the app open at once - an SSMS
    /// viewer and the GUI, say - share what's remembered and only clash over the same grid at the same moment, when the
    /// last to save wins.  A file's modified time is when the shape was last used, and only the most recently used
    /// <see cref="MaxRemembered"/> are kept.  Losing the cache, or a file in it, only loses the bars.
    /// </summary>
    public static class GridDataBarMemory
    {
        public const int MaxRemembered = 100;

        private const string Extension = ".json";

        /// <summary>Where the files are kept.  Settable so tests can use a folder of their own.</summary>
        public static string Folder { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DBADash", "GridDataBars");

        /// <summary>
        /// What identifies a result set's shape: each column's name and type, in order.  The SQL type where the SSMS
        /// extension recorded it - nvarchar(50) and nvarchar(max) are told apart - otherwise the .NET type.  Hashed, as
        /// a result set can have a lot of columns and this names a file.
        /// </summary>
        public static string Signature(DataTable table)
        {
            var shape = string.Join("\n", table.Columns.Cast<DataColumn>()
                .Select(c => c.ColumnName + "\t" + (c.ExtendedProperties["SqlType"] as string ?? c.DataType.FullName)));
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(shape)));
        }

        private static string PathFor(DataTable table) => Path.Combine(Folder, Signature(table) + Extension);

        /// <summary>The data bars remembered for result sets shaped like <paramref name="table"/>, or none.</summary>
        public static IReadOnlyDictionary<string, DataBarSettings> Recall(DataTable table)
        {
            var path = PathFor(table);
            try
            {
                if (!File.Exists(path)) return new Dictionary<string, DataBarSettings>();

                var dataBars = JsonConvert.DeserializeObject<Dictionary<string, DataBarSettings>>(File.ReadAllText(path));

                // Used now, so it's the last to be pruned.
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
                return dataBars ?? new Dictionary<string, DataBarSettings>();
            }
            catch (Exception ex)
            {
                // A file that can't be read loses the remembered bars, not the grid.
                Log.Warning(ex, "Unable to read the remembered grid data bars from {path}", path);
                return new Dictionary<string, DataBarSettings>();
            }
        }

        /// <summary>
        /// Remember <paramref name="dataBars"/> for result sets shaped like <paramref name="table"/> - or, where there
        /// are none, forget the shape.
        /// </summary>
        public static void Remember(DataTable table, IReadOnlyDictionary<string, DataBarSettings> dataBars)
        {
            var path = PathFor(table);
            try
            {
                if (dataBars.Count == 0)
                {
                    // Checked first, as File.Delete throws where the folder isn't there either.
                    if (File.Exists(path)) File.Delete(path);
                    return;
                }

                Directory.CreateDirectory(Folder);

                // Written to a name of its own and moved into place, so a crash part way through can't leave half a
                // file, and two copies of the app saving the same grid don't write into each other's temporary file.
                var temp = Path.Combine(Folder, $"{Path.GetFileNameWithoutExtension(path)}.{Guid.NewGuid():N}.tmp");
                File.WriteAllText(temp, JsonConvert.SerializeObject(dataBars, Formatting.Indented));
                File.Move(temp, path, overwrite: true);

                Prune();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to save the remembered grid data bars to {path}", path);
            }
        }

        /// <summary>
        /// Delete all but the most recently used <see cref="MaxRemembered"/> shapes, and any temporary file a save that
        /// crashed part way through left behind.
        /// </summary>
        private static void Prune()
        {
            var folder = new DirectoryInfo(Folder);
            var stale = folder.GetFiles("*" + Extension)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Skip(MaxRemembered)
                // An hour old, so it isn't one another copy of the app is writing now.
                .Concat(folder.GetFiles("*.tmp").Where(f => f.LastWriteTimeUtc < DateTime.UtcNow.AddHours(-1)));
            foreach (var file in stale)
            {
                try
                {
                    file.Delete();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // In use by another copy of the app - it goes next time.
                }
            }
        }
    }
}
