using DBADash;
using Serilog;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace DBADashService
{
    public interface IWorkItem
    {
        public string DedupKey { get; }

        public DBADashSource Source { get; }

        public string Schedule { get; set; }

        public WorkItemPriority Priority { get; }

        public string Description { get; }

        Task ExecuteAsync(CollectionConfig config, CancellationToken cancellationToken);
    }
}