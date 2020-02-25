using DBAChecks;

namespace DBAChecksService
{


    public class CollectionConfigSchedule
    {

        public string ChronSchedule { get; set; }
        public bool RunOnServiceStart { get; set; }
        public CollectionType[] CollectionTypes { get; set; }

        public CollectionConfigSchedule()
        {

        }

        public CollectionConfigSchedule(string chronSchedule, bool runOnServiceStart, CollectionType[] collectionTypes)
        {
            this.ChronSchedule = chronSchedule;
            this.CollectionTypes = collectionTypes;
            this.RunOnServiceStart = runOnServiceStart;
        }

        public static CollectionConfigSchedule[] DefaultSchedules()
        {
            return new CollectionConfigSchedule[] { new CollectionConfigSchedule("0 0 * ? * *", true, new CollectionType[] { CollectionType.General }), new CollectionConfigSchedule("0 * * ? * *", true, new CollectionType[] { CollectionType.Performance }) };
        }
        public static CollectionConfigSchedule[] DefaultImportSchedule()
        {
            return new CollectionConfigSchedule[] { new CollectionConfigSchedule("0 * * ? * *", true, null) };
        }

    }

}
