using System;
using System.Collections.Generic;
using System.Text;

namespace DBADashService
{
    /// <summary>
    /// In-memory state for a WorkItem to persist state between executions
    /// Lost on service restart (same as current behavior with JobDataMap).
    /// </summary>
    public class WorkItemState
    {
        public int JobInstanceId { get; set; }

        /// <summary>Where the next Elastic Jobs collection starts from - see DBCollector.ElasticJobsFromUtc.  Null collects the default lookback.</summary>
        public DateTime? ElasticJobsFromUtc { get; set; }

        /// <summary>The elastic job definitions last sent, and when - see DBCollector.ElasticJobsDefinitionsHash.</summary>
        public byte[] ElasticJobsDefinitionsHash { get; set; }

        public DateTime? ElasticJobsDefinitionsSentUtc { get; set; }
        public bool IsExtendedEventsNotSupportedException { get; set; }
        public DateTime JobLastModified { get; set; } = DateTime.MinValue;
        public DateTime JobCollectDate { get; set; } = DateTime.MinValue;
    }
}