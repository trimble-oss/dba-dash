using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace DBADash
{
    /// <summary>
    /// General purpose helpers shared by the collection service and the GUI projects.  Kept in a small assembly of its own so
    /// the GUI projects can use them without a reference to the whole of DBADash.
    /// </summary>
    public static class CommonExtensionMethods
    {
        public static T DeepCopy<T>(this T self)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto // Preserve type information
            };

            var serialized = JsonConvert.SerializeObject(self, settings);
            return JsonConvert.DeserializeObject<T>(serialized, settings);
        }

        public static string Truncate(this string value, int maxLength, bool ellipsis) => Truncate(value, maxLength, ellipsis ? "..." : null);

        public static string Truncate(this string value, int maxLength, string ellipsis = null)
        {
            if (maxLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            }

            if (!string.IsNullOrEmpty(ellipsis) && ellipsis.Length >= maxLength)
            {
                throw new ArgumentException("Ellipsis length must be less than maxLength.", nameof(ellipsis));
            }

            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            if (maxLength == 0)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(ellipsis))
            {
                return value[..maxLength];
            }

            return value[..(maxLength - ellipsis.Length)] + ellipsis;
        }

        /// <summary>
        /// Replace single ' quote with two single quotes '' and encloses in single quotes.  Only to be used where input can't be parameterized
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SqlSingleQuoteWithEncapsulation(this string value) => $"'{value.SqlSingleQuote()}'";

        /// <summary>
        /// Replace single ' quote with two single quotes ''.  Only to be used where input can't be parameterized
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SqlSingleQuote(this string value) => value.Replace("'", "''");

        /// <summary>
        /// Replicates SQL Server QUOTENAME function - wrapping text in square brackets and doubling up on right square bracket.  Use SqlSingleQuote/SqlSingleQuoteWithEncapsulation for single quotes.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SqlQuoteName(this string value) => $"[{value.Truncate(128).Replace("]", "]]")}]";

        /// <summary>
        /// Attach a continuation that runs only if the task faults. Success or cancellation adds virtually no overhead.
        /// </summary>
        /// <param name="task">The task to observe.</param>
        /// <param name="onError">Optional error handler (log, trace, etc).</param>
        public static void ObserveFault(this Task task, Action<Exception> onError = null)
        {
            if (task == null) return;
            _ = task.ContinueWith(t =>
            {
                onError?.Invoke(t.Exception!.Flatten());
                _ = t.Exception; // mark observed
            },
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
