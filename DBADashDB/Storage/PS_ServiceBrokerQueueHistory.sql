CREATE PARTITION SCHEME [PS_ServiceBrokerQueueHistory]
    AS PARTITION [PF_ServiceBrokerQueueHistory]
    ALL TO ([PRIMARY])
