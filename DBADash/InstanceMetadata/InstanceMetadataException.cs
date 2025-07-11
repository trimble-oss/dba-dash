using System;

namespace DBADash.InstanceMetadata
{
    public class InstanceMetadataException : Exception
    {
        public string Provider { get; }

        public InstanceMetadataException(string provider, string message) : base(message)
        {
            Provider = provider;
        }

        public InstanceMetadataException(string provider, string message, Exception innerException) : base(message, innerException)
        {
            Provider = provider;
        }
    }
}
