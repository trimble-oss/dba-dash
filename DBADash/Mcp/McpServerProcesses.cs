#nullable enable
using Serilog;
using System;
using System.Diagnostics;
using System.IO;

namespace DBADash.Mcp
{
    /// <summary>
    /// The DBADashMCP.exe processes AI tools have started.  Each holds the install folder's DLLs open, so an upgrade
    /// that replaces them has to stop the ones running from that folder first.  The AI tool starts a new one on its next
    /// call to the server - or, for some, after the user reconnects it.
    /// </summary>
    public static class McpServerProcesses
    {
        public const string ProcessName = "DBADashMCP";

        /// <summary>Stops every DBADashMCP.exe running from <paramref name="folder"/>.  Returns how many were stopped.</summary>
        public static int StopRunningFrom(string folder)
        {
            var prefix = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var stopped = 0;
            foreach (var process in Process.GetProcessesByName(ProcessName))
            {
                using (process)
                {
                    try
                    {
                        var path = process.MainModule?.FileName;
                        if (path is null || !path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

                        process.Kill();
                        process.WaitForExit(5000);
                        stopped++;
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
                    {
                        // Gone already, or another user's: the upgrade's own check reports what it couldn't stop.
                        Log.Debug(ex, "Couldn't stop {Process} {Pid}", ProcessName, process.Id);
                    }
                }
            }

            if (stopped > 0)
            {
                Log.Information("Stopped {Count} DBADashMCP process(es) running from {Folder} for the upgrade", stopped, folder);
            }

            return stopped;
        }
    }
}
