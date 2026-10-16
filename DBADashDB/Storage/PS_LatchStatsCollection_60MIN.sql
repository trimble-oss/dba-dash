CREATE PARTITION SCHEME [PS_LatchStatsCollection_60MIN]
    AS PARTITION [PF_LatchStatsCollection_60MIN]
    ALL TO ([PRIMARY])
