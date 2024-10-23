using System;
using System.Threading.Tasks;
using System.Data;
using System.Threading;

namespace DBADash.Messaging
{
    public interface IMessage
    {
        public Task<DataSet> Process(CollectionConfig cfg, Guid handle, CancellationToken cancellationToken);
    }
}