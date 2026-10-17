using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;

namespace DBADash
{
    /// <summary>
    /// A small JSON file beside the service holding runtime state that has to survive a restart but isn't config - see
    /// <see cref="CollectionPauseStore"/> and <see cref="TemporaryScheduleStore"/>.  Reading never fails the caller: a
    /// missing or unreadable file reads as nothing.  Writing replaces the file in one step, so a failure part way
    /// through can't leave it truncated, and an empty state removes it.
    /// </summary>
    internal static class JsonStateFile
    {
        /// <summary>The state in <paramref name="path"/>, or null if there's no file or it can't be read (logged).</summary>
        public static T Read<T>(string path, string description) where T : class
        {
            try
            {
                return File.Exists(path) ? JsonConvert.DeserializeObject<T>(File.ReadAllText(path)) : null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error reading {description} from {file}.  It will be ignored", description, path);
                return null;
            }
        }

        /// <summary>Writes <paramref name="state"/> to <paramref name="path"/>, or removes the file when <paramref name="isEmpty"/>.  Throws if it can't be written.</summary>
        public static void Write<T>(string path, T state, bool isEmpty)
        {
            if (isEmpty)
            {
                if (File.Exists(path)) File.Delete(path);
                return;
            }
            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, JsonConvert.SerializeObject(state, Formatting.Indented));
            File.Move(tempPath, path, overwrite: true);
        }
    }
}
