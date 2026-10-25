using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Gets the options an instance's connection is configured with, for editing in the GUI
    /// (<see cref="ConnectionOptionsUpdateMessage"/>).  Read from ServiceConfig.json.  Gated like the change it's used
    /// for (<see cref="CollectionConfig.AllowConnectionChanges"/>).
    /// </summary>
    public class ConnectionOptionsGetMessage : MessageBase
    {
        public string ConnectionID { get; set; }

        /// <summary>The reply to <see cref="ConnectionOptionsGetMessage"/></summary>
        public class ConnectionOptionsInfo
        {
            public string ConnectionID { get; set; }

            /// <summary>
            /// False for an instance the service monitors without it being in the config - found by the Azure DB or read
            /// replica scan.  Its options come from the connection it was found from.
            /// </summary>
            public bool IsInConfig { get; set; }

            public string ConnectionForPrint { get; set; }

            /// <summary>The connection's options.  Custom collections aren't included - they can only be changed in the config tool.</summary>
            public SourceConnectionOptions Options { get; set; }

            public int CustomCollectionCount { get; set; }
        }

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            ConnectionChanges.ThrowIfNotAllowed(cfg);
            if (string.IsNullOrEmpty(ConnectionID))
            {
                throw new ArgumentException("ConnectionID is required.");
            }

            DBADashSource src;
            await CollectionScheduleUpdateMessage.ConfigFileLock.WaitAsync(cancellationToken);
            try
            {
                src = ConnectionRemoveMessage.FindInFileConfig(ConnectionChanges.LoadFileConfig(), ConnectionID);
            }
            finally
            {
                CollectionScheduleUpdateMessage.ConfigFileLock.Release();
            }

            var info = new ConnectionOptionsInfo { ConnectionID = ConnectionID, IsInConfig = src != null };
            if (src != null)
            {
                info.ConnectionForPrint = src.SourceConnection.ConnectionForPrint;
                info.CustomCollectionCount = src.CustomCollections?.Count ?? 0;
                info.Options = SourceConnectionOptions.FromSource(src);
                info.Options.CustomCollections = null;
            }
            return ConnectionChanges.ToDataSet(info);
        }
    }
}
