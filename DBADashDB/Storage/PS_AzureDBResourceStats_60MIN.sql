﻿CREATE PARTITION SCHEME [PS_AzureDBResourceStats_60MIN]
    AS PARTITION [PF_AzureDBResourceStats_60MIN]
    ALL TO ([PRIMARY]);

