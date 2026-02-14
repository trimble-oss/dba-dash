CREATE PARTITION SCHEME PS_ResourceGovernorResourcePoolsMetrics
    AS PARTITION PF_ResourceGovernorResourcePoolsMetrics
    ALL TO ([PRIMARY]);