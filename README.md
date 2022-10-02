# DBA Dash - SQL Server Monitoring Tool

![DBA Dash Performance](Docs/DBADash_Performance_small.png)

## Download

[Download](https://github.com/trimble-oss/dba-dash/releases)

## [Website](https://dbadash.com) 🆕

Documentation is now available on [dbadash.com](https://dbadash.com), including an easy to follow [quick start](https://dbadash.com/docs/setup/quick-start/) guide.

## Project Summary

DBA Dash is a tool for SQL Server DBAs to assist with daily checks, performance monitoring and change tracking.

- Backups Agent Jobs, DBCC, Corruption, Drive space
- Availability Groups, Log Shipping, Mirroring
- [OS Performance Counters + Custom Metrics](https://dbadash.com/docs/help/os-performance-counters/)
- Stored Procedure/Function/Trigger execution stats
- [Capture slow queries](https://dbadash.com/docs/help/slow-queries/) (Extended Event trace)
- Azure DB monitoring
- Track changes to configuration, SQL Patching, drivers etc.
- [Schema change tracking](https://dbadash.com/docs/help/schema-snapshots/). 
- Agent Job change tracking
- Option to monitor instances in isolated environments via S3 bucket.
- [Custom Checks](https://dbadash.com/docs/help/custom-checks/)

 [What DBA Dash collects and when](https://dbadash.com/docs/help/schedule/)

## Video Overview

[![DBA Dash Overview](https://img.youtube.com/vi/X7e4zElOQ3c/0.jpg)](https://www.youtube.com/watch?v=X7e4zElOQ3c)

## Requirements

- SQL Server 2016 SP1 or later required for DBADashDB repository database.  RDS & Azure DB is supported.  
- SQL 2008-SQL 2022 supported for monitored instances - including Azure and RDS (SQL Server).  
- Windows machine to run agent.  Agent can monitor multiple SQL instances.

## Prerequisites

- Account to use for agent.  Review the [security doc](https://dbadash.com/docs/help/security/) for required permissions. 
- [.NET Desktop Runtime 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) is used by DBA Dash.  You will be prompted to install the .NET runtime version 6 if it's not already installed.

> **Note** 
> It's possible to run as a console app under your own user account for testing purposes.

## Installation

### 👋 [Quick start guide here](https://dbadash.com/docs/setup/quick-start/).

## Upgrades

### 👋 [See here for upgrade help](https://dbadash.com/docs/setup/upgrades/)

## Help

### 👋 [More help here](https://dbadash.com/docs/setup/quick-start/)
