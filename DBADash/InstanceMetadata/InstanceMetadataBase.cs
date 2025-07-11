using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace DBADash.InstanceMetadata
{
    public abstract class InstanceMetadataBase
    {

        protected readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
        public abstract string ProviderName { get; }
        protected abstract string PowerShellCommand { get; }

        protected const string GenericMetadata = """
            try{
                $ClusterIP = Get-ClusterResource -name "Cluster IP Address" -ErrorAction Stop | Get-ClusterParameter -name Address -ErrorAction Stop | select-object -Property Value
                $ClusterName = Get-ClusterResource -name "Cluster Name" -ErrorAction Stop | Get-ClusterParameter -name Name -ErrorAction Stop | select-object -Property Value
                $meta | Add-Member -MemberType NoteProperty -Name "ClusterIP" -Value $ClusterIP -Force
                $meta | Add-Member -MemberType NoteProperty -Name "ClusterName" -Value $ClusterName -Force
            }
            catch {
                # Ignore errors
            }
            try{
                # Get IPs excluding loopback and link-local addresses
                $IPs = @(Get-NetIPAddress | Where-Object {$_.AddressFamily -eq 'IPv4' -and $_.IPAddress -ne '127.0.0.1' -and $_.IPAddress -notlike '169.254.*' } -ErrorAction Stop | select-Object -ExpandProperty IPAddress) | Sort-Object
                $meta | Add-Member -MemberType NoteProperty -Name "IPAddresses" -Value $IPs -Force
            }
            catch {
                # Ignore errors
            }
            try{
                $VMHostName = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Virtual Machine\Guest\Parameters" -ErrorAction Stop | select-Object -ExpandProperty HostName  
                $meta | Add-Member -MemberType NoteProperty -Name "VMHostName" -Value $VMHostName -Force
            }
            catch {
                # Ignore errors
            }
            """;

        public virtual async Task<string> GetMetadataAsync(string computerName, CancellationToken cancellationToken = default)
        {
            ValidateComputerName(computerName);
            return await ExecuteRemoteCommandAsync(computerName, cancellationToken);
        }

        protected virtual async Task<string> ExecuteRemoteCommandAsync(string computerName, CancellationToken cancellationToken)
        {
            var connectionInfo = new WSManConnectionInfo(
                new Uri($"http://{computerName}:5985/wsman"),
                "http://schemas.microsoft.com/powershell/Microsoft.PowerShell",
                credential: null)
            {
                IdleTimeout = (int)DefaultTimeout.TotalMilliseconds,
                OperationTimeout = (int)DefaultTimeout.TotalMilliseconds
            };

            using var runspace = RunspaceFactory.CreateRunspace(connectionInfo);

            try
            {
                await Task.Run(() => runspace.Open(), cancellationToken);

                using var powershell = PowerShell.Create();
                powershell.Runspace = runspace;
                powershell.AddScript(PowerShellCommand);

                var results = await Task.Run(() =>
                {
                    var asyncResult = powershell.BeginInvoke();
                    var completed = asyncResult.AsyncWaitHandle.WaitOne(DefaultTimeout);

                    if (!completed)
                    {
                        powershell.Stop();
                        throw new TimeoutException($"{ProviderName} metadata request timed out after {DefaultTimeout.TotalSeconds} seconds");
                    }

                    return powershell.EndInvoke(asyncResult);
                }, cancellationToken);

                if (powershell.HadErrors)
                {
                    var errors = string.Join("; ", powershell.Streams.Error.Select(e => e.ToString()));
                    throw new InstanceMetadataException(ProviderName, $"PowerShell errors: {errors}");
                }

                return ProcessResults(results);
            }
            catch (Exception ex) when (!(ex is TimeoutException || ex is InstanceMetadataException))
            {
                throw new InstanceMetadataException(ProviderName, $"Failed to retrieve metadata from {computerName}: {ex.Message}", ex);
            }
        }

        private string ProcessResults(IList<PSObject> results)
        {
            return results.Count switch
            {
                0 => throw new InstanceMetadataException(ProviderName, "No results returned from PowerShell command"),
                1 => ValidateAndReturnJson(results[0].ToString()),
                _ => throw new InstanceMetadataException(ProviderName, "Multiple results returned, expected single JSON object")
            };
        }

        private string ValidateAndReturnJson(string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                throw new InstanceMetadataException(ProviderName, "Empty or null JSON string returned");
            }

            try
            {
                JToken.Parse(jsonString);
                return jsonString;
            }
            catch (Newtonsoft.Json.JsonReaderException ex)
            {
                throw new InstanceMetadataException(ProviderName, $"Invalid JSON format: {ex.Message}", ex);
            }
        }

        private static void ValidateComputerName(string computerName)
        {
            if (string.IsNullOrWhiteSpace(computerName))
            {
                throw new ArgumentException("Computer name cannot be null or empty", nameof(computerName));
            }

            if (computerName.Length > 253 || computerName.Contains(".."))
            {
                throw new ArgumentException("Invalid computer name format", nameof(computerName));
            }
        }
    }

}
