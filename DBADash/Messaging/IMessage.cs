using System;
using System.Threading.Tasks;
using System.Data;

namespace DBADash.Messaging
{
    public interface IMessage
    {
        public Task<DataSet> Process(CollectionConfig cfg, Guid handle);
    }
}