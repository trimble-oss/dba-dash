using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash
{
    /// <summary>
    /// The folder the service keeps copies of collections and imports that it couldn't write to their
    /// destination, so they can be re-imported once the cause is fixed.  Every write to it goes through here
    /// so the folder is capped at <see cref="MaxFiles"/>: a repeating failure (e.g. a repository that's missing a
    /// schema change) otherwise writes one file per collection without limit - 300K files / 100GB in one case.
    /// </summary>
    /// <remarks>
    /// When the folder is full, the oldest files are deleted to make room for the newest.  The newest failures
    /// are the ones most likely to be relevant, and deleting rather than refusing the write means a file that
    /// has to leave an import folder (see DirectoryWorkItem) can always be moved out of it.
    /// </remarks>
    public static class FailedMessageFolder
    {
        /// <summary>Default maximum number of files kept in the folder.</summary>
        public const int DefaultMaxFiles = 1000;

        /// <summary>Prefix of the files written to the folder - the same as the files the service imports.</summary>
        public const string FileNamePrefix = "dbadash";

        /// <summary>
        /// File name endings of the files counted towards <see cref="MaxFiles"/>, including temp files left by an interrupted write.
        /// Matched on the whole ending rather than <see cref="Path.GetExtension"/>, which gives ".gz" for ".xml.gz".
        /// </summary>
        private static readonly string[] Endings = { DestinationHandling.FileExtension, DestinationHandling.CompressedFileExtension, ".json", ".bin", DestinationHandling.TempFileExtension };

        /// <summary>Maximum number of files kept in the folder.</summary>
        public static int MaxFiles { get; set; } = DefaultMaxFiles;

        /// <summary>Serializes the make-room-then-write sequence, so concurrent writers can't each see room and overshoot the cap.</summary>
        private static readonly SemaphoreSlim Gate = new(1, 1);

        /// <summary>
        /// Writes a DataSet to the folder, making room first.  See <see cref="DestinationHandling.WriteFolderAsync"/>.
        /// Returns the path written, or null if the folder doesn't exist.  The name may differ from
        /// <paramref name="fileName"/> when the file is compressed.
        /// </summary>
        public static async Task<string> WriteAsync(DataSet ds, string folder, string fileName, CollectionConfig cfg)
        {
            await Gate.WaitAsync();
            try
            {
                MakeRoom(folder);
                return await DestinationHandling.WriteFolderAsync(ds, folder, fileName, cfg);
            }
            finally
            {
                Gate.Release();
            }
        }

        /// <summary>Copies an existing file into the folder, making room first.  The source file is left in place.</summary>
        public static void Copy(string sourcePath, string folder, string fileName)
        {
            Gate.Wait();
            try
            {
                MakeRoom(folder);
                File.Copy(sourcePath, Path.Combine(folder, fileName), true);
            }
            finally
            {
                Gate.Release();
            }
        }

        /// <summary>
        /// Deletes the oldest matching files until there's room for one more.  Files are only removed from the
        /// folder if it's over the cap, so a folder under the cap is never touched here.
        /// </summary>
        public static void MakeRoom(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                return;
            }

            var limit = Math.Max(1, MaxFiles);
            var files = new DirectoryInfo(folder).EnumerateFiles()
                .Where(IsManagedFile)
                .OrderBy(f => f.LastWriteTimeUtc)
                .ToList();

            var excess = files.Count - (limit - 1);
            if (excess <= 0)
            {
                return;
            }

            Log.Warning("Failed message folder {Folder} is at its limit of {MaxFiles} files.  Deleting the {Count} oldest to make room",
                folder, limit, excess);
            // Count deletions rather than taking the oldest 'excess' files: a file that can't be deleted (locked,
            // read-only) is skipped and the next oldest deleted in its place, so one stuck file can't disable the cap.
            var deleted = 0;
            foreach (var file in files)
            {
                if (deleted >= excess)
                {
                    break;
                }
                try
                {
                    file.Delete();
                    deleted++;
                }
                catch (Exception ex)
                {
                    // Still in use, or removed by someone else.  Move on to the next oldest.
                    Log.Warning(ex, "Error deleting {FileName} to make room in the failed message folder", file.FullName);
                }
            }
        }

        /// <summary>Whether the file is one this folder manages - a service-written file, or a temp file left by an interrupted write.</summary>
        public static bool IsManagedFile(FileInfo f) =>
            f.Name.StartsWith(FileNamePrefix, StringComparison.OrdinalIgnoreCase) &&
            Endings.Any(e => f.Name.EndsWith(e, StringComparison.OrdinalIgnoreCase));
    }
}
