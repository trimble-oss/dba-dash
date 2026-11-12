using AsyncKeyedLock;
using DBADash;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace DBADashService
{
    /// <summary>
    /// Manages a work queue for parallel instance collection processing with configurable per-instance concurrency
    /// </summary>
    public class CollectionWorkQueue
    {
        /// <summary>A work item and when it was enqueued (<see cref="Stopwatch.GetTimestamp"/>) - for how long it waits for a worker</summary>
        private readonly record struct QueuedItem(IWorkItem Item, long EnqueuedTimestamp);

        private readonly Channel<QueuedItem> _highChannel;
        private readonly Channel<QueuedItem> _normalChannel;
        private readonly Channel<QueuedItem> _lowChannel;
        private readonly ConcurrentDictionary<string, WorkItemState> _instanceStates;
        private readonly AsyncKeyedLocker<string> _instanceLocker;
        private readonly CollectionConfig _config;
        private readonly int _workerCount;
        private readonly int _maxConcurrentCollectionsPerInstance;
        private Task[] _workers;
        private CancellationTokenSource _cts;
        private const int DEFAULT_MAX_CONCURRENT_COLLECTIONS_PER_INSTANCE = 3;
        private int _lowInProgress;
        private const int MIN_LOW_PRIORITY_POLL_MS = 10;
        private const int MAX_LOW_PRIORITY_POLL_MS = 40;

        // Default channel capacity per priority.  This can be overridden via CollectionConfig.
        // This should be large enough to avoid any backpressure in most deployments, with minimal memory usage.
        private const int DEFAULT_CHANNEL_CAPACITY = 5000;

        // Limit the number of concurrently running Low-priority items (to avoid starving higher priorities)
        private readonly int _lowMaxConcurrent;

        private readonly SemaphoreSlim _lowGate;

        // De-duplication: track pending/running work keys
        private readonly ConcurrentDictionary<string, byte> _pendingKeys = new(StringComparer.Ordinal);

        // First-pick probabilities (should sum to 1.0). Low gets 5% first pick.
        private readonly double _highFirst = 0.80;

        private readonly double _normalFirst = 0.15; // Low first = 1 - (high + normal) = 0.05

        // Tracking item count in high priority queue (waiting in channels or pending write)
        private int _queuedHigh;

        // Tracking item count in normal priority queue (waiting in channels or pending write)
        private int _queuedNormal;

        // Tracking item count in low priority queue (waiting in channels or pending write)
        private int _queuedLow;

        // For the service stats (TakeStats).  Workers processing an item now, and since the last TakeStats: the most at once,
        // items queued, enqueues skipped as a duplicate, items started and the time they waited for a worker.
        private int _busyWorkers;
        private int _maxBusyWorkers;
        private int _queued;
        private int _skipped;

        // Items started and their wait, updated and reset together under _startedLock, so an item's count and its wait are
        // never split across two samples
        private readonly object _startedLock = new();
        private int _started;
        private long _waitTotalMs;
        private long _waitMaxMs;

        public CollectionWorkQueue(CollectionConfig config, int? maxConcurrentCollectionsPerInstance = null)
        {
            _config = config;
            _workerCount = config.GetWorkerThreadCount();
            _maxConcurrentCollectionsPerInstance = maxConcurrentCollectionsPerInstance ?? DEFAULT_MAX_CONCURRENT_COLLECTIONS_PER_INSTANCE; // Default: allow up to 3 concurrent collections per instance
            _instanceStates = new ConcurrentDictionary<string, WorkItemState>();
            _instanceLocker = new AsyncKeyedLocker<string>(new AsyncKeyedLockOptions
            {
                MaxCount = _maxConcurrentCollectionsPerInstance // Semaphore-style: allow N concurrent operations per key
            });
            // Cap low-priority concurrency to 25% of workers (min 1)
            _lowMaxConcurrent = Convert.ToInt32(Math.Max(1, config.GetLowPriorityQueueMaxThreadPercentage() * _workerCount));
            Log.Information("Low-priority queue max concurrency set to {lowMaxConcurrent} ({percentage:P0} of total workers)", _lowMaxConcurrent, config.GetLowPriorityQueueMaxThreadPercentage());
            _lowGate = new SemaphoreSlim(_lowMaxConcurrent, _lowMaxConcurrent);

            var capacity = config.ChannelCapacity ?? DEFAULT_CHANNEL_CAPACITY;
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };
            _highChannel = Channel.CreateBounded<QueuedItem>(options);
            _normalChannel = Channel.CreateBounded<QueuedItem>(options);
            _lowChannel = Channel.CreateBounded<QueuedItem>(options);

            Log.Information("CollectionWorkQueue initialized with {workerCount} workers, channel capacity {capacity} ({default}) per priority, max {maxConcurrency} concurrent collections per instance",
                _workerCount, capacity, config.ChannelCapacity.HasValue ? "User configured" : "Default", config.SourceConnections.Count, _maxConcurrentCollectionsPerInstance);
        }

        public void Start(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _workers = new Task[_workerCount];

            for (int i = 0; i < _workerCount; i++)
            {
                var workerId = i;
                _workers[i] = Task.Run(async () => await WorkerAsync(workerId, _cts.Token), _cts.Token);
            }

            Log.Information("Started {workerCount} collection workers", _workerCount);
        }

        public async Task StopAsync()
        {
            var workers = _workers;
            if (workers == null || workers.Length == 0)
            {
                return;
            }

            // Cancel first to break out of waits/loops cleanly (idempotent-safe)
            var cts = Interlocked.Exchange(ref _cts, null);
            try
            {
                cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed/canceled elsewhere; ignore
            }

            // Complete writers to wake up any readers that aren't canceled yet
            _highChannel.Writer.TryComplete();
            _normalChannel.Writer.TryComplete();
            _lowChannel.Writer.TryComplete();

            try
            {
                await Task.WhenAll(workers);
            }
            finally
            {
                cts?.Dispose();
                _pendingKeys.Clear();
                Interlocked.Exchange(ref _queuedHigh, 0);
                Interlocked.Exchange(ref _queuedNormal, 0);
                Interlocked.Exchange(ref _queuedLow, 0);
                _workers = Array.Empty<Task>();
                Log.Information("All collection workers stopped");
            }
        }

        /// <summary>
        /// Enqueue work with priority. Returns false if channel is closed or duplicate detected.
        /// </summary>
        public async Task<bool> EnqueueAsync(IWorkItem item, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!string.IsNullOrEmpty(item.DedupKey))
                {
                    if (!_pendingKeys.TryAdd(item.DedupKey, 0))
                    {
                        Interlocked.Increment(ref _skipped);
                        Log.Warning("Skipping enqueue of {item}.  The previous scheduled collection is still enqueued or in progress.",
                            item.Description);
                        return false;
                    }
                }

                var writer = GetWriter(item.Priority);

                // Count as queued before write so we include backpressured writes
                IncrementPriorityQueued(item.Priority);

                // Timed from here, so a wait for space in the channel (backpressure) counts as waiting
                var queued = new QueuedItem(item, Stopwatch.GetTimestamp());

                // Fast-path: if there is capacity, write synchronously
                if (!writer.TryWrite(queued))
                {
                    // Fall back to async path (backpressure or racing with readers/writer completion)
                    await writer.WriteAsync(queued, cancellationToken);
                }
                Interlocked.Increment(ref _queued);
                return true;
            }
            catch (ChannelClosedException)
            {
                Log.Warning("Attempted to enqueue work after channel was closed");
                if (!string.IsNullOrEmpty(item.DedupKey))
                {
                    _pendingKeys.TryRemove(item.DedupKey, out _);
                }
                DecrementPriorityQueued(item.Priority);
                return false;
            }
            catch
            {
                if (!string.IsNullOrEmpty(item.DedupKey))
                {
                    _pendingKeys.TryRemove(item.DedupKey, out _);
                }
                DecrementPriorityQueued(item.Priority);
                throw;
            }
        }

        public WorkItemState GetState(string connectionString)
        {
            return _instanceStates.GetOrAdd(connectionString, static _ => new WorkItemState());
        }

        private ChannelWriter<QueuedItem> GetWriter(WorkItemPriority priority)
        {
            return priority switch
            {
                WorkItemPriority.High => _highChannel.Writer,
                WorkItemPriority.Low => _lowChannel.Writer,
                _ => _normalChannel.Writer
            };
        }

        // Total queue depth derived from per-priority counters
        public int QueueDepth => HighQueueDepth + NormalQueueDepth + LowQueueDepth;

        public int HighQueueDepth => Volatile.Read(ref _queuedHigh);
        public int NormalQueueDepth => Volatile.Read(ref _queuedNormal);
        public int LowQueueDepth => Volatile.Read(ref _queuedLow);

        /// <summary>
        /// The queue's state for the service stats: the queue lengths and busy workers now, and since the previous call - the
        /// most workers busy at once, items queued, duplicates skipped, items started and the time they waited for a worker.
        /// Those reset with each call, so there must be only one caller (the service stats reporter).
        /// </summary>
        public WorkQueueStats TakeStats()
        {
            var busy = Volatile.Read(ref _busyWorkers);
            int started;
            long waitTotalMs, waitMaxMs;
            lock (_startedLock)
            {
                started = _started;
                waitTotalMs = _waitTotalMs;
                waitMaxMs = _waitMaxMs;
                _started = 0;
                _waitTotalMs = 0;
                _waitMaxMs = 0;
            }
            return new WorkQueueStats
            {
                QueueLengthHigh = HighQueueDepth,
                QueueLengthNormal = NormalQueueDepth,
                QueueLengthLow = LowQueueDepth,
                Workers = _workerCount,
                BusyWorkers = busy,
                // Reset to the workers busy now, the starting point for the next interval's most
                MaxBusyWorkers = Math.Max(Interlocked.Exchange(ref _maxBusyWorkers, busy), busy),
                Queued = Interlocked.Exchange(ref _queued, 0),
                Skipped = Interlocked.Exchange(ref _skipped, 0),
                Started = started,
                WaitTotalMs = waitTotalMs,
                WaitMaxMs = waitMaxMs
            };
        }

        /// <summary>An item has been taken off the queue - <paramref name="queued"/>'s wait is added to the stats</summary>
        private IWorkItem Dequeued(QueuedItem queued)
        {
            var waitMs = (long)Stopwatch.GetElapsedTime(queued.EnqueuedTimestamp).TotalMilliseconds;
            lock (_startedLock)
            {
                _started++;
                _waitTotalMs += waitMs;
                if (waitMs > _waitMaxMs) _waitMaxMs = waitMs;
            }
            return queued.Item;
        }

        private static void InterlockedMax(ref int location, int value)
        {
            var current = Volatile.Read(ref location);
            while (value > current)
            {
                var previous = Interlocked.CompareExchange(ref location, value, current);
                if (previous == current) return;
                current = previous;
            }
        }

        // Centralized dequeue by priority with proper low gating and counters
        private bool TryDequeue(WorkItemPriority priority, out IWorkItem item)
        {
            switch (priority)
            {
                case WorkItemPriority.High:
                    if (_highChannel.Reader.TryRead(out var high))
                    {
                        DecrementPriorityQueued(WorkItemPriority.High);
                        item = Dequeued(high);
                        return true;
                    }
                    break;

                case WorkItemPriority.Normal:
                    if (_normalChannel.Reader.TryRead(out var normal))
                    {
                        DecrementPriorityQueued(WorkItemPriority.Normal);
                        item = Dequeued(normal);
                        return true;
                    }
                    break;

                case WorkItemPriority.Low:
                    if (_lowGate.Wait(0))
                    {
                        if (_lowChannel.Reader.TryRead(out var low))
                        {
                            Interlocked.Increment(ref _lowInProgress);
                            DecrementPriorityQueued(WorkItemPriority.Low);
                            item = Dequeued(low);
                            return true;
                        }
                        _lowGate.Release();
                    }
                    break;
            }

            item = default!;
            return false;
        }

        /// <summary>
        /// Returns an item from one of the queues, picking a queue at random based on priority weights, then trying queues in strict priority order.
        /// </summary>
        private bool DequeueByPriorityWithRandom(out IWorkItem item)
        {
            var roll = Random.Shared.NextDouble();
            var firstPriority =
                roll < _highFirst ? WorkItemPriority.High :
                roll < (_highFirst + _normalFirst) ? WorkItemPriority.Normal :
                WorkItemPriority.Low;

            if (TryDequeue(firstPriority, out item)) return true;

            if (TryDequeue(WorkItemPriority.High, out item)) return true;
            if (TryDequeue(WorkItemPriority.Normal, out item)) return true;
            if (TryDequeue(WorkItemPriority.Low, out item)) return true;

            item = default!;
            return false;
        }

        private async Task<IWorkItem> ReadNextAsync(CancellationToken cancellationToken)
        {
            if (DequeueByPriorityWithRandom(out var random)) return random;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (DequeueByPriorityWithRandom(out var item)) return item;

                var allowLow = _lowGate.CurrentCount > 0;

                var highReady = _highChannel.Reader.WaitToReadAsync(cancellationToken).AsTask();
                var normalReady = _normalChannel.Reader.WaitToReadAsync(cancellationToken).AsTask();
                var lowReadyOrTick = allowLow
                    ? _lowChannel.Reader.WaitToReadAsync(cancellationToken).AsTask()
                    : Task.Delay(Random.Shared.Next(MIN_LOW_PRIORITY_POLL_MS, MAX_LOW_PRIORITY_POLL_MS), cancellationToken);

                await Task.WhenAny(highReady, normalReady, lowReadyOrTick);

                if (_highChannel.Reader.Completion.IsCompleted &&
                    _normalChannel.Reader.Completion.IsCompleted &&
                    _lowChannel.Reader.Completion.IsCompleted &&
                    !DequeueByPriorityWithRandom(out item))
                {
                    throw new OperationCanceledException();
                }
            }

            throw new OperationCanceledException();
        }

        private async Task WorkerAsync(int workerId, CancellationToken cancellationToken)
        {
            Log.Debug("Worker {workerId} started", workerId);

            while (!cancellationToken.IsCancellationRequested)
            {
                IWorkItem item;
                try
                {
                    item = await ReadNextAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Log.Information("Worker {workerId} canceled", workerId);
                    break;
                }

                var isLow = item.Priority == WorkItemPriority.Low;

                InterlockedMax(ref _maxBusyWorkers, Interlocked.Increment(ref _busyWorkers));
                try
                {
                    await ProcessWorkItemAsync(item, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Worker {workerId} error processing {item}",
                        workerId, item.Description);
                }
                finally
                {
                    Interlocked.Decrement(ref _busyWorkers);
                    if (!string.IsNullOrEmpty(item.DedupKey))
                    {
                        _pendingKeys.TryRemove(item.DedupKey, out _);
                    }

                    if (isLow)
                    {
                        Interlocked.Decrement(ref _lowInProgress);
                        _lowGate.Release();
                    }
                }
            }

            Log.Debug("Worker {workerId} stopped", workerId);
        }

        private async Task ProcessWorkItemAsync(IWorkItem item, CancellationToken cancellationToken)
        {
            var connectionString = item.Source?.ConnectionString ?? Guid.NewGuid().ToString();

            // Allow up to N concurrent collections per instance (configured via MaxCount in AsyncKeyedLocker)
            // This maintains the current behavior where different schedules can run concurrently
            using (await _instanceLocker.LockAsync(connectionString, cancellationToken))
            {
                await item.ExecuteAsync(_config, cancellationToken);
            }
        }

        private void IncrementPriorityQueued(WorkItemPriority priority)
        {
            switch (priority)
            {
                case WorkItemPriority.High:
                    Interlocked.Increment(ref _queuedHigh);
                    break;

                case WorkItemPriority.Normal:
                    Interlocked.Increment(ref _queuedNormal);
                    break;

                case WorkItemPriority.Low:
                    Interlocked.Increment(ref _queuedLow);
                    break;
            }
        }

        private void DecrementPriorityQueued(WorkItemPriority priority)
        {
            switch (priority)
            {
                case WorkItemPriority.High:
                    Interlocked.Decrement(ref _queuedHigh);
                    break;

                case WorkItemPriority.Normal:
                    Interlocked.Decrement(ref _queuedNormal);
                    break;

                case WorkItemPriority.Low:
                    Interlocked.Decrement(ref _queuedLow);
                    break;
            }
        }
    }
}