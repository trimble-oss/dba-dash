﻿CREATE PARTITION SCHEME [PS_CPU_60MIN]
    AS PARTITION [PF_CPU_60MIN]
    ALL TO ([PRIMARY]);

