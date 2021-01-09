CREATE PARTITION SCHEME [PS_ObjectExecutionStats_60MIN]
    AS PARTITION [PF_ObjectExecutionStats_60MIN]
    ALL TO ([PRIMARY]);

