namespace DBADash
{
    /// <summary>
    /// DataSet table names for the Service Broker collection, in the order SQLServiceBroker.sql returns them.
    /// DBImporter keys off these, so they are also the wire contract.
    ///
    /// <para>The tables are sent to dbo.ServiceBroker_Upd together as table-valued parameters, which bind by
    /// ordinal rather than by name - a column added to the collection query must be added in the same position
    /// in the matching CREATE TYPE.</para>
    /// </summary>
    public static class ServiceBrokerTables
    {
        public const string QueuesTableName = "ServiceBrokerQueues";

        public const string TransmissionQueueTableName = "ServiceBrokerTransmissionQueue";

        public const string ConversationsTableName = "ServiceBrokerConversations";

        public const string EndpointTableName = "ServiceBrokerEndpoint";

        public const string CountersTableName = "ServiceBrokerCounters";

        /// <summary>
        /// Databases the collection visited but could not read.  Empty when every visit succeeded.
        /// dbo.DatabaseNames, shared with any other procedure that takes a list of database names.
        /// </summary>
        public const string DatabasesTableName = "DatabaseNames";

        /// <summary>Result sets of SQLServiceBroker.sql, in order.</summary>
        public static readonly string[] TableNames =
        [
            QueuesTableName, TransmissionQueueTableName, ConversationsTableName, EndpointTableName, CountersTableName, DatabasesTableName
        ];
    }
}
