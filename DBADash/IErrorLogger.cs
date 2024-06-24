using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DBADash
{
    public interface IErrorLogger
    {
        public void LogError(Exception ex, string errorSource, string errorContext);
    }
}