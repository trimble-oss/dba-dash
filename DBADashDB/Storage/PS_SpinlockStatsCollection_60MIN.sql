CREATE PARTITION SCHEME [PS_SpinlockStatsCollection_60MIN]
    AS PARTITION [PF_SpinlockStatsCollection_60MIN]
    ALL TO ([PRIMARY])
