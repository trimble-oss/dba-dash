CREATE PARTITION SCHEME [PS_QueryStatsCollection]
    AS PARTITION [PF_QueryStatsCollection]
    ALL TO([PRIMARY]);
