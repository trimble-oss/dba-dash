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
                try { cts?.Dispose(); } catch { }
                cts = new CancellationTokenSource();
                var token = cts.Token;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(retention, token).ConfigureAwait(false);
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
