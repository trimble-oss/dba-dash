CREATE PARTITION SCHEME [PS_QueryStatsCollection_60MIN]
    AS PARTITION [PF_QueryStatsCollection_60MIN]
    ALL TO ([PRIMARY]);
