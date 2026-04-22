-- ============================================================
-- DBADash AI Example Questions - Seed Data
-- Preserves existing categories + adds new tool coverage
-- ============================================================

DELETE FROM DBADash.AIExampleQuestions;
GO

INSERT INTO DBADash.AIExampleQuestions (Category, Question, SortOrder) VALUES
--  Alerts (existing) 
(N'Alerts', N'What are the top issues requiring immediate DBA action right now?', 0),
(N'Alerts', N'What unresolved alerts are currently highest priority?', 0),
(N'Alerts', N'Which alert types are recurring most often this week?', 0),
(N'Alerts', N'Which critical alerts are still active and unacknowledged?', 0),
(N'Alerts', N'Which instances have the most active alerts right now?', 0),

--  Availability Groups & DR (existing + new) 
(N'Availability Groups & DR', N'What are our top DR and RPO risks across active instances?', 0),
(N'Availability Groups & DR', N'Which Availability Groups are showing recent health instability?', 0),
(N'Availability Groups & DR', N'Which instances have AG alerts plus backup risk right now?', 0),
(N'Availability Groups & DR', N'Which replicas/AG-related alerts should be prioritized immediately?', 0),
(N'Availability Groups & DR', N'Are all AG replicas healthy and synchronized?', 1),
(N'Availability Groups & DR', N'What is the send queue and redo queue size for each replica?', 2),
(N'Availability Groups & DR', N'Is automatic failover at risk for any AG?', 3),
(N'Availability Groups & DR', N'What is the secondary lag across all AGs?', 4),
(N'Availability Groups & DR', N'Are there any suspended AG databases?', 5),
(N'Availability Groups & DR', N'Is database mirroring healthy?', 6),

--  Backups & Storage (existing + new) 
(N'Backups & Storage', N'Which databases are at backup risk right now?', 0),
(N'Backups & Storage', N'Which databases are growing fastest this week?', 0),
(N'Backups & Storage', N'Which databases have not had a recent successful full backup?', 0),
(N'Backups & Storage', N'Which drives are at risk of filling up?', 0),
(N'Backups & Storage', N'Which instances have the lowest free drive space?', 0),
(N'Backups & Storage', N'Which databases have not had a log backup in the last 4 hours?', 1),
(N'Backups & Storage', N'Are any backups damaged or missing checksums?', 2),
(N'Backups & Storage', N'What is the RPO risk for each database?', 3),
(N'Backups & Storage', N'Are backups encrypted and compressed?', 4),
(N'Backups & Storage', N'How long would it take to restore each database?', 5),
(N'Backups & Storage', N'How fast are drives filling up?', 6),
(N'Backups & Storage', N'How many days until each drive is full?', 7),
(N'Backups & Storage', N'Which databases are using the most storage?', 8),
(N'Backups & Storage', N'Are any database files close to their max size?', 9),
(N'Backups & Storage', N'Are there any autogrowth concerns?', 10),

--  Capacity Forecasting (existing) 
(N'Capacity Forecasting', N'Summarize storage and memory capacity risks for the next 7 days.', 0),
(N'Capacity Forecasting', N'Which drives are most likely to fill up soon?', 0),
(N'Capacity Forecasting', N'Which instances show high memory pressure right now?', 0),
(N'Capacity Forecasting', N'Which servers have less than 15% free space?', 0),
(N'Capacity Forecasting', N'Are any instances still warming the buffer cache after a restart?', 1),

--  Configuration Drift (existing) 
(N'Configuration Drift', N'What configuration changes happened in the last week?', 0),
(N'Configuration Drift', N'What high-risk SQL configuration changes happened in the last 14 days?', 0),
(N'Configuration Drift', N'What SQL patching changes were detected recently?', 0),
(N'Configuration Drift', N'Which instances have the highest configuration drift risk this week?', 0),
(N'Configuration Drift', N'Which servers had frequent config changes that need review?', 0),
(N'Configuration Drift', N'Which servers had hardware or driver changes recently?', 0),
(N'Configuration Drift', N'Which trace flag or DB option changes need review?', 0),

--  Configuration Best Practices (new) 
(N'Configuration Best Practices', N'Are there any configuration best practice violations?', 0),
(N'Configuration Best Practices', N'What is maxdop set to across all instances?', 1),
(N'Configuration Best Practices', N'What is max server memory configured to?', 2),
(N'Configuration Best Practices', N'Is xp_cmdshell enabled on any instance?', 3),
(N'Configuration Best Practices', N'Compare configuration across all instances', 4),
(N'Configuration Best Practices', N'Is cost threshold for parallelism still at the default?', 5),

--  Database Configuration (new) 
(N'Database Configuration', N'Do any databases have auto_shrink enabled?', 0),
(N'Database Configuration', N'Has anyone changed the recovery model recently?', 1),
(N'Database Configuration', N'What databases have non-default scoped configurations?', 2),
(N'Database Configuration', N'Has the compatibility level changed on any databases?', 3),
(N'Database Configuration', N'Is page_verify set to CHECKSUM for all databases?', 4),
(N'Database Configuration', N'Are there any databases with trustworthy enabled?', 5),

--  Cross-Signal Analysis (existing) 
(N'Cross-Signal Analysis', N'Correlate alerts, waits, blocking, deadlocks, and storage risk by instance.', 0),
(N'Cross-Signal Analysis', N'Show likely root-cause clusters across the estate.', 0),
(N'Cross-Signal Analysis', N'What are the top systemic risks where multiple signals align?', 0),
(N'Cross-Signal Analysis', N'Which instances have the highest multi-signal risk score?', 0),

