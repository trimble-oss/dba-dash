using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace DBADash.Messaging
{
    public interface IMessage
    {
        public Task<DataSet> Process(CollectionConfig cfg, Guid handle);
    }
}