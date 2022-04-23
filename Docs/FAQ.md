# FAQ

* [I Found a bug](#i-found-a-bug)
* [I have a feature suggestion](#i-have-a-feature-suggestion)
* [How to I contribute?](#how-do-i-contribute)
* [I need help or want to ask a question](#i-need-help-or-want-to-ask-a-question)
* [How do I create alert notifications with DBA Dash?](#how-do-i-create-alert-notifications-with-dba-dash)
* [How many instances can I monitor with DBA Dash?](#how-many-instances-can-i-monitor-with-dba-dash)
* [I get a 'Operation will cause database event session memory to exceed allowed limit.' error on AzureDB](#i-get-a-operation-will-cause-database-event-session-memory-to-exceed-allowed-limit-error-on-azuredb)
* [The stored procedure names are not showing](#the-stored-procedure-names-are-not-showing)
* [How do I get notifications of new releases?](#how-do-i-get-notifications-of-new-releases)

## I Found a bug

Please create an [Issue](https://github.com/trimble-oss/dba-dash/issues).  Before submitting an issue, please check if there is an existing issue for the bug.  Include as much information as possible about your environment and how to reproduce the bug, including full error messages.  Be careful not to include any security sensitive data in your issue.  

Are you able to fix the bug yourself?  Please mention this in the issue.  We accept pull requests!

## I have a feature suggestion

Please create an [Issue](https://github.com/trimble-oss/dba-dash/issues).  Before submitting an issue, please check if there is an existing issue for the feature request. Provide a clear description for the feature.  If you are willing to help develop the new feature, please mention this in the issue.  DBA Dash is an open soure project and will improve through community contributions.

## How Do I Contribute?

There are some [guidelines here](https://trimble-oss.github.io/contribute/) for Trimble employees.  We also accept external contributions.  We will [create some documentation](https://github.com/trimble-oss/dba-dash/issues/69) for external contributions shortly.

## I need help or want to ask a question

You can ask questions via the [discussions](https://github.com/trimble-oss/dba-dash/discussions) on GitHub.  There is also a **#dbadash** channel on the [SQL Server Community slack](https://dbatools.io/slack).  Before submitting a query, please check if there is an answer to your question in the documentation.  Also search the discussions to see if a similar question has already been asked. DBA Dash is an open source project that relies on community support to help answer queries.

## How do I create alert notifications with DBA Dash?

DBA Dash doesn't have any built in alerting capabilities, but you can create your own custom alerts based on the data collected.  Some [examples here](/Alerts.md)

## How many instances can I monitor with DBA Dash?

There are no licensing restrictions or hard coded limits on the number of instances you can monitor with DBA Dash.  There are some practical limitations to consider which will impact how you deploy DBA Dash.  The DBA Dash GUI isn't designed for thousands of instances.  Collecting data from a large number of instances could also be a challenge. This will put additional pressure on the DBA Dash agent and the central repository database.  The size of the central repository database will also increase with the number of instances monitored. There are options if you have a large number of instances:

* Increase the ServiceThreads value in the ServiceConfig.json file to allow the DBA Dash agent to work with a larger number of instances.
* Use multiple DBA Dash agents. 
*Tip: Multiple agents can also be deployed to the same server.  Change the ServiceName in the ServiceConfig.json file to configure a unique name for each service.*
* Split the instances between multiple DBA Dash repositories.

Automation also becomes important as the number of instances increase.  DBADashConfig.exe can be used for automation.  To get started run:

`DBADashConfig --help`

## I get a 'Operation will cause database event session memory to exceed allowed limit.' error on AzureDB

You will typically run into this issue when using elastic pools.  You might need to be selective about which databases you enable slow query capture for.  [See here](https://github.com/trimble-oss/dba-dash/discussions/138) for more info.  

## The stored procedure names are not showing

Object names might display as {object_id:1234567}.  This can occur if the DBA Dash service account doesn't have permissions to collect the object name.  [Review the permissions](/Security.md) assgined to the service account.

## How do I get notifications of new releases?

Click the GitHub "Watch" button at the top of this page.  A drop down will appear.  Select "Custom".  Check "Releases" and click apply - this will only notify you when releases are published.  
