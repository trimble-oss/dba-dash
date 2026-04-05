using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashGUI.Utils
{
    /// <summary>
    /// Holds a strong reference to a value for a short retention period and
    /// then releases it. An optional callback runs when the retention expires.
    /// Thread-safe and supports Reset to extend the retention window.
    /// </summary>
    public sealed class Retainer<T> : IDisposable where T : class
    {
        private readonly object lockObj = new object();
        private CancellationTokenSource cts;
        private TimeSpan retention;
        private readonly Action onTimeout;

        // Generation counter to avoid race where an old retention task fires after Reset/Dispose
        private int generation;

        public Retainer(T value, TimeSpan retention, Action onTimeout = null)
        {
            Table = value;
            this.retention = retention <= TimeSpan.Zero ? TimeSpan.FromMinutes(5) : retention;
            this.onTimeout = onTimeout;
            StartRetentionTask();
        }

        public T Table { get; private set; }

        private void StartRetentionTask()
        {
            try
            {
                cts?.Cancel();
                cts?.Dispose();
                cts = new CancellationTokenSource();
                var token = cts.Token;

                // Increment generation for this retention period and capture locally for the task
                var gen = System.Threading.Interlocked.Increment(ref generation);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(retention, token).ConfigureAwait(false);
                        // If generation changed since this task was started, another Reset/Dispose
                        // has occurred and we must not clear the value.
                        if (gen != generation) return;
                        OnTimeout();
                    }
                    catch (OperationCanceledException)
                    {
                        // expected when reset/disposed
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Retainer task error: {ex}");
                    }
                }, token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Retainer: failed to start retention task: {ex}");
            }
        }

        private void OnTimeout()
        {
            // Clear the retained value and release the CTS to avoid leaving
            // native resources alive if callers never call Dispose().
            lock (lockObj)
            {
                try
                {
                    Table = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Retainer.OnTimeout error clearing value: {ex}");
                }

                try
                {
                    // Dispose and null out the current CTS. It's safe if Reset/Dispose
                    // already cleared it; capture and log any disposal errors.
                    cts?.Dispose();
                    cts = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Retainer.OnTimeout disposing CTS error: {ex}");
                }
            }

            try
            {
                onTimeout?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Retainer: onTimeout callback error: {ex}");
            }
        }

        public bool TryGet(out T value)
        {
            lock (lockObj)
            {
                value = Table;
                return value != null;
            }
        }

        public void Reset(T value)
        {
            lock (lockObj)
            {
                Table = value;
                try
                {
                    StartRetentionTask();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Retainer.Reset error: {ex}");
                }
            }
        }

        public void Dispose()
        {
            // Bump generation to invalidate any in-flight retention tasks so they
            // will not call OnTimeout after disposal.
            try
            {
                Interlocked.Increment(ref generation);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Retainer.Dispose generation increment error: {ex}", ex);
            }

            lock (lockObj)
            {
                try { cts?.Cancel(); } catch (Exception ex) { Debug.WriteLine($"Retainer.Dispose cancel error: {ex}"); }
                try { cts?.Dispose(); } catch (Exception ex) { Debug.WriteLine($"Retainer.Dispose dispose error: {ex}"); }
                cts = null;
                Table = null;
            }
        }
    }
}