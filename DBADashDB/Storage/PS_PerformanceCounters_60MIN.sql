CREATE PARTITION SCHEME [PS_PerformanceCounters_60MIN]
    AS PARTITION [PF_PerformanceCounters_60MIN]
    ALL TO ([PRIMARY]);

