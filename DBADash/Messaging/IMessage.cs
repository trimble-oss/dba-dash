using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADash.Messaging
{
    public interface IMessage
    {
        Task Process(CollectionConfig cfg, Guid handle);
    }
}