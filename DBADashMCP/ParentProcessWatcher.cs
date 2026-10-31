using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DBADashMCP
{
    /// <summary>
    /// Stops the server when the process that started it has gone.
    ///
    /// A client - Claude Code, VS Code, Claude Desktop - starts one server per session and talks to it over stdin, and
    /// the server normally ends when that pipe closes.  It doesn't close when the client is killed while something else
    /// still holds the other end: a cmd or npx wrapper, or another child of the client.  Those servers would run until
    /// the user logged off, holding DBA Dash's files open - which also stops an upgrade replacing them.
    /// </summary>
    internal sealed class ParentProcessWatcher(IHostApplicationLifetime lifetime, ILogger<ParentProcessWatcher> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var (parent, parentId) = Parent();
            using (parent)
            {
                if (parentId is null)
                {
                    // Can't tell which process started this one.  The stdin pipe is still there to end the server.
                    return;
                }

                if (parent is not null)
                {
                    try
                    {
                        await parent.WaitForExitAsync(stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
                    {
                        // Can't wait on it - another user's process, say.  The stdin pipe is still there to end the server.
                        logger.LogDebug(ex, "Can't watch the parent process");
                        return;
                    }
                }
            }

            logger.LogWarning("The process that started this server (PID {Pid}) has exited, so the server is stopping", parentId);
            lifetime.StopApplication();

            // The stdio transport may sit in a read of stdin that nothing will ever answer; don't wait on it for long.
            _ = Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None).ContinueWith(_ => Environment.Exit(0), TaskScheduler.Default);
        }

        /// <summary>
        /// The process that started this one, and its ID.  The process is null when it has already gone - including when
        /// its ID now belongs to a process started after this one, which can't be its parent - and the ID is null only
        /// when it can't be told which process that was.
        /// </summary>
        internal static (Process? Process, int? Id) Parent()
        {
            using var self = Process.GetCurrentProcess();
            var info = new ProcessBasicInformation();
            if (NtQueryInformationProcess(self.Handle, 0, ref info, Marshal.SizeOf<ProcessBasicInformation>(), out _) != 0)
            {
                return (null, null);
            }

            var id = (int)info.InheritedFromUniqueProcessId;
            Process parent;
            try
            {
                parent = Process.GetProcessById(id);
            }
            catch (ArgumentException)
            {
                return (null, id);
            }

            try
            {
                if (parent.StartTime > self.StartTime)
                {
                    parent.Dispose();
                    return (null, id);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                // Its start time can't be read - another user's process.  Still worth watching.
            }

            return (parent, id);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessBasicInformation
        {
            public IntPtr ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public IntPtr BasePriority;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass,
            ref ProcessBasicInformation processInformation, int processInformationLength, out int returnLength);
    }
}
