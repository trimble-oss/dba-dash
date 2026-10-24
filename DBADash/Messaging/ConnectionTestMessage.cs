using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    /// <summary>
    /// Checks connections before they're added from the GUI (<see cref="ConnectionAddMessage"/>): the service connects to
    /// each one, as the service account, and reports what adding it would mean - see <see cref="SourceConnectionCheck"/>.
    /// The GUI can't do this itself: it might not be able to reach the instance, and with Windows authentication its user
    /// might have access the service account doesn't, or the other way round.  Changes nothing.  Must be explicitly
    /// enabled on the service (<see cref="CollectionConfig.AllowConnectionChanges"/>).
    /// </summary>
    public class ConnectionTestMessage : MessageBase
    {
        /// <summary>Connection strings (or instance names) to check, each encrypted with the service's key (<see cref="MessageEncryption"/>).</summary>
        public List<string> ConnectionStrings { get; set; } = new();

        /// <summary>The options the connections would be added with - some checks depend on them, e.g. whether deadlock collection has a schedule.</summary>
        public SourceConnectionOptions Options { get; set; } = new();

        public override async Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken)
        {
            ThrowIfExpired();
            ConnectionChanges.ThrowIfNotAllowed(cfg);
            ConnectionChanges.ValidateCount(ConnectionStrings?.Count ?? 0);
            ConnectionChanges.ValidateOptions(Options);

            CollectionConfig fileConfig;
            await CollectionScheduleUpdateMessage.ConfigFileLock.WaitAsync(cancellationToken);
            try
            {
                fileConfig = ConnectionChanges.LoadFileConfig();
            }
            finally
            {
                CollectionScheduleUpdateMessage.ConfigFileLock.Release();
            }

            await ReportProgressAsync(new ResponseMessage
            {
                Type = ResponseMessage.ResponseTypes.Progress,
                Message = $"Checking {ConnectionStrings.Count} connection(s)..."
            });

            var checks = await ConnectionChanges.ForEachThrottledAsync(ConnectionStrings, async encrypted =>
            {
                DBADashSource src;
                try
                {
                    src = ConnectionChanges.CreateSource(encrypted, Options);
                }
                catch (Exception ex)
                {
                    return new SourceConnectionCheck { IsInvalid = true, ValidationError = ex.Message };
                }
                return await SourceConnectionAdder.CheckAsync(fileConfig, src);
            }, cancellationToken);

            return ConnectionChanges.ToDataSet(checks);
        }
    }
}
