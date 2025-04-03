using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADash
{
    [Serializable]
    public class DatabaseConnectionException : Exception
    {
        public DatabaseConnectionException()
        { }

        public DatabaseConnectionException(string message)
            : base(message) { }

        public DatabaseConnectionException(string message, Exception inner)
            : base(message, inner) { }
    }
}