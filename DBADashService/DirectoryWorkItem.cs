using AsyncKeyedLock;
using DBADash;
using Serilog;
using System;
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
                List<string> files;
                try
                {
                    files = Directory.EnumerateFiles(folder, DestinationHandling.FileSearchPattern, SearchOption.TopDirectoryOnly)
                                     .Where(f => f.EndsWith(DestinationHandling.FileExtension))
                                     .ToList();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Enumerate files {folder}", folder);
                    return;
                }

                var filesByInstance = GetFilesToProcessByInstance(files);
                var tasks = filesByInstance.Select(kv => ProcessInstanceFilesAsync(kv.Value, Source, config, cancellationToken)).ToList();
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Process a given list of files in order for a specific instance, writing collected data to the DBADash repository database
        /// </summary>
        private static async Task ProcessInstanceFilesAsync(List<string> files, DBADashSource source, CollectionConfig config, CancellationToken cancellationToken)
        {
            files.Sort(); // Ensure we process files in order
            foreach (var f in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessFileAsync(f, source, config);
                TryDeleteFile(f);
            }
        }

        private static async Task ProcessFileAsync(string f, DBADashSource source, CollectionConfig config, int tryCount = 1)
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
            }
            catch (IOException ex) when ((uint)ex.HResult == ERROR_SHARING_VIOLATION) // Another process has a lock on the file.  It might still be being written to.
            {
                if (tryCount > MaxTryCount)
                {
                    Log.Warning("File {FileName} is in use.  Exceeded max wait/retry.  File will be processed on the next iteration", fileName);
                    return;
                }
                Log.Information("File {FileName} is in use.  Waiting for lock to release. Attempt {TryCount}/{MaxRetryCount}", fileName, tryCount, MaxTryCount);
                await Task.Delay(RetryDelay);
                await ProcessFileAsync(f, source, config, tryCount + 1);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error importing from {filename}.  File will be copied to {failedMessageFolder}", fileName, SchedulerServiceConfig.FailedMessageFolder);
                File.Copy(f, Path.Combine(SchedulerServiceConfig.FailedMessageFolder, f));
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

        private static void TryDeleteFile(string filePath)
        {
            if (!File.Exists(filePath)) return;
            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error deleting file");
            }
        }
    }
}