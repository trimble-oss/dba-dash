CREATE PARTITION SCHEME [PS_DeadlockProcesses]
    AS PARTITION [PF_DeadlockProcesses]
    ALL TO ([PRIMARY]);

