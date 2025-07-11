using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

namespace DBADash.InstanceMetadata
{
    public static class InstanceMetadataProviders
    {

        [JsonConverter(typeof(StringEnumConverter))]
        public enum Providers
        {
            AWS,
            Azure,
            Generic
        }

        private static readonly AWSInstanceMetadata AWS = new();
        private static readonly AzureInstanceMetadata Azure = new();
        private static readonly GenericInstanceMetadata Generic = new();


        public static InstanceMetadataBase GetInstanceMetadataProvider(Providers provider)
        {
            return provider switch
            {
                Providers.AWS => AWS,
                Providers.Azure => Azure,
                Providers.Generic => Generic,
                _ => throw new ArgumentOutOfRangeException(nameof(provider), $"No instance metadata found for provider: {provider}")
            };
        }

        public static HashSet<InstanceMetadataProviders.Providers>EnabledProviders { get; set; } = new()
        {
            Providers.AWS,
            Providers.Azure,
            Providers.Generic
        };

        /// <summary>
        /// Tries all cloud providers and returns the first one that succeeds
        /// </summary>
        public static async Task<(string ProviderName, string Json)> GetMetadataAsync(string computerName, CancellationToken cancellationToken = default)
        {
            var exceptions = new List<Exception>();
            if(EnabledProviders.Count == 0)
            {
                throw new InvalidOperationException("No instance metadata providers are enabled. Please configure at least one provider.");
            }
            foreach (var provider in EnabledProviders)
            {
                try
                {
                    var meta = GetInstanceMetadataProvider(provider);
                    var json = await meta.GetMetadataAsync(computerName, cancellationToken);

                    return (meta.ProviderName, json);
                }
                catch (Exception ex)
                {
                    exceptions.Add(new Exception($"Failed to retrieve metadata from {provider}", ex));
                }
            }

            throw new AggregateException($"Failed to retrieve metadata from any cloud provider for {computerName}", exceptions);
        }
    }
}