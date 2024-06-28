using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADashGUI.Messaging
{
    internal class BrokerResponse
    {
        public string Type { get; set; }
        public byte[] Payload { get; set; }

        public Guid Handle { get; set; }
    }
}
