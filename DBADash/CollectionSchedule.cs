using DBADash;
using System.Collections.Generic;
using System.Linq;

namespace DBADashService
{
    public class CollectionSchedules : Dictionary<CollectionType, CollectionSchedule>
    {
        private const string every1min = "0 * * ? * *";
        private const string hourly = "0 0 * ? * *";
        private const string midnight = "0 0 0 1/1 * ? *";
        private const string elevenPm = "0 0 23 1/1 * ? *";
        private const string disabled = "";

        private static readonly CollectionSchedules collectionSchedules = new() {
                            {CollectionType.ServerProperties, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.Databases, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.SysConfig, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.Drives, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.DBFiles, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.Backups, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.LogRestores, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.ServerExtraProperties, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.DBConfig, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.Corruption, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.TraceFlags, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.DBTuningOptions, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.AzureDBServiceObjectives, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.LastGoodCheckDB, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.Alerts, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.CustomChecks, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.DatabaseMirroring, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.Jobs, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.AzureDBResourceGovernance, new CollectionSchedule(){ Schedule = hourly } },
                            {CollectionType.ServerServices, new CollectionSchedule(){Schedule = hourly} },

                            {CollectionType.ObjectExecutionStats, new CollectionSchedule(){ Schedule = every1min,RunOnServiceStart=false } },
                            {CollectionType.CPU, new CollectionSchedule(){ Schedule = every1min,RunOnServiceStart=false  } },
                            {CollectionType.IOStats, new CollectionSchedule(){ Schedule = every1min,RunOnServiceStart=false  } },
                            {CollectionType.Waits, new CollectionSchedule(){ Schedule = every1min,RunOnServiceStart=false  } },
                            {CollectionType.AzureDBResourceStats, new CollectionSchedule(){ Schedule = every1min,RunOnServiceStart=false  } },
                            {CollectionType.AzureDBElasticPoolResourceStats, new CollectionSchedule(){ Schedule = every1min,RunOnServiceStart=false  } },
                            {CollectionType.SlowQueries, new CollectionSchedule(){ Schedule = every1min,RunOnServiceStart=false  } },
                            {CollectionType.PerformanceCounters, new CollectionSchedule(){ Schedule = every1min,RunOnServiceStart=false } },
                            {CollectionType.JobHistory, new CollectionSchedule(){ Schedule = every1min,RunOnServiceStart=false  } },
                            {CollectionType.RunningQueries, new CollectionSchedule(){ Schedule = every1min,RunOnServiceStart=false  } },
                            {CollectionType.DatabasesHADR, new CollectionSchedule(){ Schedule = every1min,RunOnServiceStart=false  } },
                            {CollectionType.AvailabilityGroups, new CollectionSchedule(){ Schedule = every1min,RunOnServiceStart=false  } },
                            {CollectionType.AvailabilityReplicas, new CollectionSchedule(){ Schedule = every1min,RunOnServiceStart=false  } },
                            {CollectionType.MemoryUsage, new CollectionSchedule(){ Schedule = every1min,RunOnServiceStart=false  } },
                            {CollectionType.RunningJobs, new CollectionSchedule(){ Schedule = every1min,RunOnServiceStart=false  } },
                            {CollectionType.OSInfo, new CollectionSchedule(){ Schedule = every1min } },

                            {CollectionType.ServerPrincipals, new CollectionSchedule(){ Schedule = midnight } },
                            {CollectionType.ServerRoleMembers, new CollectionSchedule(){ Schedule = midnight } },
                            {CollectionType.ServerPermissions, new CollectionSchedule(){ Schedule = midnight } },
                            {CollectionType.DatabasePrincipals, new CollectionSchedule(){ Schedule = midnight } },
                            {CollectionType.DatabaseRoleMembers, new CollectionSchedule(){ Schedule = midnight } },
                            {CollectionType.DatabasePermissions, new CollectionSchedule(){ Schedule = midnight } },
                            {CollectionType.VLF, new CollectionSchedule(){ Schedule = midnight } },
                            {CollectionType.DriversWMI, new CollectionSchedule(){ Schedule = midnight } },
                            {CollectionType.OSLoadedModules, new CollectionSchedule(){ Schedule = midnight } },
                            {CollectionType.ResourceGovernorConfiguration, new CollectionSchedule(){ Schedule = midnight } },
                            {CollectionType.DatabaseQueryStoreOptions, new CollectionSchedule(){ Schedule = midnight } },
                            {CollectionType.IdentityColumns, new CollectionSchedule(){ Schedule = midnight} },
                            {CollectionType.SchemaSnapshot, new CollectionSchedule(){Schedule=elevenPm} },
                            {CollectionType.AvailableProcs, new CollectionSchedule(){Schedule = elevenPm}},

                            { CollectionType.TableSize, new CollectionSchedule() {Schedule = disabled, RunOnServiceStart = false} },
        };

        public static readonly CollectionSchedules DefaultSchedules
               = collectionSchedules;

        public CollectionSchedules CombineWithDefault()
        {
            return Combine(DefaultSchedules, this);
        }

        public static CollectionSchedules Combine(CollectionSchedules baseSchedule, CollectionSchedules overrideSchedule)
        {
            var combined = new CollectionSchedules();
            if (overrideSchedule != null)
            {
                foreach (var s in overrideSchedule)
                {
                    combined.Add(s.Key, s.Value);
                }
            }
            foreach (var s in baseSchedule)
            {
                if (overrideSchedule == null || !overrideSchedule.ContainsKey(s.Key))
                {
                    combined.Add(s.Key, s.Value);
                }
            }
            return combined;
        }

        public Dictionary<string, CollectionType[]> GroupedBySchedule
        {
            get
            {
                var groupedSchedule = new Dictionary<string, CollectionType[]>();
                var schedules = this.Where(s => s.Key != CollectionType.SchemaSnapshot && !string.IsNullOrEmpty(s.Value.Schedule)).Select(s => s.Value.Schedule).Distinct().ToArray();
                foreach (var schedule in schedules)
                {
                    groupedSchedule.Add(schedule, this.Where(s => s.Value.Schedule == schedule).Select(s => s.Key).ToArray());
                }
                return groupedSchedule;
            }
        }

        public CollectionType[] OnServiceStartCollection
        {
            get
            {
                return this.Where(s => s.Key != CollectionType.SchemaSnapshot && s.Value.RunOnServiceStart).Select(s => s.Key).ToArray();
            }
        }
    }

    public class CollectionSchedule
    {
        public string Schedule;
        public bool RunOnServiceStart = true;

        private const string every1min = "0 * * ? * *";
        private static readonly CollectionSchedule importSchedule = new() { Schedule = "10" };
        public static readonly CollectionSchedule DefaultImportSchedule = importSchedule;
    }
}