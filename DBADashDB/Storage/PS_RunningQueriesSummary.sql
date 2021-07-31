CREATE PARTITION SCHEME [PS_RunningQueriesSummary]
    AS PARTITION [PF_RunningQueriesSummary]
    ALL TO ([PRIMARY])