using Microsoft.Extensions.Configuration;

namespace DBADashAI.Configuration;

/// <summary>
/// Configuration source that triggers <see cref="DpApiConfigurationProvider"/>.
/// It holds a snapshot of the sources registered *before* it was added so that calling
/// Build() cannot cause infinite recursion.
/// </summary>
internal sealed class DpApiConfigurationSource : IConfigurationSource
{
    private readonly IReadOnlyList<IConfigurationSource> _priorSources;

    public DpApiConfigurationSource(IReadOnlyList<IConfigurationSource> priorSources)
    {
        _priorSources = priorSources;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        // Build a root from only the sources that existed before this one was added.
        // This avoids infinite recursion and means Azure KV / env-var values (already
        // the highest-priority plain-text values) are what gets scanned for the dpapi: prefix.
        var snapshotBuilder = new ConfigurationBuilder();
        foreach (var source in _priorSources)
            snapshotBuilder.Add(source);

        return new DpApiConfigurationProvider(snapshotBuilder.Build());
    }
}

internal static class DpApiConfigurationExtensions
{
    /// <summary>
    /// Adds a configuration provider that decrypts any values prefixed with "dpapi:" using
    /// DPAPI LocalMachine scope. Call this after all other configuration sources have been added
    /// (including Azure Key Vault) so that higher-priority plain-text values are not re-processed.
    /// </summary>
    public static IConfigurationBuilder AddDpApiSecrets(this IConfigurationBuilder builder)
    {
        // Snapshot sources registered so far — before this source is added — to prevent
        // infinite recursion when DpApiConfigurationSource.Build() calls builder.Build().
        var snapshot = builder.Sources.ToList();
        builder.Add(new DpApiConfigurationSource(snapshot));
        return builder;
    }
}
