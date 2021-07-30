# Running Queries

[RunningQueries](../DBADash/SQL/RunningQueries.sql) captures queries that are currently executing.  This takes inspiration from the likes of [sp_whoisactive](http://whoisactive.com/) and [sp_BlitzWho](https://github.com/BrentOzarULTD/SQL-Server-First-Responder-Kit/blob/dev/sp_BlitzWho.sql).  It captures queries that are currently executing as well as sleeping sessions with open transactions.  This point in time snapshot might not be representitive of your SQL Server load but it provides a piece of the puzzle that can help diagnose performance issues.  For example, the data captured is useful to diagnose blocking issues, tempdb contention and memory grant issues.

## What DBA Dash focuses on
DBA Dash focuses on providing lightweight collection of running queries to a central repository. Query text and plans are captured separately from the running query snapshot. A distinct list of sql_handles is constructed from the snapshot and the text is collected for those queries.  If you have multiple instances of the same query running - the text will be collected and stored once.  Those handles are cached so if the same query is captured in future we won't need to collect the text for those queries.  

Query plans can optionally be captured.  Plans can add some overhead in terms of capture and storage so this also uses a similar system to the query text capture.  Plans are captured at the statement level and the caching is done on the plan_handle in combination with the query_plan_hash and statement offsets.  Those plans won't be captured again for future collections as long as the DBA Dash service remains running.  Additionally thresholds are used for plan capture to reduce the overhead of collecting and storing plans that are unlikely to be of interest. By default, plans are not captured but if you enable plan collection these thresholds will be used:

 1,000ms CPU, 10,000ms Duration, 6400 pages (50MB) memory grant, query count: 2

 Plans will be collected for any query that exceeds the specified thresholds.  The thresholds can be configured in the ServiceConfig.json file.
