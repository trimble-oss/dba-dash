/* TODO






* Maintain history of alert generation.  
* Some alerts might not be notified if notification channel isn't active for time period.  Some way to indicate why alert wasn't sent?
* Alerts should probably appear in the app as well as be sent to notification channels
* Consider how to handle different notification channels.  e.g. google chat, email, slack, pager duty, etc.



What to alert on?
Performance:
Wait type.  Blocking LCK_%, RESOURCE_SEMAPHORE, RESOURCE_SEMAPHORE_QUERY_COMPILE, THREADPOOL
Perf Counters.  e.g. Memory grants pending.  Low memory signal state, nodes reporting thread resources low etc.
CPU
IO?
-----
Backups
AGs
Job Failure
Instance availability


Done:
* Thread key probably needs to be a GUID to avoid potential conflicts with other apps.  | Solved with hash
* Hours, DaysOfWeek - how to handle timezones? | TZ can be specified on schedule
* Alert notification of resolution 
* Acknowledgement & supression of alerts?
* Alert status needed.  e.g. Active, Resolved
* Archival of alerts
* Alert re-trigger/update frequency.  Might depend on the alert type or priority of the alert.  Maybe this reduces over time?  
* Supress all alerts for maintanance?  
* Mute a channel for a period of time?
*/