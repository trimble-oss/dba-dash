using AsyncKeyedLock;
using DBADash;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashService
{
    public sealed class DirectoryWorkItem : IWorkItem
    {
        private static readonly AsyncKeyedLocker<string> _folderLocker = new();
        private const uint ERROR_SHARING_VIOLATION = 0x80070020;
        private static readonly TimeSpan StaleTempFileAge = TimeSpan.FromHours(1);

        /// <summary>
        /// How long a file that's in use is waited for before the files behind it are processed without it.
        /// A file still being written is only locked for as long as the write takes, so a lock held for
        /// longer is something else - another service importing from the same folder, or a stuck handle -
        /// and waiting on it indefinitely would stall every file behind it.
        /// </summary>
        private static readonly TimeSpan LockedFileWaitLimit = TimeSpan.FromMinutes(5);

        /// <summary>
        /// When a file that's in use was first seen locked, keyed on source folder and then on file path.  The
        /// wait is bounded from that point rather than from the file's last write time: a file can sit in the
        /// folder long before it's imported - a backlog built up while the service was down, for example - so
        /// its age says nothing about how long the lock has been held, and bounding on it would skip a whole
        /// backlog on the first momentary lock.
        /// Tracking is held per folder so a pass over one folder only ever prunes its own entries.  Each
        /// folder's entries are only read and written while the lock for that folder is held.
        /// </summary>
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, DateTime>> LockedFilesFirstSeenByFolder = new(StringComparer.OrdinalIgnoreCase);

        public DBADashSource Source { get; set; }

        public string DedupKey => Source.ConnectionString;

        public string Schedule { get; set; }

        public WorkItemPriority Priority { get; set; } = WorkItemPriority.Normal;

        public string Description => $"Import from {Source.ConnectionString}";

        public async Task ExecuteAsync(CollectionConfig config, CancellationToken cancellationToken)
        {
            var folder = Source.GetSource();
            Log.Logger.Information("Import from folder {folder}", folder);
            if (!Directory.Exists(folder))
            {
                Log.Error("Source directory doesn't exist {folder}", folder);
                return;
            }

            // One job per folder at a time
            using (await _folderLocker.LockAsync(Source.ConnectionString).ConfigureAwait(false))
            {
                List<string> allFiles;
                try
                {
                    allFiles = Directory.EnumerateFiles(folder, DestinationHandling.FileSearchPattern, SearchOption.TopDirectoryOnly)
                                        .ToList();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Enumerate files {folder}", folder);
                    return;
                }
                DeleteStaleTempFiles(allFiles);
                var lockedFilesFirstSeen = LockedFilesFirstSeenByFolder.GetOrAdd(folder, _ => new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase));
                PruneLockTracking(lockedFilesFirstSeen, allFiles);
                var files = allFiles.Where(DestinationHandling.IsCompletedFileName).ToList();

                var filesByInstance = GetFilesToProcessByInstance(files);
                var tasks = filesByInstance.Select(kv => ProcessInstanceFilesAsync(kv.Value, Source, config, lockedFilesFirstSeen, cancellationToken)).ToList();
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Process a given list of files in order for a specific instance, writing collected data to the DBADash repository database
        /// </summary>
        private static async Task ProcessInstanceFilesAsync(List<string> files, DBADashSource source, CollectionConfig config, ConcurrentDictionary<string, DateTime> lockedFilesFirstSeen, CancellationToken cancellationToken)
        {
            files.Sort(); // Ensure we process files in order
            foreach (var f in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await ProcessFileAsync(f, source, config, lockedFilesFirstSeen);
                if (result == FileResult.Imported)
                {
                    TryDeleteFile(f);
                }
                else if (result == FileResult.Retry)
                {
                    // The file still holds data to import.  Some collections discard a snapshot older than
                    // the one they last imported, so the files behind it wait for the next iteration rather
                    // than overtaking it.
                    Log.Information("Stopping at {FileName}.  The files behind it are processed on the next iteration", Path.GetFileName(f));
                    break;
                }
            }
        }

        /// <summary>
        /// What the caller does with a file after an import attempt.
        /// </summary>
        private enum FileResult
        {
            /// <summary>Imported.  The file can be deleted.</summary>
            Imported,

            /// <summary>Nothing more to do with it in this pass.  The file was quarantined, no longer exists, or is locked by something that isn't writing it.</summary>
            Skip,

            /// <summary>Not imported and still holding data to import.  Leave it and stop, so it keeps its place in the order.</summary>
            Retry
        }

        /// <summary>
        /// Import a single file.  A file that can't be imported is quarantined in the failed message folder
        /// rather than left in place - files are imported in order, so leaving it would block every file
        /// behind it for this instance indefinitely.
        /// </summary>
        private static async Task<FileResult> ProcessFileAsync(string f, DBADashSource source, CollectionConfig config, ConcurrentDictionary<string, DateTime> lockedFilesFirstSeen, int tryCount = 1)
        {
            const int MaxTryCount = 5;
            const int RetryDelay = 10;
            Log.Information("Processing file {0}", f);
            var fileName = Path.GetFileName(f);
            try
            {
                var ds = DataSetSerialization.DeserializeFromFile(f);
                var id = GetID(ds);
                using (await Locker.AsyncLocker.LockAsync(id))
                {
                    await DestinationHandling.WriteAllDestinationsAsync(ds, source, fileName, config);
                }
                lockedFilesFirstSeen.TryRemove(f, out _);
                return FileResult.Imported;
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException) // Deleted after the folder was listed.  e.g. Another service importing from the same folder.
            {
                Log.Information("File {FileName} no longer exists.  Skipping", fileName);
                lockedFilesFirstSeen.TryRemove(f, out _);
                return FileResult.Skip;
            }
            catch (IOException ex) when ((uint)ex.HResult == ERROR_SHARING_VIOLATION) // Another process has a lock on the file.  It might still be being written to.
            {
                if (tryCount > MaxTryCount)
                {
                    // Leave the file in place either way - it hasn't been imported.  While the lock is young
                    // enough that the file is plausibly still being written, the files behind it wait for it
                    // to keep them in order.  Past that the lock is something else and they're processed
                    // without it.
                    var firstSeenLocked = lockedFilesFirstSeen.GetOrAdd(f, _ => DateTime.UtcNow);
                    if (firstSeenLocked > DateTime.UtcNow.Subtract(LockedFileWaitLimit))
                    {
                        Log.Warning("File {FileName} is in use.  Exceeded max wait/retry.  File will be processed on the next iteration", fileName);
                        return FileResult.Retry;
                    }
                    Log.Warning("File {FileName} has been in use since {FirstSeenLocked} UTC.  Continuing with the files behind it", fileName, firstSeenLocked);
                    return FileResult.Skip;
                }
                Log.Information("File {FileName} is in use.  Waiting for lock to release. Attempt {TryCount}/{MaxRetryCount}", fileName, tryCount, MaxTryCount);
                await Task.Delay(RetryDelay);
                return await ProcessFileAsync(f, source, config, lockedFilesFirstSeen, tryCount + 1);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error importing from {filename}.  File will be copied to {failedMessageFolder}", fileName, SchedulerServiceConfig.FailedMessageFolder);
                // A quarantined file can never be imported, so the files behind it aren't overtaking anything
                // by continuing.  One that's still in the source folder is imported again on the next
                // iteration, so they wait for it instead.
                return QuarantineFile(f, lockedFilesFirstSeen) ? FileResult.Skip : FileResult.Retry;
            }
        }

        internal static string GetID(DataSet ds)
        {
            try
            {
                return ds.Tables["DBADash"].Rows[0]["Instance"] + "_" + ds.Tables["DBADash"].Rows[0]["DBName"];
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error getting ID from DataSet");
                return "DEFAULT";
            }
        }

        /// <summary>
        /// Parse Instance from filename.  File format is DBADash_YYYYMMDD_HHMM_SS_{InstanceName}_{random}.xml
        /// </summary>
        public static string ParseInstance(string fileName)
        {
            return fileName[25..fileName.LastIndexOf('_')];
        }

        /// <summary>
        /// Split file list by Instance parsed from the filename.  Each instance will have 1 item in the dictionary containing a list of files to process for that instance
        /// </summary>
        internal static Dictionary<string, List<string>> GetFilesToProcessByInstance(List<string> files)
        {
            Dictionary<string, List<string>> filesToProcessByInstance = new();
            foreach (var path in files)
            {
                string instance;
                try
                {
                    instance = ParseInstance(Path.GetFileName(path));
                }
                catch (Exception ex)
                {
                    instance = "default";
                    Log.Warning("Unable to parse Instance from {0}: {1}", path, ex.Message);
                }
                if (filesToProcessByInstance.TryGetValue(instance, out var value))
                {
                    value.Add(path);
                }
                else
                {
                    filesToProcessByInstance.Add(instance, new List<string> { path });
                }
            }
            return filesToProcessByInstance;
        }

        /// <summary>
        /// Take a file that couldn't be imported out of the source folder so it doesn't block the files queued
        /// behind it, keeping a copy in the failed message folder for 7 days (SchedulerService.FolderCleanup).
        /// Returns false if the file is still in the source folder afterwards, as it's then imported again on
        /// the next iteration and the files behind it can't be allowed to overtake it.
        /// </summary>
        private static bool QuarantineFile(string filePath, ConcurrentDictionary<string, DateTime> lockedFilesFirstSeen)
        {
            var fileName = Path.GetFileName(filePath);
            try
            {
                if (string.IsNullOrEmpty(SchedulerServiceConfig.FailedMessageFolder))
                {
                    throw new Exception("Failed message folder is not available");
                }
                // Note: Path.GetFileName is required here.  filePath is rooted and Path.Combine returns a rooted
                // second argument unchanged, which would make this a copy of the file over itself.
                // Copy rather than move: the source folder is often a file share and the failed message folder
                // is always local to the service, and File.Move has volume restrictions that a copy doesn't.
                FailedMessageFolder.Copy(filePath, SchedulerServiceConfig.FailedMessageFolder, fileName);
                if (!TryDeleteFile(filePath))
                {
                    Log.Warning("{FileName} has been copied to the failed message folder but couldn't be removed from the source folder.  The import will be attempted again on the next iteration", fileName);
                    return false;
                }
                lockedFilesFirstSeen.TryRemove(filePath, out _);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error copying {FileName} to failed message folder {failedMessageFolder}.  The file will be left in place and retried", fileName, SchedulerServiceConfig.FailedMessageFolder);
                return false;
            }
        }

        /// <summary>
        /// Temp files are renamed to .xml once the write completes (see DestinationHandling.WriteFolderAsync).
        /// A service killed mid-write leaves one behind that will never be renamed, so remove it once it's old
        /// enough that it can't still be in the process of being written.
        /// </summary>
        private static void DeleteStaleTempFiles(IEnumerable<string> files)
        {
            try
            {
                var cutOff = DateTime.UtcNow.Subtract(StaleTempFileAge);
                foreach (var f in files.Where(f => f.EndsWith(DestinationHandling.TempFileExtension)
                                                   && IsLastWrittenBefore(f, cutOff)))
                {
                    Log.Warning("Deleting incomplete file {FileName}", Path.GetFileName(f));
                    TryDeleteFile(f);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting incomplete files");
            }
        }

        /// <summary>
        /// Whether the file was last written before the cut off.  A file whose last write time can't be read
        /// isn't treated as old - the caller deletes what's old, and deleting a file we know nothing about is
        /// worse than leaving it.  Reading the timestamp doesn't throw, so one unreadable file doesn't stop
        /// the rest from being cleaned up.
        /// </summary>
        private static bool IsLastWrittenBefore(string filePath, DateTime cutOffUtc)
        {
            try
            {
                return File.GetLastWriteTimeUtc(filePath) < cutOffUtc;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error reading the last write time of {FileName}.  It won't be deleted", Path.GetFileName(filePath));
                return false;
            }
        }

        /// <summary>
        /// Drop lock tracking for files that have left the source folder - imported by another service, or
        /// removed by hand.  Tracking has to survive the passes that skip a locked file, so it can't be
        /// cleared on the way past.
        /// Pruned against the folder listing this pass is working from rather than by testing each tracked
        /// path.  A folder that can't be listed doesn't get a pass at all, so a file that's only unreachable
        /// isn't mistaken for one that's gone and given a fresh wait every time the folder drops out.
        /// </summary>
        private static void PruneLockTracking(ConcurrentDictionary<string, DateTime> lockedFilesFirstSeen, List<string> folderFiles)
        {
            if (lockedFilesFirstSeen.IsEmpty) return;
            var present = folderFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var f in lockedFilesFirstSeen.Keys.Where(f => !present.Contains(f)))
            {
                lockedFilesFirstSeen.TryRemove(f, out _);
            }
        }

        /// <summary>
        /// Delete a file.  Returns false if it's still there afterwards.
        /// </summary>
        private static bool TryDeleteFile(string filePath)
        {
            if (!File.Exists(filePath)) return true;
            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting file {FileName}", Path.GetFileName(filePath));
                return false;
            }
        }
    }
}