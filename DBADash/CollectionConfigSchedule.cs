using DBADash;

namespace DBADashService
{


    public class CollectionConfigSchedule
    {

        public string CronSchedule { get; set; }
        public bool RunOnServiceStart { get; set; }
        public CollectionType[] CollectionTypes { get; set; }

        public CollectionConfigSchedule()
        {

        }

        public CollectionConfigSchedule(string cronSchedule, bool runOnServiceStart, CollectionType[] collectionTypes)
        {
            this.CronSchedule = cronSchedule;
            this.CollectionTypes = collectionTypes;
            this.RunOnServiceStart = runOnServiceStart;
        }

        public static CollectionConfigSchedule[] DefaultSchedules()
        {
            return new CollectionConfigSchedule[] { new CollectionConfigSchedule("0 0 * ? * *", true, new CollectionType[] { CollectionType.General }), 
                                                    new CollectionConfigSchedule("0 * * ? * *", true, new CollectionType[] { CollectionType.Performance }),
                                                    new CollectionConfigSchedule("0 0 0 1/1 * ? *", true, new CollectionType[] { CollectionType.Infrequent})};
        }
        public static CollectionConfigSchedule[] DefaultImportSchedule()
        {
            return new CollectionConfigSchedule[] { new CollectionConfigSchedule("0 * * ? * *", true, null) };
        }

    }

}
