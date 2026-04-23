using DBADash;
using Microsoft.Extensions.Configuration;

namespace DBADashAI.Configuration;

/// <summary>
/// A configuration provider that decrypts values prefixed with <see cref="EncryptText.DpApiPrefix"/>
/// using DPAPI LocalMachine scope. This allows secrets to be stored encrypted in appsettings.json
/// and decrypted in memory at startup without any changes to the rest of the application.
///
/// Values protected by a higher-priority provider (e.g., Azure Key Vault, environment variables)
/// naturally override the DPAPI-encrypted values in appsettings.json and are never passed through
/// this provider.
/// </summary>
internal sealed class DpApiConfigurationProvider : ConfigurationProvider
{
    private readonly IConfigurationRoot _root;

    public DpApiConfigurationProvider(IConfigurationRoot root)
    {
        _root = root;
    }

    public override void Load()
    {
        Data.Clear();
        foreach (var kvp in _root.AsEnumerable())
        {
            if (kvp.Value is not null && kvp.Value.StartsWith(EncryptText.DpApiPrefix, StringComparison.Ordinal))
            {
                try
                {
                    Data[kvp.Key] = kvp.Value.FromMachineProtectedConfigValue();
                }
                catch
                {
                    // Leave the raw (still-prefixed) value so diagnostics endpoint can detect it.
                }
            }
        }
    }
}
