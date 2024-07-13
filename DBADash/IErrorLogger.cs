using System;

namespace DBADash
{
    public interface IErrorLogger
    {
        public void LogError(Exception ex, string errorSource, string errorContext);
    }
}