--  Instance Inventory (existing) 
(N'Instance Inventory', N'How many instances are Enterprise Edition vs Standard Edition?', 0),
(N'Instance Inventory', N'How many servers have more than 16GB of RAM?', 0),
(N'Instance Inventory', N'How many servers have more than 64GB of RAM?', 0),
(N'Instance Inventory', N'How many SQL Servers are on SQL 2016?', 0),
(N'Instance Inventory', N'How many SQL Servers are on SQL 2019 or newer?', 0),
(N'Instance Inventory', N'List SQL Server counts by major version.', 0),
(N'Instance Inventory', N'Which instances have the highest memory and CPU footprint?', 0),

--  Jobs (existing + new) 
(N'Jobs', N'Are there any currently running jobs that look stuck?', 0),
(N'Jobs', N'Which jobs are failing repeatedly over the last 7 days?', 0),
(N'Jobs', N'Which jobs are running longer than normal?', 0),
(N'Jobs', N'Which jobs failed today?', 0),
(N'Jobs', N'Are any job steps silently failing?', 1),
(N'Jobs', N'Which jobs have the most failures in the last 24 hours?', 2),

--  Operational Hygiene (existing) 
(N'Operational Hygiene', N'Show unresolved vs resolved-unacknowledged alert counts by instance.', 0),
(N'Operational Hygiene', N'What stale alert workflow issues should be cleaned up today?', 0),
(N'Operational Hygiene', N'Where is alert hygiene debt accumulating right now?', 0),
(N'Operational Hygiene', N'Which instances have the highest resolved-but-unacknowledged alert backlog?', 0),

--  Performance & Waits (existing + new) 
(N'Performance & Waits', N'Are deadlocks increasing today?', 0),
(N'Performance & Waits', N'Are we seeing blocking in the last 24 hours?', 0),
(N'Performance & Waits', N'What are the top waits right now?', 0),
(N'Performance & Waits', N'Which instances have the highest query wait times right now?', 0),
(N'Performance & Waits', N'Which sessions are currently causing the most blocking?', 0),
(N'Performance & Waits', N'Is there CPU pressure based on wait stats?', 1),
(N'Performance & Waits', N'Are there any RESOURCE_SEMAPHORE waits?', 2),

--  Performance & CPU (new) 
(N'Performance & CPU', N'Which instances have high CPU usage?', 0),
(N'Performance & CPU', N'Show me CPU utilization trends for the last 24 hours', 1),
(N'Performance & CPU', N'What are the top stored procedures by CPU consumption?', 2),
(N'Performance & CPU', N'What is the Page Life Expectancy across all instances?', 3),
(N'Performance & CPU', N'Are there any memory grant waits?', 4),
(N'Performance & CPU', N'Show me the key performance counters for all instances', 5),
(N'Performance & CPU', N'Which procedures have the highest average reads per execution?', 6),

--  Query Performance (existing + new) 
(N'Query Performance', N'Which queries are driving the most CPU right now?', 0),
(N'Query Performance', N'Which queries have the biggest plan regressions this week?', 0),
(N'Query Performance', N'Which queries have the highest average duration today?', 0),
(N'Query Performance', N'Which queries regressed in the last day?', 0),
(N'Query Performance', N'Which queries have the highest logical reads per execution?', 1),
(N'Query Performance', N'What long-running queries are active right now?', 2),
(N'Query Performance', N'Are any queries using large memory grants?', 3),

--  Running Queries (new) 
(N'Running Queries', N'What is running on the server right now?', 0),
(N'Running Queries', N'Who is the head blocker?', 1),
(N'Running Queries', N'Are there any sleeping sessions with open transactions?', 2),
(N'Running Queries', N'How long has the oldest open transaction been running?', 3),

--  Reliability (existing) 
(N'Reliability', N'Show the top servers by reliability risk score.', 0),
(N'Reliability', N'Which instances are most unstable based on restarts, offline events, and job failures?', 0),
(N'Reliability', N'Which reliability incidents are recurring this week?', 0),
(N'Reliability', N'Which unresolved reliability alerts need immediate attention?', 0),
(N'Reliability', N'Have any instances restarted unexpectedly?', 1),

--  Table Sizes (new) 
(N'Table Sizes', N'What are the largest tables across all instances?', 0),
(N'Table Sizes', N'Which tables are growing the fastest?', 1),
(N'Table Sizes', N'Are there any tables with excessive unused space?', 2),

--  Identity Columns (new) 
(N'Identity Columns', N'Are any identity columns at risk of exhaustion?', 0),
(N'Identity Columns', N'How long until identity overflow on any table?', 1),
(N'Identity Columns', N'Which INT identity columns are over 50% used?', 2),

--  TempDB (new) 
(N'TempDB', N'Is TempDB configured correctly across all instances?', 0),
(N'TempDB', N'Do any instances have insufficient TempDB data files?', 1),
(N'TempDB', N'Are TempDB files evenly sized?', 2),

--  Triage & Summary (existing) 
(N'Triage & Summary', N'Summarize current DBA risks for the next 24 hours.', 0),
(N'Triage & Summary', N'What should I check first to triage current performance issues?', 0),
(N'Triage & Summary', N'Why are we slow right now?', 0),

--  Workload Pressure (existing) 
(N'Workload Pressure', N'Correlate waits, blocking, deadlocks, and slow queries for the last 24 hours.', 0),
(N'Workload Pressure', N'Where do we have sustained lock contention and query regressions?', 0),
(N'Workload Pressure', N'Which instances show the highest workload pressure right now?', 0),
(N'Workload Pressure', N'Which servers have the highest combined pressure score?', 0)
GO
