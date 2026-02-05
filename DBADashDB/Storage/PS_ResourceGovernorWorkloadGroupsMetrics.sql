CREATE PARTITION SCHEME PS_ResourceGovernorWorkloadGroupsMetrics
    AS PARTITION PF_ResourceGovernorWorkloadGroupsMetrics
    ALL TO ([PRIMARY]);