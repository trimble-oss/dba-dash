using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DBADash.Messaging
{
    /// <summary>
    /// The jobs the service will start or stop when asked from the GUI - <see cref="CollectionConfig.AllowedJobs"/>.  A comma-separated list of
    /// job names, categories or job IDs, with * or % as wildcards.  An item prefixed with "-" denies, and a deny wins.  Blank allows none.
    /// Shared by SQL Agent jobs (<see cref="AgentJobExecutionMessage"/>) and elastic jobs (<see cref="ElasticJobExecutionMessage"/>).
    /// </summary>
    internal static class AllowedJobsFilter
    {
        /// <summary>True if any of the job's values (its name, category, ID) is allowed and none is denied</summary>
        public static bool IsAllowed(string allowedJobs, IReadOnlyCollection<string> jobValues)
        {
            if (string.IsNullOrEmpty(allowedJobs)) return false;
            if (allowedJobs is "*" or "%") return true;
            var items = allowedJobs.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var denied = items.Where(s => s.StartsWith('-')).Select(s => s[1..]);
            var allowed = items.Where(s => !s.StartsWith('-'));
            return !denied.Any(pattern => jobValues.Any(value => IsWildcardMatch(value, pattern)))
                   && allowed.Any(pattern => jobValues.Any(value => IsWildcardMatch(value, pattern)));
        }

        /// <summary>True if any of the job's values is denied - for the error message</summary>
        public static bool IsDenied(string allowedJobs, IReadOnlyCollection<string> jobValues) =>
            !string.IsNullOrEmpty(allowedJobs) &&
            allowedJobs.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.StartsWith('-'))
                .Select(s => s[1..])
                .Any(pattern => jobValues.Any(value => IsWildcardMatch(value, pattern)));

        /// <summary>Throws if the job can't be started or stopped</summary>
        /// <param name="allowedJobs"><see cref="CollectionConfig.AllowedJobs"/></param>
        /// <param name="jobValues">Name, category, ID - whatever identifies the job</param>
        /// <param name="jobDescription">How the job is described in the error</param>
        public static void ThrowIfNotAllowed(string allowedJobs, IReadOnlyCollection<string> jobValues, string jobDescription)
        {
            if (IsDenied(allowedJobs, jobValues))
            {
                throw new Exception($"Job execution is denied for {jobDescription}");
            }
            if (!IsAllowed(allowedJobs, jobValues))
            {
                throw new Exception($"Job execution is not allowed for {jobDescription}");
            }
        }

        internal static bool IsWildcardMatch(string value, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return string.IsNullOrEmpty(value);

            if (pattern is "*" or "%")
                return true;

            value ??= string.Empty;

            // For exact matches (no wildcards), use simple comparison
            if (!pattern.Contains('*') && !pattern.Contains('%'))
                return string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);

            // Use regex for wildcard patterns
            var regexPattern = Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("%", ".*");

            return Regex.IsMatch(value, $"^{regexPattern}$", RegexOptions.IgnoreCase);
        }
    }
}
